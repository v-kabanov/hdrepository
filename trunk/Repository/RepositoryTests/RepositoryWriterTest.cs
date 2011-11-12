using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Threading;
using System.Text;
using NUnit.Framework;

using bfs.Repository.Interfaces;
using bfs.Repository.Storage;


namespace RepositoryTests
{
    
    
    /// <summary>
    ///This is a test class for RepositoryWriterTest and is intended
    ///to contain all RepositoryWriterTest Unit Tests
    ///</summary>
	[TestFixture]
	public class RepositoryWriterTest : TestBase
	{
		/// <summary>
		///		A test for Write
		///</summary>
		[Test]
		public void WriteTest()
		{
			string subFolderName = "WriteTest";

			int initialSubfoldersCount = Repository.RootFolder.SubFolders.Count;

			IRepositoryFolder targetFolder = FixtureRootRepoFolder.CreateSubfolder(subFolderName);

			IRepositoryWriter writer = targetFolder.GetWriter();

			writer.AllowSubfoldersCreation = true;

			string fullPath = targetFolder.FullPath;

			Mock.TestDataItem item;

			for (int n = 0; n < 100; ++n)
			{
				item = Mock.TestDataItem.GetTestItem(n);
				writer.Write(item);
			}

			// testing automatic subfolder creation
			item = Mock.TestDataItem.GetTestItem(1000);

			item.RelativePath = "AutoSubfolder1/Auto2";

			writer.Write(item);

			Assert.AreEqual(1, targetFolder.SubFolders.Count, "Automatic subfolder creation during write failed");
			Assert.IsNotNull(targetFolder.GetSubFolder("AutoSubfolder1"));
			Assert.AreEqual(1, targetFolder.GetSubFolder("AutoSubfolder1").SubFolders.Count);
			Assert.IsNotNull(targetFolder.GetSubFolder("AutoSubfolder1").GetSubFolder("Auto2"));

			writer.Flush();
			writer.Close();

			targetFolder.Delete(true, true);
			Assert.IsFalse(Directory.Exists(fullPath), "Directory not removed from disk");
			Assert.AreEqual(initialSubfoldersCount, Repository.RootFolder.SubFolders.Count);
		}

		[Test]
		public void DefaultSettingsTest()
		{
			IRepositoryWriter writer = Repository.RootFolder.GetWriter();

			Assert.IsTrue(writer.TrackUnsavedItems, "Default setting for TrackUnsavedItems should be true");
			Assert.IsFalse(writer.AllowSubfoldersCreation, "Default setting for AllowSubfoldersCreation should be false");

			writer.Close();
		}

		[Test]
		public void GetUnsavedItemsWithFlushTest()
		{
			const int subfolderCount = 3;

			string subFolderName = "GetUnsavedItemsTest";

			IRepositoryFolder targetFolder = FixtureRootRepoFolder.CreateSubfolder(subFolderName);

			IRepositoryWriter writer = targetFolder.GetWriter();

			IDataRouter dataRouter = new Mock.NumberedDataRouter(subfolderCount);
			writer.DataRouter = dataRouter;
			writer.AllowSubfoldersCreation = true;

			string fullPath = targetFolder.FullPath;

			Mock.TestDataItem item;

			int lastFlushCount = 0;

			for (int n = 0; n < 10000; ++n)
			{
				item = Mock.TestDataItem.GetTestItem(n);
				writer.Write(item);

				if ((n + 1) % 10 == 0)
				{
					IDictionary<string, IList<IDataItem>> unsavedItems = writer.GetUnsavedItems();
					Assert.IsNotNull(unsavedItems);
					Assert.AreEqual(Math.Min(n + 1, subfolderCount), unsavedItems.Count, "Unsaved items dictionary entry count is not equal to the direct writers count");

					Assert.AreEqual(n + 1 - lastFlushCount, unsavedItems.Values.Sum((l) => l.Count), "Total number of unsaved items incorrect");
				}
				else if ((n + 1) % 134 == 0)
				{
					writer.Flush();
					lastFlushCount = n + 1;

					IDictionary<string, IList<IDataItem>> unsavedItems = writer.GetUnsavedItems();
					Assert.IsNotNull(unsavedItems);
					Assert.AreEqual(Math.Min(n + 1, subfolderCount), unsavedItems.Count, "Unsaved items dictionary entry count is not equal to the direct writers count");

					Assert.AreEqual(0, unsavedItems.Values.Sum((l) => l.Count), "Total number of unsaved items after flush must be 0");
				}
			}

			writer.Close();
		}
		
