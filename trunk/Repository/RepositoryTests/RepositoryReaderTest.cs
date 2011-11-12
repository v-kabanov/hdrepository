using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using bfs.Repository.Storage;
using bfs.Repository.Interfaces;
using bfs.Repository.Events;
using RepositoryTests.Mock;
using bfs.Repository.Interfaces.Infrastructure;


namespace RepositoryTests
{
	[TestFixture]
	public class RepositoryReaderTest : TestBase
	{
		public class SeekStatusListener
		{
			private List<FolderSeekStatus> _statuses = new List<FolderSeekStatus>(20);

			public void HanldeStatus(object sender, PositionRestoreStatusEventArgs args)
			{
				_statuses.Add(args.Status);
			}

			public IList<FolderSeekStatus> Statuses
			{
				get { return _statuses.AsReadOnly(); }
			}
		}

		[Test]
		public void QuickReaderTest()
		{
			string targetFolderName = "QuickReaderTest";
			IRepositoryFolder targetFolder = FixtureRootRepoFolder.GetSubFolder(targetFolderName);
			if (targetFolder != null)
			{
				targetFolder.Delete(true, true);
			}
			targetFolder = FixtureRootRepoFolder.CreateSubfolder(targetFolderName);

			string targetFolderPath = targetFolder.FullPath;

			const int subfolderCount = 3;
			const int itemsIntervalHours = 1;
			const int desiredFileSize = 2000;

			targetFolder.Properties.DesiredItemsPerFile = desiredFileSize;
			IRepositoryWriter writer = targetFolder.GetWriter();

			IDataRouter dataRouter = new NumberedDataRouter(subfolderCount);
			writer.DataRouter = dataRouter;
			writer.AllowSubfoldersCreation = true;

			DateTime firstTime = DateTime.Now.AddDays(-10);
			DateTime lastTime = DateTime.MinValue;
			int itemsCount = 100000;
			int n;

			for (n = 0; n < itemsCount; ++n)
			{
				Mock.TestDataItem item = Mock.TestDataItem.GetTestItem(n);
				lastTime = firstTime.AddHours(n * itemsIntervalHours);
				item.DateTime = lastTime;
				writer.Write(item);
			}

			writer.Flush();
			writer.Close();
			// will test lazy loading
			targetFolder.UnloadSubfolders();

			Assert.IsTrue(targetFolder.SubFolders.Count == subfolderCount, "Router had to make writer create the configured number of subfolders");

			IRepositoryFolder firstItemSubfolder = targetFolder.GetDescendant(
				dataRouter.GetRelativePath(Mock.TestDataItem.GetTestItem(0)), false);

			Assert.AreEqual(firstTime, firstItemSubfolder.FirstTimestamp
				, "Fisrt item timestamp reported incorrectly by Folder.FirstTimestamp");

			Assert.AreEqual(firstTime, targetFolder.GetFirstItemTimestamp(true, false)
				, "Fisrt item timestamp reported incorrectly by Folder.GetFirstItemTimestamp");

			IRepositoryReader reader = targetFolder.GetReader(firstTime, true);
			Assert.IsTrue(reader.HasData, "Folder just populated but no data can be read");

			IDataItemRead ritem = null;
			n = 0;

			IRepositoryReader altReader = null;
			SeekStatusListener seekStatusListener = new SeekStatusListener();

			while (reader.HasData)
			{
				if (n > 0 && n % 100 == 0)
				{
					altReader = Repository.ObjectFactory.GetReader(reader.Position, seekStatusListener.HanldeStatus);
				}
				ritem = reader.Read();
				Assert.IsNotNull(ritem, "reader.Read() returned null after returning true from HasData");
				Assert.AreNotSame(targetFolder, ritem.RepositoryFolder, "Router failed");

				Assert.IsInstanceOf<Mock.TestDataItem>(ritem.DataItem, "Data item read from repository is of different type");
				Assert.AreEqual(firstTime.AddHours(n * itemsIntervalHours), ritem.DataItem.DateTime);
				((Mock.TestDataItem)ritem.DataItem).Check(n);

				if (altReader != null)
				{
					IDataItemRead altItem = altReader.Read();
					Assert.AreEqual(ritem.DataItem.DateTime, altItem.DataItem.DateTime);
					Assert.AreEqual(0, seekStatusListener.Statuses.Count);
				}

				++n;
			}

			Assert.AreEqual(lastTime, ritem.DataItem.DateTime, "Last item has unexpected timestamp");
			Assert.AreEqual(itemsCount, n, "Unexpected number of data items read");

			DateTime timestampToSeek = firstTime.AddHours(desiredFileSize / 3 * itemsIntervalHours);

			reader.Seek(timestampToSeek);
			Assert.IsTrue(reader.HasData, "Repeated Seek after reading all failed");
			ritem = reader.Read();
			Assert.IsNotNull(ritem);

			Assert.AreEqual(timestampToSeek, ritem.DataItem.DateTime, "First read item timestamp unexpected");

			reader.Direction = bfs.Repository.Util.EnumerationDirection.Backwards;

			Assert.IsTrue(reader.HasData, "No data after reversing in the middle of data");
			//ritem = reader.Read();
			//Assert.AreEqual<DateTime>(timestampToSeek, ritem.DataItem.DateTime
			//	, "First read item timestamp unexpected after changing direction");
			n = 0;
			altReader = null;

			while (reader.HasData)
			{
				if (n > 0 && n % 100 == 0)
				{
					if (altReader != null)
					{
						altReader.Dispose();
					}
					altReader = Repository.ObjectFactory.GetReader(reader.Position, seekStatusListener.HanldeStatus);
				}
				ritem = reader.Read();
				Assert.IsNotNull(ritem, "reader.Read() returned null after returning true from HasData");
				Assert.AreEqual(timestampToSeek.AddHours(-n * itemsIntervalHours), ritem.DataItem.DateTime);

				if (altReader != null)
				{
					IDataItemRead altItem = altReader.Read();
					Assert.AreEqual(ritem.DataItem.DateTime, altItem.DataItem.DateTime);
				}

				++n;
			}

			Assert.AreEqual(firstTime, ritem.DataItem.DateTime, "Did not pick up first item after reversing");

			// reversing after reaching end
			reader.Direction = bfs.Repository.Util.EnumerationDirection.Forwards;
			ritem = reader.Read();
			Assert.IsNotNull(ritem, "Did not read firts item reversing after reaching end");

			Assert.AreEqual(firstTime, ritem.DataItem.DateTime, "Did not pick up first item after reversing after reaching end");

			// cleanup
			//targetFolder.Delete(true, false);
			//Assert.IsFalse(Directory.Exists(targetFolderPath), "Test repo directory not removed from disk by Delete()");
		}

