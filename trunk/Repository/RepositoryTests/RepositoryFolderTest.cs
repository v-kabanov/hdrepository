using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Runtime.Serialization;
using NUnit.Framework;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Storage;
using RepositoryTests.Mock;


namespace RepositoryTests
{
    /// <summary>
    ///This is a test class for RepositoryFolderTest and is intended
    ///to contain all RepositoryFolderTest Unit Tests
    ///</summary>
	[TestFixture]
	public class RepositoryFolderTest : TestBase
	{
		public static void Populate(IRepositoryWriter writer, int count)
		{
			for (int n = 0; n < count; ++n)
			{
				Mock.TestDataItem item = Mock.TestDataItem.GetTestItem(n);
				writer.Write(item);
			}

			writer.Flush();
		}

		#region Additional test attributes

		//
		//Use ClassInitialize to run code before running the first test in the class
		[TestFixtureSetUp]
		public void MyClassInitialize()
		{
			IRepositoryWriter writer = FixtureRootRepoFolder.GetWriter();
			writer.AllowSubfoldersCreation = true;

			Populate(writer, 1000);
			writer.Flush();
			writer.Close();
		}

		//
		//Use ClassCleanup to run code after all tests in a class have run
		[TestFixtureTearDown]
		public void MyClassCleanup()
		{
			//FixtureRootRepoFolder.Delete(true, true);
		}

		#endregion

		[Test]
		public void RootFolderTest()
		{
			Assert.IsNotNull(Repository.RootFolder);
			Assert.IsEmpty(Repository.RootFolder.Name);
			Assert.IsNull(Repository.RootFolder.ParentFolder);
		}

		/// <summary>
		///A test for AddToReader
		///</summary>
		[Test]
		public void AddToReaderTest()
		{
			string name = "AddToReaderTest";
			string subName = "SubName";

			IRepositoryFolder target = FixtureRootRepoFolder.CreateSubfolder(name);
			IRepositoryFolder subFolder = target.CreateSubfolder(subName);

			//-----------------------------------

			Mock.RepositoryReaderMock mockedReader = new Mock.RepositoryReaderMock();
			target.AddToReader(mockedReader, false);
			Assert.AreEqual(1, mockedReader.AddFolderCalled);
			Assert.AreSame(mockedReader.LastFolderArgument, target);

			mockedReader.Reset();

			target.AddToReader(mockedReader, true);
			Assert.AreEqual(2, mockedReader.AddFolderCalled);
			Assert.IsTrue(mockedReader.FolderArguments.Contains(target));
			Assert.IsTrue(mockedReader.FolderArguments.Contains(subFolder));

			//------------------------------------
			//now real reader

			IRepositoryReader reader = Repository.RootFolder.GetReader(DateTime.Now.AddDays(-1), false);

			Assert.AreEqual(1, reader.Folders.Count, "Reader just created from a folder");

			target.AddToReader(reader, false);

			Assert.AreEqual(2, reader.Folders.Count, "Another reader added");
		}

		/// <summary>
		///A test for CreateSubfolder
		///</summary>
		[Test]
		public void CreateSubfolderTest()
		{
			string subFolderName = "CreateSubfolderTest";

			int initialSubfoldersCount = FixtureRootRepoFolder.SubFolders.Count;

			IRepositoryFolder subfolder1 = FixtureRootRepoFolder.CreateSubfolder(subFolderName);
			IRepositoryFolder rootFoder = Repository.RootFolder;

			string fullPath = subfolder1.FullPath;
			Assert.IsTrue(((IFolder)subfolder1).Exists);
			Assert.IsTrue(Directory.Exists(fullPath));
			Assert.AreEqual(initialSubfoldersCount + 1, FixtureRootRepoFolder.SubFolders.Count());

			string customPropertyName = "CustomProperty1";
			string customPropertyValue = "CustomPropertyValue1";
			subfolder1.Properties.SetCustomProperty(customPropertyName, "CustomPropertyValue1");
			subfolder1.Properties.Load();
			Assert.AreEqual(customPropertyValue, subfolder1.Properties.GetCustomProperty(customPropertyName)
				, "Custom property value not persisted");

			IRepositoryWriter writer = subfolder1.GetWriter();
			writer.AllowSubfoldersCreation = true;

			Populate(writer, 100);

			writer.Close();

			subfolder1.Delete(true, true);
			Assert.IsFalse(Directory.Exists(fullPath), "Directory not removed from disk");
			Assert.AreEqual(initialSubfoldersCount, FixtureRootRepoFolder.SubFolders.Count);
		}

