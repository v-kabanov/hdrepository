using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RepositoryTests.Mock;
using NUnit.Framework;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;

namespace RepositoryTests
{
	[TestFixture]
	public class RepositoryManagerTest : TestBase
	{
		[Test]
		public void TestRegisterUnregisterAccessors()
		{
			Assert.Throws<ArgumentNullException>(() => Repository.RegisterReader(null));
			Assert.Throws<ArgumentNullException>(() => Repository.RegisterWriter(null));

			using (var writer = FixtureRootRepoFolder.GetWriter())
			{
				Assert.IsFalse(Repository.UnRegisterReader(writer), "Writer reported to have been successfully unregistered as reader");

				// must have been registered
				Assert.IsTrue(Repository.UnRegisterWriter(writer));
				Assert.IsFalse(Repository.UnRegisterWriter(writer), "Unregistered writer reported to have been successfully unregistered again");
			}

			using (var reader = FixtureRootRepoFolder.GetReader(DateTime.Now, false))
			{
				Assert.IsFalse(Repository.UnRegisterWriter(reader), "Reader reported to have been successfully unregistered as writer");

				// must have been registered
				Assert.IsTrue(Repository.UnRegisterReader(reader: reader));
				Assert.IsFalse(Repository.UnRegisterReader(reader: reader), "Unregistered reader reported to have been successfully unregistered again");
			}
		}

		public void DisposedStateTrackingTest()
		{
			const string folderName = "DisposedStateTrackingTest";
			var originalDescendant = FixtureRootRepoFolder.CreateSubfolder(folderName);

			using (var repo = GetStandaloneRepository())
			{
				var descendant = repo.RootFolder.GetDescendant(originalDescendant.LogicalPath, false);
				Assert.IsNotNull(descendant);
				Assert.IsTrue(descendant.Exists);

				repo.Dispose();

				Assert.Throws<ObjectDisposedException>(() => repo.IsDataBeingAccessed(folder: descendant, subtree: true));
				Assert.Throws<ObjectDisposedException>(() => repo.RegisterReader(reader: null));
				Assert.Throws<ObjectDisposedException>(() => repo.RegisterWriter(writer: null));
				Assert.Throws<ObjectDisposedException>(() => repo.UnRegisterReader(reader: null));
				Assert.Throws<ObjectDisposedException>(() => repo.UnRegisterWriter(writer: null));

				Assert.Throws<ObjectDisposedException>(() => repo.RootFolder.Refresh());
				Assert.Throws<ObjectDisposedException>(() => descendant.Refresh());

				Assert.Throws<ObjectDisposedException>(() => repo.ObjectFactory.CreateNewFile((IFolder)descendant));
			}
		}
	}
}
