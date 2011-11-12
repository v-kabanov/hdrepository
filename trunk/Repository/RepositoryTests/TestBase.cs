/*
 * User: vasily
 * Date: 20/03/2011
 * Time: 11:31 AM
 * 
 */

using System;
using bfs.Repository.Storage;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Interfaces;

namespace RepositoryTests
{
	/// <summary>
	/// Description of TestBase.
	/// </summary>
	public class TestBase : IDisposable
	{
		public TestBase()
		{
			string name = this.GetType().Name;
			FixtureRootRepoFolder = Repository.RootFolder.GetSubFolder(name);
			if (null == FixtureRootRepoFolder)
			{
				FixtureRootRepoFolder = Repository.RootFolder.CreateSubfolder(this.GetType().Name);
			}
		}

		protected static RepositoryManager Repository
		{ get { return RepositorySetUpFixture.Manager; } }

		protected IFileSystemProvider FileSystemProvider
		{ get { return Repository.ObjectFactory.FileSystemProvider; } }

		/// <summary>
		///		Get ambient storage transaction.
		/// </summary>
		protected IFileSystemTransaction AmbientTransaction
		{ get { return FileSystemProvider.AmbientTransaction; } }

		public static bool EnvironmentSupportsTransactions
		{
			get
			{
				return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.CompareTo(new Version(6, 0)) >= 0;
			}
		}

		public IFolder FixtureRootRepoFolder
		{ get; private set; }

		//------------------------------------------------------------------

		/// <summary>
		///		Get writer preconfigured to track unsaved changes and allow subfolders creation
		/// </summary>
		/// <param name="targetFolder"></param>
		/// <param name="desiredFileSize"></param>
		/// <param name="router"></param>
		/// <returns></returns>
		public IRepositoryWriter GetWriter(IRepositoryFolder targetFolder, IDataRouter router)
		{
			IRepositoryWriter writer = targetFolder.GetWriter();
			writer.TrackUnsavedItems = true;

			if (router != null)
			{
				writer.DataRouter = router;
			}

			writer.AllowSubfoldersCreation = true;

			return writer;
		}

		/// <summary>
		///		Get writer preconfigured to track unsaved changes and allow subfolders creation and targeting
		///		child folder named <paramref name="targetFolderName"/> of <see cref="FixtureRootRepoFolder"/>
		/// </summary>
		public IRepositoryWriter GetStandaloneWriter(string targetFolderName, int desiredFileSize, IDataRouter router)
		{
			IRepositoryManager manager = GetStandaloneRepository();

			manager.Settings.StorageTransactionSettings = StorageTransactionSettings.RequireTransactions | StorageTransactionSettings.DisallowJoiningAmbientManaged;

			IRepositoryFolder targetFolder = manager.RootFolder.GetDescendant(FixtureRootRepoFolder.LogicalPath, false).GetSubFolder(targetFolderName);
			targetFolder.Properties.DesiredItemsPerFile = desiredFileSize;

			return GetWriter(targetFolder: targetFolder, router: router);
		}

		/// <summary>
		///		Get new repository instance targetting the same root as <see cref="Repository"/>.
		/// </summary>
		/// <returns></returns>
		public IRepository GetStandaloneRepository()
		{
			return new RepositoryManager(Repository.RepositoryRoot);
		}

		public IFolder CreateNewTestFolder(string folderName)
		{
			IFolder retval = FixtureRootRepoFolder.GetSubFolder(folderName);
			if (retval != null)
			{
				retval.Delete(true, true);
			}

			retval = FixtureRootRepoFolder.CreateSubfolder(folderName);
			return retval;
		}

		#region IDisposable implementation
		
		public virtual void Dispose ()
		{
			// this is to be able to execute all tests in "using" code block
		}
		
		#endregion
	}
}