		/// <summary>
		///		A test for deleting root folder which has to throw InvalidOperationException
		///</summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void DeleteRootFolderTest()
		{
			Repository.RootFolder.Delete(true, true);
		}

		/// <summary>
		///A test for DetachSubfolder
		///</summary>
		[Test]
		public void DetachAddSubfolderTest()
		{
			string subFolderName = "DetachSubfolderTest";

			int initialSubfoldersCount = FixtureRootRepoFolder.SubFolders.Count;

			IFolder subfolder1 = (IFolder)FixtureRootRepoFolder.CreateSubfolder(subFolderName);

			Assert.AreEqual(initialSubfoldersCount + 1, FixtureRootRepoFolder.SubFolders.Count
				, "Subfolders count not incremented by creating new subfolder");

			string fullPath = subfolder1.FullPath;

			Assert.IsNotNull(FixtureRootRepoFolder.GetSubFolder(subFolderName), "Making sure new folder is found by parent");

			bool detached = FixtureRootRepoFolder.RemoveFromChildList(subfolder1, true);
			Assert.IsTrue(detached, "Detach real subfolder returned failure");
			detached = FixtureRootRepoFolder.RemoveFromChildList(subfolder1, true);
			Assert.IsFalse(detached, "Detach already detached subfolder returned success");

			Assert.IsTrue(subfolder1.IsDetached);
			Assert.IsNull(subfolder1.ParentFolder);

			Assert.IsNull(FixtureRootRepoFolder.GetSubFolder(subFolderName), "Detached subfolder is still found by parent");

			FixtureRootRepoFolder.AddToChildList(subfolder1, true);

			Assert.IsNotNull(FixtureRootRepoFolder.GetSubFolder(subFolderName), "Re-added after detach still not found by parent");
		}

