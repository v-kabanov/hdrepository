using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using bfs.Repository.Storage;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;


namespace RepositoryTests
{
    
    
    /// <summary>
    ///This is a test class for DataFileIteratorTest and is intended
    ///to contain all DataFileIteratorTest Unit Tests
    ///</summary>
	[TestFixture]
	public class DataFileIteratorTest : TestBase
	{
		private const int _itemsIntervalMinutes = 40;
		private static int _dataItemsPerFile = 200;

		private static IDataFolder _emptyDataFolder;
		private static RepoFileContainerDescriptor _deletedDataFolder;
		private static DateTime _firstDataItemTime;
		private static DateTime _lastDataItemTime;
		private static double _daysPerFile;

		private static int _expectedFileCount;


		#region Additional test attributes
		 
		
		[TestFixtureSetUp]
		public void MyClassInitialize()
		{
			lock (GetType())
			{
				if (_emptyDataFolder == null)
				{
					const int itemsCount = 100000;
					_daysPerFile = ((double)_dataItemsPerFile) * _itemsIntervalMinutes / 60.0 / 24.0;
					// 200 * 20 minutes = 8000 minutes per file (5.55556 days)

					DateTime firstTime = DateTime.Now.AddDays(-10);
					_firstDataItemTime = firstTime;
					//_expectedFileCount = (int)Math.Ceiling((double)itemsCount / (double)_dataItemsPerFile);

					IFolder targetFolder = (IFolder)FixtureRootRepoFolder;
					string targetFolderPath = targetFolder.FullPath;

					targetFolder.Properties.DesiredItemsPerFile = _dataItemsPerFile;
					using (IRepositoryWriter writer = targetFolder.GetWriter())
					{
						DateTime lastTime = DateTime.MinValue;
						int n;
						for (n = 0; n < itemsCount; ++n)
						{
							Mock.TestDataItem item = Mock.TestDataItem.GetTestItem(n);
							lastTime = firstTime.AddMinutes(n * _itemsIntervalMinutes);
							item.DateTime = lastTime;
							writer.Write(item);
						}

						_lastDataItemTime = lastTime;

						writer.Flush();
						writer.Close();
					}

					for (
						var dataFile = targetFolder.RootDataFolder.FindFirstDataFile(false);
						dataFile != null;
						dataFile = dataFile.GetNext(false), ++_expectedFileCount
					)
					{ }

					Console.WriteLine("Expected file count enumerated via RepositoryFile: {0}", _expectedFileCount);

					// data folder boundaries may split data files thus extra ones
					Assert.GreaterOrEqual(_expectedFileCount, (int)Math.Ceiling((double)itemsCount / (double)_dataItemsPerFile), "Data file count unexpected");

					// creating empty folder
					IRepositoryFile file = targetFolder.RootDataFolder.Seek(firstTime.AddMinutes(itemsCount * _itemsIntervalMinutes / 3), false);
					_emptyDataFolder = file.ContainingFolder;

					for (
						file = _emptyDataFolder.FindFirstDataFile(false);
						file != null && file.ContainingFolder == _emptyDataFolder;
						file = file.GetNext(false))
					{
						file.Delete();
						--_expectedFileCount;
					}

					Assert.AreEqual(0, _emptyDataFolder.DataFileBrowser.FileCount);

					Console.WriteLine("Expected file count after removing file by file: {0}", _expectedFileCount);

					//
					IDataFolder dfolderToDelete = _emptyDataFolder.ParentDataFolder.GetNextSiblingInTree(false).GetNextSiblingInTree(false);

					Assert.AreEqual(1, dfolderToDelete.Level);

					_deletedDataFolder = new RepoFileContainerDescriptor()
					{
						Start = dfolderToDelete.Start,
						End = dfolderToDelete.End,
						Level = dfolderToDelete.Level,
						RelativePath = dfolderToDelete.RelativePath
					};

					_expectedFileCount -= dfolderToDelete.GetSubfolders(DateTime.MinValue, false).Sum((f) => f.DataFileBrowser.FileCount);

					Console.WriteLine("Expected file count after removing data folder {0}: {1}", dfolderToDelete.PathInRepository, _expectedFileCount);

					Console.WriteLine("Removing folder {0}", dfolderToDelete.PathInRepository);

					dfolderToDelete.Delete(false);

					Assert.IsFalse(dfolderToDelete.Exists);
				}
			} //lock
		}

		#endregion


		[Test]
		public void QuickIntegrationTest()
		{
			IRepositoryFolder targetFolder = FixtureRootRepoFolder;
			IDataFileIterator iterator = Repository.ObjectFactory.GetDataFileIterator(targetFolder, false);

			// this must guarantee that previous, current and next are all not null
			DateTime seekTime = _firstDataItemTime.AddDays(_daysPerFile * 3);

			iterator.Seek(seekTime);
			Assert.IsNotNull(iterator.Current);
			Assert.IsNotNull(iterator.NextBackwards);
			Assert.IsNotNull(iterator.NextForward);

			Assert.IsTrue(iterator.Current.Name.End > seekTime);
			Assert.IsTrue(iterator.NextBackwards.Name.End <= seekTime);
			Assert.IsTrue(iterator.NextForward.Name.FirstItemTimestamp > seekTime);
		}

		[Test]
		public void StateTrackingTest()
		{
			using (var repo = GetStandaloneRepository())
			{
				var folder = (IFolder)repo.RootFolder.GetDescendant(FixtureRootRepoFolder.LogicalPath, false);

				var iterator = repo.ObjectFactory.GetDataFileIterator(folder, false);

				// this must guarantee that previous, current and next are all not null
				iterator.Seek(_firstDataItemTime.AddDays(_daysPerFile * 3));

				var parent = folder.ParentFolder;

				folder.Detach(true);

				Assert.Throws<InvalidOperationException>(() => iterator.MoveNext());

				folder.Attach(parent: (IFolder)parent, addToParentsList: true);

				repo.Dispose();

				Assert.Throws<ObjectDisposedException>(() => iterator.MoveNext());
			}
		}

		[Test]
		public void ForwardCountTest()
		{
			var iterator = Repository.ObjectFactory.GetDataFileIterator(FixtureRootRepoFolder, false);
			iterator.Seek(DateTime.MinValue);

			Assert.IsNull(iterator.Previous, "Before moving forward there can be no previous");

			int count = 0;
			IRepositoryFile previous = null;

			while (iterator.Current != null)
			{
				if (count > 0)
				{
					Assert.Greater(iterator.Current.Name.FirstItemTimestamp, previous.Name.LastItemTimestamp);
				}
				++count;
				previous = iterator.Current;
				iterator.MoveNext();
				Assert.AreSame(previous, iterator.Previous);
			}

			Assert.AreEqual(_expectedFileCount, count);
		}

		[Test]
		public void BackwardCountTest()
		{
			var iterator = Repository.ObjectFactory.GetDataFileIterator(FixtureRootRepoFolder, true);
			iterator.Seek(DateTime.MaxValue);

			Assert.IsNull(iterator.Previous, "Before moving forward there can be no previous");

			int count = 0;
			IRepositoryFile previous = null;

			while (iterator.Current != null)
			{
				if (count > 0)
				{
					Assert.Less(iterator.Current.Name.LastItemTimestamp, previous.Name.FirstItemTimestamp);
				}
				++count;
				previous = iterator.Current;
				iterator.MoveNext();
				Assert.AreSame(previous, iterator.Previous);
			}

			Assert.AreEqual(_expectedFileCount, count);
		}
	}
}