		[Test]
		public void ReadEmptyFolderTest()
		{
			string targetFolderName = "ReadEmptyFolderTest";
			IRepositoryFolder targetFolder = FixtureRootRepoFolder.CreateSubfolder(targetFolderName);

			using (IRepositoryReader target = targetFolder.GetReader(DateTime.MinValue, true))
			{
				Assert.IsFalse(target.HasData);
			}
		}

		[Test]
		public void RegistrationTest()
		{
			using (var reader = FixtureRootRepoFolder.GetReader(DateTime.MinValue, false))
			{
				Assert.IsTrue(reader.IsAccessing(FixtureRootRepoFolder, false));
				Assert.IsTrue(Repository.IsDataBeingAccessed(FixtureRootRepoFolder, false));
				Assert.IsTrue(Repository.IsDataBeingReadFrom(FixtureRootRepoFolder, false));
				var readers = Repository.GetReaders(FixtureRootRepoFolder, false);
				Assert.AreEqual(1, readers.Count);
				Assert.AreSame(reader, readers[0]);

				reader.Close();

				Assert.IsFalse(Repository.IsDataBeingAccessed(FixtureRootRepoFolder, false));
				Assert.IsFalse(Repository.IsDataBeingReadFrom(FixtureRootRepoFolder, false));
				Assert.IsFalse(reader.IsAccessing(FixtureRootRepoFolder, false));
				readers = Repository.GetReaders(FixtureRootRepoFolder, false);
				Assert.AreEqual(0, readers.Count);

				reader.Dispose();

				Assert.IsFalse(Repository.UnRegisterReader(reader), "Dispose did not unregister reader");

				Assert.Throws<ObjectDisposedException>(() => reader.AddFolder(FixtureRootRepoFolder));
				Assert.Throws<ObjectDisposedException>(() => reader.Seek(DateTime.Now));
				Assert.Throws<ObjectDisposedException>(() => reader.Direction = bfs.Repository.Util.EnumerationDirection.Backwards);
				Assert.Throws<ObjectDisposedException>(() => reader.CanChangeDirection.ToString());
				Assert.Throws<ObjectDisposedException>(() => reader.Read());
				Assert.Throws<ObjectDisposedException>(() => reader.RemoveFolder(null));

				Assert.IsFalse(reader.IsAccessing(FixtureRootRepoFolder, true));
			}
		}

		[Test]
		public void TestPositionHashVerification()
		{
			var folderName = "TestPositionHashVerification";
			IFolder folder = CreateNewTestFolder(folderName);
			folder.Properties.DesiredItemsPerFile = 10;

			var testItems = new TestDataItem[200];
			for (int n = 0; n < testItems.Length; ++n)
			{
				var item = TestDataItem.GetTestItem(n);
				item.DateTime = DateTime.Now.AddDays(n);
				testItems[n] = item;
			}


			using (var writer = folder.GetWriter())
			{
				foreach (var item in testItems)
				{
					writer.Write(item);
				}
			}

			SeekStatusListener seekStatusListener = new SeekStatusListener();

			using (var reader = folder.GetReader(DateTime.Now.AddHours(-1), false))
			{
				Random random = new Random();
				int skipItems = random.Next(50, 150);
				for (int n = 0; n < skipItems; ++n)
				{
					Assert.AreEqual(testItems[n], reader.Read().DataItem);
				}
				var position = new ReadingPosition(reader.Position);
				FolderReadingPosition folderPos = (FolderReadingPosition)position.FolderPositions[folder.FolderKey];
				Assert.IsNotNull(folderPos);

				folderPos.VerificationLastReadItemHash = ~folderPos.VerificationLastReadItemHash;

				using (var altReader = Repository.ObjectFactory.GetReader(position, seekStatusListener.HanldeStatus))
				{
					for (int n = skipItems; n < testItems.Length; ++n)
					{
						Assert.AreEqual(testItems[n], altReader.Read().DataItem);
					}
				}
				Assert.AreEqual(1, seekStatusListener.Statuses.Count);
				Assert.AreEqual(FolderSeekStatus.PositionStatus.DataItemHashMismatch, seekStatusListener.Statuses[0].Status);
			}
		}
	}
}