		[Test]
		public void GetUnsavedItemsAmbientTransactionTest()
		{
			const int subfolderCount = 3;

			const string subFolderName = "GetUnsavedItemsAmbientTransactionTest";

			IRepositoryFolder targetFolder = FixtureRootRepoFolder.CreateSubfolder(subFolderName);

			IRepositoryWriter writer = targetFolder.GetWriter();
			targetFolder.Properties.DesiredItemsPerFile = 100;

			IDataRouter dataRouter = new Mock.NumberedDataRouter(subfolderCount);
			writer.DataRouter = dataRouter;
			writer.AllowSubfoldersCreation = true;

			string fullPath = targetFolder.FullPath;

			Mock.TestDataItem item;
			IDictionary<string, IList<IDataItem>> unsavedItems;

			using (TransactionScope scope = new TransactionScope())
			{
				Assert.IsNotNull(Transaction.Current);
	
				const int count = 10000;
	
				for (int n = 0; n < count; ++n)
				{
					item = Mock.TestDataItem.GetTestItem(n);
					writer.Write(item);
	
					if ((n + 1) % 134 == 0)
					{
						writer.Flush();
	
						unsavedItems = writer.GetUnsavedItems();
	
						Assert.IsNotNull(unsavedItems);
						
						Assert.AreEqual(Math.Min(n + 1, subfolderCount), unsavedItems.Count
						                , "Unsaved items dictionary entry count is not equal to the direct writers count");
	
						Assert.AreEqual(n + 1, unsavedItems.Values.Sum((l) => l.Count)
						                , "Total number of unsaved items after flush must not change if in ambient transaction");
					}
				}
				
				unsavedItems = writer.GetUnsavedItems();
	
				Assert.IsNotNull(unsavedItems);
				
				Assert.AreEqual(subfolderCount, unsavedItems.Count
				                , "Unsaved items dictionary entry count is not equal to the direct writers count");
	
				Assert.AreEqual(count, unsavedItems.Values.Sum((l) => l.Count)
				                , "Total number of unsaved items must equal number of added items if in ambient transaction");
				scope.Complete();
			}

			Thread.Sleep(50);
			
			unsavedItems = writer.GetUnsavedItems();

			Assert.IsNotNull(unsavedItems);
			
			Assert.AreEqual(subfolderCount, unsavedItems.Count
			                , "Unsaved items dictionary entry count is not equal to the direct writers count");

			Assert.AreEqual(0, unsavedItems.Values.Sum((l) => l.Count)
			                , "Total number of unsaved items after committing ambient transaction must be 0");

			writer.Close();
		}

		[Test]
		public void TestUnsavedItemsWithInterruption()
		{
			const string subFolderName = "TestUnsavedItemsWithInterruption";

			const int desiredFileSize = 100;

			Mock.MultiLevelDataRouter router = new Mock.MultiLevelDataRouter(3, 2);

			Repository.RootFolder.GetDescendant(FixtureRootRepoFolder.LogicalPath, false).CreateSubfolder(subFolderName); 

			IRepositoryWriter writer = GetStandaloneWriter(subFolderName, desiredFileSize, router);

			const int itemCount = 100000;
			const int intervalMinutes = 1;
			int intervalSameFolderMinutes = intervalMinutes * router.SubtreeFolderCount;
			IDataItem[] data = GetTestData(itemCount, DateTime.Now, intervalMinutes);

			const int checkIntervalItemCountBase = 57;
			Assume.That(checkIntervalItemCountBase > router.SubtreeFolderCount);

			var random = new Random();
			int nextCheckCount = checkIntervalItemCountBase;

			int stopAndRestartCounter = itemCount / 2 + random.Next(-itemCount / 5, itemCount / 5);
			
			for (int n = 0; n < itemCount; ++n)
			{
				writer.Write(dataItem: data[n]);

				if (n == nextCheckCount)
				{
					nextCheckCount = nextCheckCount + checkIntervalItemCountBase + random.Next(20) - 10;

					IDictionary<string, IList<IDataItem>> unsavedItemsDict = writer.GetUnsavedItems();
					Assert.AreEqual(router.SubtreeFolderCount, unsavedItemsDict.Count);

					Dictionary<string, DateTime> lastSavedTimestamps = new Dictionary<string, DateTime>();
					
					foreach (KeyValuePair<string, IList<IDataItem>> pair in unsavedItemsDict)
					{
						string relativePath = pair.Key.Substring(writer.Folder.LogicalPath.Length);
						IRepositoryFolder sourceFolder = writer.Folder.GetDescendant(relativePath, false);
						Assert.IsNotNull(sourceFolder);
						DateTime lastFlushedTimestamp = sourceFolder.LastTimestamp;
						if (pair.Value.Count > 0)
						{
							if (lastFlushedTimestamp > DateTime.MinValue)
							{
								Assert.AreEqual(lastFlushedTimestamp.AddMinutes(intervalSameFolderMinutes), pair.Value[0].DateTime);
							}
							else
							{
								Assert.Less(n, desiredFileSize * (router.SubtreeFolderCount + 1), "Data must have been flushed by now due to desired file size");
							}
							lastSavedTimestamps[relativePath] = pair.Value[pair.Value.Count - 1].DateTime;
						}
						else
						{
							Assert.AreNotEqual(DateTime.MinValue, lastFlushedTimestamp);
							lastSavedTimestamps[relativePath] = lastFlushedTimestamp;
						}

					}

					DateTime lastSavedTimestampTotal = lastSavedTimestamps.Values.Max();
					List<DateTime> lastSavedPerFolder = lastSavedTimestamps.Values.ToList();
					lastSavedPerFolder.Sort();

					for (int j = 0; j < router.SubtreeFolderCount - 2; ++j)
					{
						Assert.AreEqual(lastSavedPerFolder[j].AddMinutes(intervalMinutes), lastSavedPerFolder[j + 1]);
					}

					Assert.AreEqual(lastSavedPerFolder[router.SubtreeFolderCount - 1], data[n].DateTime);
				} // if (n == nextCheckCount)

				if (n == stopAndRestartCounter)
				{
					var unsavedItemsDict = writer.GetUnsavedItems();
					writer = GetStandaloneWriter(subFolderName, desiredFileSize, router);

					var unsavedList = MergeUnsavedItems(unsavedItemsDict);
					foreach (var dataItem in unsavedList)
					{
						writer.Write(dataItem);
					}
				}
			}
			
			IRepositoryFolder targetFolder = writer.Folder;

			writer.Close();

			CheckAllDataInFolder(targetFolder, data);
		}