		/// <summary>
		///A test for GetFolderPathKey
		///</summary>
		[Test]
		public void GetFolderPathKeyTest()
		{
			string relativePath = string.Empty;
			string expected = string.Empty;
			string actual;
			actual = RepositoryFolder.GetFolderPathKey(relativePath);
			Assert.AreEqual(expected, actual);

			relativePath = @"\ara\para\";
			expected = "ara/para";
			actual = RepositoryFolder.GetFolderPathKey(relativePath);
			Assert.AreEqual(expected, actual);

			relativePath = @"/ara/para";
			actual = RepositoryFolder.GetFolderPathKey(relativePath);
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void CoderEncryptorConfigTest()
		{
			const string topFolderName = "CoderEncryptorConfigTest";

			IFolder topFolder = (IFolder)FixtureRootRepoFolder.GetSubFolder(topFolderName);
			if (topFolder != null)
			{
				topFolder.Delete(true, true);
			}

			topFolder = (IFolder)FixtureRootRepoFolder.CreateSubfolder(topFolderName);

			var targetFolder = topFolder.GetDescendant("Intermediate/Target", true);

			Assert.AreEqual(string.Empty, targetFolder.Properties.Encryptor);
			Assert.AreEqual(string.Empty, targetFolder.Properties.Compressor);

			const string coderKey = "my-coder";
			const string encKey = "my-encryptor";

			using (var repo1 = GetStandaloneRepository())
			{
				var topFolderInner = repo1.RootFolder.GetDescendant(topFolder.LogicalPath, false);
				Assume.That(null != topFolderInner);

				Assert.Throws<ArgumentException>(() => topFolderInner.Properties.Compressor = coderKey);
				Assert.Throws<ArgumentException>(() => topFolderInner.Properties.Encryptor = encKey);

				repo1.ObjectFactory.AddCompressor(new CoderMock(coderKey), false);
				repo1.ObjectFactory.AddEncryptor(new CoderMock(encKey), false);

				topFolderInner.Properties.Compressor = coderKey;
				topFolderInner.Properties.Encryptor = encKey;
				topFolderInner.Properties.EnableEncryption = true;

				Assert.AreEqual(coderKey, topFolderInner.Properties.Compressor);
				Assert.AreEqual(encKey, topFolderInner.Properties.Encryptor);

				var targetFolderInner = repo1.RootFolder.GetDescendant(targetFolder.LogicalPath, false);
				Assume.That(null != targetFolderInner);

				Assert.AreEqual(coderKey, targetFolderInner.Properties.Compressor);
				Assert.AreEqual(encKey, targetFolderInner.Properties.Encryptor);

			}

			using (var repo1 = GetStandaloneRepository())
			{
				var topFolderInner = repo1.RootFolder.GetDescendant(topFolder.LogicalPath, false);
				Assume.That(null != topFolderInner);

				repo1.ObjectFactory.AddCompressor(new CoderMock(coderKey), false);
				repo1.ObjectFactory.AddEncryptor(new CoderMock(encKey), false);

				var targetFolderInner = repo1.RootFolder.GetDescendant(targetFolder.LogicalPath, false);
				Assume.That(null != targetFolderInner);

				Assert.AreEqual(coderKey, targetFolderInner.Properties.Compressor);
				Assert.AreEqual(encKey, targetFolderInner.Properties.Encryptor);

				using (var writer = targetFolderInner.GetWriter())
				{
					IDataRouter dataRouter = new Mock.NumberedDataRouter(2);
					writer.DataRouter = dataRouter;
					writer.AllowSubfoldersCreation = true;

					IDataItem[] data = RepositoryWriterTest.GetTestData(100, DateTime.Now, 2);

					for (int n = 0; n < 100; ++n)
					{
						writer.Write(data[n]);
					}
					//writer.Flush();
					// flushes data
					writer.Close();

					var enm = targetFolderInner.SubFolders.GetEnumerator();
					Assert.IsTrue(enm.MoveNext());
					var dataFile = ((IFolder)enm.Current).RootDataFolder.FindFirstDataFile(false);
					Assert.IsNotNull(dataFile);

					Assert.IsTrue(dataFile.Path.EndsWith(encKey));

					using (var reader = targetFolderInner.GetReader(DateTime.MinValue, true))
					{
						IDataItem[] dataRead = new IDataItem[data.Length];
						int n = 0;
						while (reader.HasData)
						{
							dataRead[n] = reader.Read().DataItem;
							Assert.AreEqual(data[n], dataRead[n]);
							++n;
						}
						Assert.AreEqual(data.Length, n);
					}

				}
			}
		}
		[Test]
		public void ItemsPerFileTest()
		{
			const string topFolderName = "ItemsPerFileTest";

			IFolder topFolder = CreateNewTestFolder(topFolderName);

			var targetFolder = topFolder.GetDescendant("Intermediate/Target", true);

			Assert.AreEqual(0, targetFolder.Properties.DesiredItemsPerFile);

			const int itemsPerFile = 12;

			using (var repo1 = GetStandaloneRepository())
			{
				var topFolderInner = repo1.RootFolder.GetDescendant(topFolder.LogicalPath, false);
				Assume.That(null != topFolderInner);

				topFolderInner.Properties.DesiredItemsPerFile = itemsPerFile;

				Assert.AreEqual(itemsPerFile, topFolderInner.Properties.DesiredItemsPerFile);

				var targetFolderInner = repo1.RootFolder.GetDescendant(targetFolder.LogicalPath, false);
				Assume.That(null != targetFolderInner);

				Assert.AreEqual(itemsPerFile, targetFolderInner.Properties.DesiredItemsPerFile);

			}

			using (var repo1 = GetStandaloneRepository())
			{
				var topFolderInner = repo1.RootFolder.GetDescendant(topFolder.LogicalPath, false);
				Assume.That(null != topFolderInner);


				var targetFolderInner = repo1.RootFolder.GetDescendant(targetFolder.LogicalPath, false);
				Assume.That(null != targetFolderInner);

				Assert.AreEqual(itemsPerFile, targetFolderInner.Properties.DesiredItemsPerFile);

				using (var writer = targetFolderInner.GetWriter())
				{
					IDataRouter dataRouter = new Mock.NumberedDataRouter(2);
					writer.DataRouter = dataRouter;
					writer.AllowSubfoldersCreation = true;

					IDataItem[] data = RepositoryWriterTest.GetTestData(100, DateTime.Now, 2);

					for (int n = 0; n < 100; ++n)
					{
						writer.Write(data[n]);
					}
					//writer.Flush();
					// flushes data
					writer.Close();

					var enm = targetFolderInner.SubFolders.GetEnumerator();
					Assert.IsTrue(enm.MoveNext());
					var dataFile = ((IFolder)enm.Current).RootDataFolder.FindFirstDataFile(false);
					Assert.IsNotNull(dataFile);


					using (var reader = targetFolderInner.GetReader(DateTime.MinValue, true))
					{
						IDataItem[] dataRead = new IDataItem[data.Length];
						int n = 0;
						while (reader.HasData)
						{
							dataRead[n] = reader.Read().DataItem;
							Assert.AreEqual(data[n], dataRead[n]);
							++n;
						}
						Assert.AreEqual(data.Length, n);
					}

				}
			}
		}

		[Test]
		public void LoadSubfoldersTest()
		{
			const string topFolderName = "LoadSubfoldersTest";
			const string subFolderName1 = "sub1";
			const string subFolderName2 = "sub2";

			IFolder topFolder = CreateNewTestFolder(topFolderName);

			IFolder sub1 = topFolder.CreateSubfolder(subFolderName1);
			IFolder sub2 = topFolder.CreateSubfolder(subFolderName2);

			Assert.AreEqual(2, topFolder.SubFolders.Count);
			Assert.IsNull(sub1.Properties.DisplayName, "Display name must be null after creation");

			IRepository repoStandalone = GetStandaloneRepository();
			IFolder sub1Alt = repoStandalone.RootFolder.GetDescendant(sub1.LogicalPath, false);
			const string newDisplayName1 = "newDisplayName1";
			sub1Alt.Properties.DisplayName = newDisplayName1;

			// check that instances are preserved
			topFolder.LoadSubfolders(true, false, false);
			Assert.AreEqual(2, topFolder.SubFolders.Count);
			Assert.AreSame(sub1, topFolder.GetSubFolder(subFolderName1), "Existing folder instance not preserved");
			Assert.AreSame(sub2, topFolder.GetSubFolder(subFolderName2), "Existing folder instance not preserved");

			Assert.IsNull(sub1.Properties.DisplayName, "Contents refreshed ");

			// check that missing folder is purged

			sub1Alt.Delete(true, true);
			Assert.Throws(Is.InstanceOf<InvalidOperationException>(), () => sub1Alt.Delete(true, true));

			topFolder.LoadSubfolders(true, false, false);
			Assert.AreEqual(1, topFolder.SubFolders.Count);
			Assert.AreSame(sub2, topFolder.GetSubFolder(subFolderName2), "Existing folder instance not preserved");
			Assert.IsTrue(sub1.IsDetached, "Purged subfolder must be detached after reloading");
			
			// check that subfolders are unloaded
			topFolder.UnloadSubfolders();
			Assert.IsFalse(topFolder.SubfoldersLoaded);

			// now check that with reload OFF folders are reloaded
			topFolder.LoadSubfolders(reloadIfLoaded: false, recursive: false, refreshContent: false);
			Assert.AreEqual(1, topFolder.SubFolders.Count);

			// recreating previously deleted folder
			sub1 = topFolder.CreateSubfolder(subFolderName1);
			Assert.IsNull(sub1.Properties.DisplayName, "Display name must be null after creation");
			// note that tis is required to make sure data folders are loaded to check refreshContent option later
			Assert.IsNull(sub1.GetDataFileIterator(false).Seek(DateTime.MinValue));

			repoStandalone.RootFolder.UnloadSubfolders();
			sub1Alt = repoStandalone.RootFolder.GetDescendant(sub1.LogicalPath, false);
			sub1Alt.Properties.Description = newDisplayName1;
			using (var writer = sub1Alt.GetWriter())
			{
				writer.Write(TestDataItem.GetTestItem(2));
			}

			topFolder.LoadSubfolders(reloadIfLoaded: false, recursive: false, refreshContent: false);
			Assert.IsNull(sub1.Properties.Description, "Did not ask for content refreshing");
			Assert.IsNull(sub1.GetDataFileIterator(false).Seek(DateTime.MinValue));

			topFolder.LoadSubfolders(reloadIfLoaded: true, recursive: false, refreshContent: true);
			Assert.AreEqual(newDisplayName1, sub1.Properties.Description, "Did ask for content to be refreshed");
			Assert.IsNotNull(sub1.GetDataFileIterator(false).Seek(DateTime.MinValue));
		}
	}
}