		[Test]
		public void TestWritingNotInOrder()
		{
			const string subFolderName = "TestWritingNotInOrder";

			const int desiredFileSize = 100;

			Mock.MultiLevelDataRouter router = new Mock.MultiLevelDataRouter(2, 2);

			IRepositoryFolder targetFolder = Repository.RootFolder.GetDescendant(FixtureRootRepoFolder.LogicalPath, false).CreateSubfolder(subFolderName);
			targetFolder.Properties.DesiredItemsPerFile = desiredFileSize;


			const int itemCount = 100000;
			const int intervalMinutes = 1;

			IDataItem[] data = GetTestData(itemCount, DateTime.Now, intervalMinutes);

			for (int j = 0; j < 2; ++j)
			{
				using (IRepositoryWriter writer = GetWriter(targetFolder, router))
				{
					for (int n = j; n < itemCount; n = n + 2)
					{
						writer.Write(dataItem: data[n]);
					}
				}
			}

			// the following commented out call is necessary if writing was done instandalone instance because target folder does not know that subfolders have been created
			// during writing; but now I am not using standalone writer
			//targetFolder.Refresh(true, true);
			
			CheckAllDataInFolder(targetFolder, data);
		}

		private void CheckAllDataInFolder(IRepositoryFolder folder, IDataItem[] expectedData)
		{
			List<IDataItem> dataRead = new List<IDataItem>(expectedData.Length);

			using (IRepositoryReader reader = folder.GetReader(DateTime.MinValue, true))
			{
				while (reader.HasData)
				{
					dataRead.Add(reader.Read().DataItem);
				}
			}
			Assert.AreEqual(expectedData.Length, dataRead.Count, "The total number of items read after writing does not match the amount written");
			for (int n = 0; n < expectedData.Length; ++n)
			{
				Assert.AreEqual(expectedData[n], dataRead[n]);
			}
		}

		private List<IDataItem> MergeUnsavedItems(IDictionary<string, IList<IDataItem>> unsavedItems)
		{
			int totalCount = unsavedItems.Sum((p) => p.Value.Count);
			List<IDataItem> retval = new List<IDataItem>(totalCount);
			foreach (var values in unsavedItems.Values)
			{
				retval.AddRange(values);
			}
			retval.Sort(new Comparison<IDataItem>((x, y) => x.DateTime.CompareTo(y.DateTime)));
			return retval;
		}

		internal static IDataItem[] GetTestData(int count, DateTime firstTimestamp, int intervalMinutes)
		{
			IDataItem[] retval = new IDataItem[count];
			DateTime timestamp = firstTimestamp;
			Mock.TestDataItem dataItem;
			for (int n = 0; n < count; ++n)
			{
				dataItem = Mock.TestDataItem.GetTestItem(n);
				dataItem.DateTime = timestamp;
				retval[n] = dataItem;

				timestamp = timestamp.AddMinutes(intervalMinutes);
			}
			return retval;
		}
	}
}
