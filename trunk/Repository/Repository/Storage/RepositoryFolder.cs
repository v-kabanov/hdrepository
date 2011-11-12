//-----------------------------------------------------------------------------
// <created>2/12/2010 9:38:14 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using System.Collections.ObjectModel;
using log4net;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	[DebuggerDisplay("{LogicalPath} ({Properties.DisplayName}), Exists = {Exists}")]
	public class RepositoryFolder : IFolder
	{
		#region const declarations --------------------------------------------

		public const string logicalPathSeparator = "/";
		public const string folderConfigFileName = "config.xml";
		public const string dataFolderName = "$d~";
		public static readonly char[] pathSeparators = new char[] { '\\', '/' };

		#endregion const declarations -----------------------------------------

		#region fields --------------------------------------------------------

		private IRepository _repository;
		private IFolder _parentFolder;
		private IHistoricalFoldersTraits _dataFolderTraits;
		private string _folderName;

		/// <summary>
		///		The dictionary by normilised folder name (not path)
		/// </summary>
		private Dictionary<string, IFolder> _subFolders;

		private IDataFolder _dataFoldersVirtualRoot;

		private IFolderProperties _folderConfig;

		private static readonly ILog _log = LogManager.GetLogger("RepositoryFolder");

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		/// <summary>
		///		Constructs root repo folder
		/// </summary>
		/// <param name="repo">
		///		The repository manager
		/// </param>
		/// <param name="folderName">
		/// </param>
		internal RepositoryFolder(IRepository repo)
		{
			CheckHelper.CheckRepositoryNotDisposed(repo);
			Initialize(repo, null, string.Empty);
		}

		/// <summary>
		///		Create non-root folder instance.
		/// </summary>
		/// <param name="parentFolder">
		///		Must not be null.
		/// </param>
		/// <param name="folderName"></param>
		internal RepositoryFolder(IFolder parentFolder, string folderName)
		{
			Check.RequireArgumentNotNull(parentFolder, "parentFolder");
			CheckNotDetached(parentFolder);

			if (string.IsNullOrEmpty(folderName))
			{
				throw new ArgumentException(
					"Cannot use empty folder name for non-root folder"
					, "folderName");
			}
			Initialize(parentFolder.Repository, parentFolder, folderName);
		}

		#endregion constructors -----------------------------------------------

		#region public methods ------------------------------------------------

		/// <summary>
		///		Get stable identifier of a folder from its name. This implementation returns lowered name using invariant
		///		culture. Accepts null value.
		/// </summary>
		/// <param name="relativePath">
		///		The folder relative path which may be a multi-part name. It may be null which is considered same as empty string
		///		and identifying root.
		/// </param>
		/// <returns>
		///		String which may be used as a unique identifier of the [sub]folder
		/// </returns>
		/// <remarks>
		///		If the <paramref name="relativePath"/> is already normalised it is returned as is to avoid keeping duplicates in memory.
		/// </remarks>
		public static string GetFolderPathKey(string relativePath)
		{
			if (null == relativePath)
			{
				return string.Empty;
			}
			string retval = relativePath.ToLowerInvariant().Replace('\\', '/').Trim(pathSeparators);

			if (string.Equals(retval, relativePath, StringComparison.Ordinal))
			{
				// allow the duplicate to be garbage collected
				retval = relativePath;
			}
			return retval;
		}

		public static IFolder CastFolder(IRepositoryFolder folder)
		{
			return CastFolder(folder: folder, paramName: string.Empty);
		}

		public static IFolder CastFolder(IRepositoryFolder folder, string paramName)
		{
			IFolder retval = folder as IFolder;

			Check.DoAssertLambda(retval != null, () => new ArgumentException(
				message: StorageResources.FolderInstanceDoesNotImplementIFolder, paramName: paramName));

			return retval;
		}

		#endregion public methods ---------------------------------------------

		#region protected and internal properties -----------------------------

		internal bool IsRoot
		{
			get
			{
				return _parentFolder == null && _repository != null;
			}
		}

		protected string ParentFolderPath
		{
			get
			{
				return this.IsRoot
					? _repository.RepositoryRoot
					: _parentFolder.FullPath;
			}
		}

		/// <summary>
		///		Convenience property returning repository's file system directory provider
		/// </summary>
		internal IDirectoryProvider DirectoryProvider
		{
			get
			{
				return this.Repository.ObjectFactory.FileSystemProvider.DirectoryProvider;
			}
		}

		/// <summary>
		///		Convenience property returning repository's file system file provider
		/// </summary>
		internal IFileProvider FileProvider
		{
			get
			{
				return this.Repository.ObjectFactory.FileSystemProvider.FileProvider;
			}
		}

		#endregion protected and internal properties --------------------------

		#region private methods -----------------------------------------------

		private void Initialize(IRepository repoManager, IFolder parentFolder, string folderName)
		{
			Check.RequireArgumentNotNull(repoManager, "repoManager");
			CheckHelper.CheckRepositoryNotDisposed(repoManager);

			_repository = repoManager;
			_parentFolder = parentFolder;
			_folderName = folderName;

			_dataFolderTraits = _repository.ObjectFactory.GetHistoricalFoldersTraits(this);
			UpdateLogicalPath();

			_folderConfig = new FolderProperties(this);

			SetUp();

			Check.Ensure(!IsDetached);
		}

		/// <summary>
		///		Set up directory infrastructure. Creates folder directory and root data folder
		///		special directory
		/// </summary>
		private void SetUp()
		{
			_folderConfig.Load();

			CreateDirectoryIfNotExists(this.FullPath);
			CreateDirectoryIfNotExists(this.DataFolderFullPath);

			_dataFoldersVirtualRoot = this.Repository.ObjectFactory.GetDataFolderRoot(this);
		}

		private void CreateDirectoryIfNotExists(string path)
		{
			if (!this.DirectoryProvider.Exists(path))
			{
				this.DirectoryProvider.Create(path);
			}
		}

		/// <summary>
		///		Retrieve first and last leaf level folders
		/// </summary>
		private void RefreshDataFolders()
		{
			_dataFoldersVirtualRoot.Refresh();
		}

		/// <summary>
		///		Create and initialize <see cref="RepositoryFolder"/> instance with the specified name.
		/// </summary>
		/// <remarks>
		///		The directory may not exist on disk.
		/// </remarks>
		private IFolder AddSubFolder(string name)
		{
			EnsureChildFoldersLoaded();

			IFolder newFolder = Repository.ObjectFactory.GetFolder(this, name);
			newFolder.RefreshContent(false);
			AddToChildList(newFolder, false);
			return newFolder;
		}


		/// <summary>
		///		Ensure child folders are loaded; eagerly load child folders; do nothing if already loaded.
		/// </summary>
		/// <param name="reloadIfLoaded">
		///		Reload 
		/// </param>
		private void LoadChildFolders(bool reloadIfLoaded)
		{
			LoadSubfolders(reloadIfLoaded, recursive: false, refreshContent: false);
		}

		protected string GetLogicalPath()
		{
			if (this.IsRoot)
			{
				return logicalPathSeparator;
			}
			else
			{
				string parentPath = this.ParentFolder.LogicalPath;
				StringBuilder bld = new StringBuilder(this.Name.Length + parentPath.Length + 1);
				bld.Append(parentPath);
				if (!parentPath.EndsWith(logicalPathSeparator))
				{
					bld.Append(logicalPathSeparator);
				}
				bld.Append(this.Name);
				return bld.ToString();
			}
		}

		/// <summary>
		///		Having subfolder name remove it from the list of subfolders
		/// </summary>
		private bool DetachSubfolderImpl(string name)
		{
			return _subFolders.Remove(GetFolderPathKey(name));
		}

		private void CheckDataNotAccessed(bool subtree)
		{
			Check.DoAssertLambda(!Repository.IsDataBeingAccessed(this, subtree)
				, () => Exceptions.ConcurrencyExceptionHelper.GetLockedFolderModificationAttempted(this));
		}

		private void LoadSubfoldersLazy(bool reloadIfLoaded)
		{
			if (SubfoldersLoaded && reloadIfLoaded)
			{
				UnloadSubfolders();
			}
		}

		/// <summary>
		///		Throws InvalidOperationException if <paramref name="folder"/> is detached. Otherwise throws ObjectDisposedException if the
		///		repository is disposed; otherwise does nothing
		/// </summary>
		internal static void CheckNotDetached(IRepositoryFolder folder)
		{
			Check.DoCheckOperationValid(!folder.IsDetached, () => StorageResources.FolderIsNotPartOfARepository);
			CheckHelper.CheckRepositoryNotDisposed(folder.Repository);
		}

		/// <summary>
		///		Throws InvalidOperationException if this folder is detached and ObjectDisposedException if repository is disposed.
		/// </summary>
		internal void CheckNotDetached()
		{
			CheckNotDetached(this);
		}

		/// <summary>
		///		Throws ObjectDisposedException if repository is disposed.
		/// </summary>
		internal void CheckNotDisposed()
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
		}

		protected string GetDataFoldersRootPath()
		{
			return Path.Combine(FullPath, DataFolderName);
		}

		#endregion private methods --------------------------------------------

		#region IRepositoryFolder members

		/// <summary>
		///		Move this folder into another folder
		/// </summary>
		/// <param name="newParentFolder">
		///		New parent folder
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void Move(IRepositoryFolder newParentFolder)
		{
			CheckNotDetached();
			Check.DoRequireArgumentNotNull(newParentFolder, "newParentFolder");

			Check.DoCheckOperationValid(!this.IsRoot, StorageResources.CannotMoveRootRepoFolder);
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this.Repository, newParentFolder.Repository);
			Check.DoAssertLambda(newParentFolder.GetSubFolder(this.Name) == null,
				() => Exceptions.FoderAlreadyExistsExceptionHelper.CreateForMove("newParentFolder"));

			IFolder newParent = CastFolder(newParentFolder);
			IFolder oldParent = _parentFolder;

			string oldPath = this.FullPath;

			_parentFolder = RepositoryFolder.CastFolder(newParentFolder);
			string newPath = this.FullPath;

			var scope = StorageTransactionScope.Create(Repository);
			try
			{
				using (scope)
				{
					this.DirectoryProvider.Move(oldPath, newPath);
					oldParent.RemoveFromChildList(this, false);
					_parentFolder.AddToChildList(this, false);
					scope.Complete();
				}
			}
			catch (IOException)
			{
				// restoring previous state
				_parentFolder = oldParent;
				throw;
			}
			catch (Exception)
			{
				// restoring
				if (scope.NoTransaction)
				{
					// manually rolling back
					this.DirectoryProvider.Move(newPath, oldPath);
				}
				_parentFolder = oldParent;
				newParent.RemoveFromChildList(this, false);
				if (oldParent.GetSubFolder(this.Name) == null)
				{
					oldParent.AddToChildList(this, false);
				}
				throw;
			}

			UpdateLogicalPath();

			Check.Ensure(!IsDetached);
		}

		public string Name
		{
			get
			{
				return _folderName;
			}
		}

		/// <exception cref="InvalidOperationException">
		///		The folder is detached
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public string FullPath
		{
			get
			{
				CheckNotDetached();

				if (this.IsRoot)
				{
					return _repository.RepositoryRoot;
				}
				else
				{
					return System.IO.Path.Combine(this.ParentFolderPath, _folderName);
				}
			}
		}

		/// <summary>
		///		Get the path to the folder relative to repo root.
		/// </summary>
		/// <remarks>
		///		Uses RepositoryFolder.logicalPathSeparator ('/'). For virtual root returns path separator. For real top-level folder returns folder name.
		///		Example: "/Folder1/Folder1.1/Folder1.1.1"
		///		Note than the operation is not cheap, consider caching the result.
		/// </remarks>
		public string LogicalPath
		{ get; private set; }

		/// <summary>
		///		Get normalized <see cref="LogicalPath"/>, the string which will be a unique and stable identifier
		///		of the folder in its repository.
		/// </summary>
		/// <remarks>
		///		If the folder is renamed in a way that does not change the target folder (such as when only character
		///		casing changes) the returned value must not change. Folder keys should be case-insensitive, in line
		///		with folder names convention.
		/// </remarks>
		public string FolderKey
		{ get; private set; }

		/// <exception cref="InvalidOperationException">
		///		The folder is detached
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public string DataFolderFullPath
		{
			// gets updated and thus cached in UpdateLogicalPath
			get
			{
				CheckNotDetached();
				if (null != _dataFolderTraits)
				{
					return _dataFolderTraits.RootPath;
				}
				else
				{
					return GetDataFoldersRootPath();
				}
			}
		}

		/// <summary>
		///		Path to the <see cref="RootDataFolder"/> relative to the repository root.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public string DataFolderRootRelativePath
		{
			get
			{
				CheckNotDetached();
				return Path.Combine(LogicalPath, DataFolderName);
			}
		}

		/// <summary>
		///		Get repository instance to which the folder belongs.
		/// </summary>
		public IRepository Repository
		{ get { return _repository; } }

		/// <summary>
		///		Get repository instance to which the folder belongs.
		/// </summary>
		IRepositoryManager IRepositoryFolder.Repository
		{ get { return this.Repository; } }

		/// <summary>
		///		Get parent folder. Returns null for root folder.
		/// </summary>
		IRepositoryFolder IRepositoryFolder.ParentFolder
		{ get { return ParentFolder; } }

		/// <summary>
		///		Get parent folder. Returns null for root folder.
		/// </summary>
		public IFolder ParentFolder
		{ get { return _parentFolder; } }

		public IHistoricalFoldersExplorer DataFoldersExplorer
		{ get { return _dataFolderTraits; } }

		/// <summary>
		///		Get read-only collection of all child folders.
		/// </summary>
		/// <remarks>
		///		By default folders are loaded lazily, so referencing this property may trigger loading child folders
		///		from disk.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		Subfolders are not loaded and the folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public ICollection<IRepositoryFolder> SubFolders
		{
			get
			{
				CheckNotDisposed();
				EnsureChildFoldersLoaded();
				return new Util.ReadOnlyCollection<IFolder, IRepositoryFolder>(_subFolders.Values);
			}
		}

		/// <summary>
		///		Get last item timestamp; no recursion.
		/// </summary>
		/// <returns>
		///		<see cref="DateTime.MinValue"/> if no data
		/// </returns>
		/// <remarks>
		///		The reason to make it and <see cref="FirstTimestamp"/> property rather than a method was
		///		primarily the convenience for data binding. The retrieval should be O(log(n)) but certainly not free.
		///		Should refrain from calling it very often.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public DateTime LastTimestamp
		{
			get
			{
				CheckNotDetached();
				return this.RootDataFolder.GetFirstItemTimestamp(true);
			}
		}

		/// <summary>
		///		Get first item timestamp; no recursion.
		/// </summary>
		/// <returns>
		///		<see cref="DateTime.MaxValue"/> if no data
		/// </returns>
		/// <remarks>
		///		The reason to make it and <see cref="LastTimestamp"/> property rather than a method was
		///		primarily the convenience for data binding. The retrieval should be O(log(n)) but certainly not free.
		///		Should refrain from calling it very often.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public DateTime FirstTimestamp
		{
			get
			{
				CheckNotDetached();
				return this.RootDataFolder.GetFirstItemTimestamp(false);
			}
		}

		/// <summary>
		///		Get data folders tree virtual root
		/// </summary>
		public IDataFolder RootDataFolder
		{
			get { return _dataFoldersVirtualRoot; }
		}

		/// <summary>
		///		Get predefined name of the child folder which contains all data files.
		///		The name cannot be used for repository folders.
		/// </summary>
		public string DataFolderName
		{
			get { return dataFolderName; }
		}

		/// <summary>
		///		Get folder persistent configuration
		/// </summary>
		public IFolderProperties Properties
		{ get { return _folderConfig; } }

		/// <summary>
		///		Get boolean value indicating whether child folders are loaded.
		/// </summary>
		/// <seealso cref="LoadSubfolders(bool, bool, bool)"/>
		/// <seealso cref="UnloadSubfolders"/>
		public bool SubfoldersLoaded
		{
			get { return _subFolders != null; }
		}

		/// <summary>
		///		Create new subfolder.
		/// </summary>
		/// <param name="name">
		///		The name of the new subfolder, It must not be equel to the reserved root data folder
		///		(<see cref="DataFolderName"/>, it must be shorter than <see cref="IDirectoryProvider.MaxPathElementLength"/>
		///		and the resultant full path must be less than the <see cref="IDirectoryProvider.MaxDirectoryPathLengh"/>
		///		and leaving 30 characters for data folders and files.
		/// </param>
		/// <returns>
		///		New repository folder instance ready to receive data.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="name"/> is <see langword="null"/>
		/// </exception>
		/// <exception cref="ArgumentException">
		///		- <paramref name="name"/> is reserved (<see cref="DataFolderName"/>)
		///		- <paramref name="name"/> is too long (<see cref="IDirectoryProvider.MaxPathElementLength"/>)
		///		- a folder with the specified <paramref name="name"/> already exists.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///		Full path to the new directory would be too long (<see cref="IDirectoryProvider.MaxDirectoryPathLengh"/>).
		///		30 characters are reserved for data folders and files, out of 32000 windows can handle.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public IFolder CreateSubfolder(string name)
		{
			Check.RequireArgumentNotNull(name, "name");
			CheckNotDetached();

			_log.InfoFormat("Create subfolder {0} in {1}", name, this.FullPath);

			Check.RequireLambda(name.Length <= this.DirectoryProvider.MaxPathElementLength
				, () => new ArgumentException("Directory name too long"));

			Check.RequireLambda(!string.Equals(name, DataFolderName, StringComparison.InvariantCultureIgnoreCase)
				, () => new ArgumentException("The specified name is reserved"));

			EnsureChildFoldersLoaded();

			string nameKey = GetFolderPathKey(name);
			string newFolderPath = Path.Combine(this.FullPath, name);

			if (_subFolders.ContainsKey(nameKey) || DirectoryProvider.Exists(newFolderPath))
			{
				throw Exceptions.FoderAlreadyExistsExceptionHelper.CreateForNewOrRename("name");
			}

			// reserving 30 characters for data folders hierarchy and file names
			if (newFolderPath.Length - 30 > DirectoryProvider.MaxDirectoryPathLengh)
			{
				throw new PathTooLongException();
			}

			IFolder newFolder;
			// NOTE: transaction is used here because say if writing folder properties file fails with exception the folder does not remain hanging
			// around; it does not affect performance really.
			using (var scope = StorageTransactionScope.Create(Repository))
			{
				newFolder = AddSubFolder(name);
				scope.Complete();
			}
			
			// new folder is likely to be empty, better initialize the empty subfolders collection now
			newFolder.EnsureChildFoldersLoaded();

			return newFolder;
		}

		IRepositoryFolder IRepositoryFolder.CreateSubfolder(string name)
		{
			return CreateSubfolder(name);
		}

		/// <summary>
		///		Get existing subfolder. Returs null if not found.
		/// </summary>
		/// <param name="name">
		///		Subfolder name, case insensitive
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		Subfolders are not loaded and the folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public IFolder GetSubFolder(string name)
		{
			CheckNotDisposed();

			EnsureChildFoldersLoaded();

			IFolder retval = null;
			string key = GetFolderPathKey(name);
			_subFolders.TryGetValue(key, out retval);
			return retval;
		}

		IRepositoryFolder IRepositoryFolder.GetSubFolder(string name)
		{
			return GetSubFolder(name);
		}

		/// <summary>
		///		Get existing or [optionally] create new repo subfolder which is a descendant
		///		of this folder.
		/// </summary>
		/// <param name="relativePath">
		///		Path relative to this folder
		/// </param>
		/// <param name="createIfMissing">
		///		Whether to create missing folders
		/// </param>
		/// <returns>
		///		<code>this</code> if <paramref name="relativePath"/> is empty or <see langword="null"/>;
		///		Existing descendant folder if already existed or <paramref name="createIfMissing"/> is <see langword="true"/>
		///		<see langword="null"/> if descendant with <paramref name="relativePath"/> did not exist and
		///			<paramref name="createIfMissing"/> is <see langword="false"/>
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public IFolder GetDescendant(string relativePath, bool createIfMissing)
		{
			CheckNotDisposed();
			EnsureChildFoldersLoaded();

			IFolder retval = this;
			if (!string.IsNullOrEmpty(relativePath))
			{
				string[] names = relativePath.Split(RepositoryFolder.pathSeparators);
				int n = 0;
				using (var scope = StorageTransactionScope.Create(Repository))
				{
					do
					{
						if (!string.IsNullOrEmpty(names[n]))
						{
							IFolder parentFolder = retval;
							retval = parentFolder.GetSubFolder(names[n]);
							if (retval == null)
							{
								if (createIfMissing)
								{
									retval = parentFolder.CreateSubfolder(names[n]);
								}
							}
						}
						++n;
					}
					while (n < names.Length && retval != null);
					scope.Complete();
				}
			}

			return retval;
		}

		IRepositoryFolder IRepositoryFolder.GetDescendant(string relativePath, bool createIfMissing)
		{
			return GetDescendant(relativePath: relativePath, createIfMissing: createIfMissing);
		}

		/// <summary>
		///		Change the folder name.
		/// </summary>
		/// <param name="newName">
		///		New name.
		/// </param>
		/// <remarks>
		///		Renaming cannot be done while data in any of its descendant is being accessed.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		- this folder is repository root (<see cref="IRepositoryManager.RootFolder"/>)
		///		- this folder is detached
		/// </exception>
		/// <exception cref="bfs.Repository.Exceptions.ConcurrencyException">
		///		 Data in any of this folder's descendants is being accessed.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		A folder with the name <paramref name="newName"/> already exists.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void Rename(string newName)
		{
			CheckNotDetached();
			Check.DoAssertLambda(!this.IsRoot, () => new InvalidOperationException(StorageResources.CannotMoveRootRepoFolder));
			Check.DoAssertLambda(this.ParentFolder.GetSubFolder(newName) == null, () => Exceptions.FoderAlreadyExistsExceptionHelper.CreateForNewOrRename("newName"));

			if (newName != Name)
			{
				lock (this)
				{
					// check if somebody is accessing data anywhere in the subtree
					CheckDataNotAccessed(true);

					string oldName = _folderName;
					string oldPath = this.FullPath;
					_folderName = newName;
					try
					{
						using (var scope = StorageTransactionScope.Create(Repository))
						{
							DirectoryProvider.Move(oldPath, this.FullPath);
							scope.Complete();
						}
					}
					catch (Exception)
					{
						_folderName = oldName;
						throw;
					}
					_parentFolder.OnChildFolderRenamed(this, oldName);
					UpdateLogicalPath();
				}
			}
		}

		/// <summary>
		///		Get reader from this folder or repository subtree (with this folder as its root)
		/// </summary>
		/// <param name="recursive">
		///		Whether to read all subfolders
		/// </param>
		/// <param name="startPosition">
		///		The point in time from which to start reading; inclusive.
		/// </param>
		/// <returns>
		///		A <see cref="IRepositoryReader"/> ready to return data items.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public IRepositoryReader GetReader(DateTime startPosition, bool recursive)
		{
			CheckNotDetached();
			IRepositoryReader retval = _repository.ObjectFactory.GetReader((IRepositoryFolder)null);
			this.AddToReader(retval, recursive);
			retval.Seek(startPosition);
			return retval;
		}

		/// <summary>
		///		Get <see cref="IRepositoryWriter"/> instance pointing to
		///		this directory as target root folder. Data items may go to descendants folders,
		///		<see cref="bfs.Repository.Interfaces.IDataItem.RelativePath"/>
		/// </summary>
		/// <returns>
		///		New instance of <see cref="IRepositoryWriter"/>
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public IRepositoryWriter GetWriter()
		{
			CheckNotDetached();
			Check.Require(this.Exists);
			return _repository.ObjectFactory.GetWriter(this);
		}

		/// <summary>
		///		Synchronise the state of the object with underlying repository; read from disk all subfolders,
		///		and all data folders (recursive)
		/// </summary>
		/// <remarks>
		///		This is same as <code>Refresh(true, false)</code>
		/// </remarks>
		public void Refresh()
		{
			Refresh(true, false);
		}

		/// <summary>
		///		Synchronise the state of the object with underlying repository. Refresh both contents and children/descendants.
		/// </summary>
		/// <param name="recursive">
		///		Whether to refresh all descendants as well.
		/// </param>
		/// <param name="eager">
		///		Whether to load subfolders if not yet loaded. If <see langword="false"/> subfolders collection will be loaded on demand
		///		if not already loaded.
		/// </param>
		/// <remarks>
		///		If <paramref name="recursive"/> is <see langword="false"/> only this folder's structure (data and child folders) is re-read
		///		from disk. The method preserves subfolders collection and instances of subfolders which are not deleted from disk.
		///		Folder metadata (<see cref="Properties"/> is also re-loaded from disk.
		///		Note that if <paramref name="recursive"/> is <see langword="false"/>, child folders' content is not reloaded (for those
		///		child folders which are already loaded).
		///		Otherwise content is reloaded for all descendants as well as this folder.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		/// <seealso cref="LoadSubfolders(bool, bool, bool)"/>
		/// <seealso cref="UnloadSubfolders()"/>
		/// TODO: unit tests
		public void Refresh(bool recursive, bool eager)
		{
			CheckNotDetached();
			_log.DebugFormat("Refresh({0}, {1}) commencing on {2} ({3})", recursive, eager, this.Name, this.FullPath);

			RefreshContent(false);

			if (eager)
			{
				LoadSubfolders(true, recursive, recursive);
			}
			else
			{
				UnloadSubfolders();
			}
		}

		/// <summary>
		///		Refresh ([re-]read) contents of this folder from disk.
		///		(<see cref="SubfoldersLoaded"/> returns <see langword="false"/>), <paramref name="recursive"/> has no effect.
		/// </summary>
		/// <param name="recursive">
		///		Whether to refresh content of <b>loaded</b> descendant folders recursively.
		/// </param>
		/// <remarks>
		///		Contents include metadata (i.e. <see cref="Properties"/>) and data.
		///		The subfolders collection is not reloaded.
		///		That is, externally created subfolders will not be loaded; if child folders are not loaded.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void RefreshContent(bool recursive)
		{
			CheckNotDetached();
			_folderConfig.Load();
			this.RootDataFolder.Refresh();
		}

		/// <summary>
		///		Causes deferred refresh of subfolders. After this call <see cref="SubfoldersLoaded"/> returns <see langword="false"/>
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void UnloadSubfolders()
		{
			CheckNotDisposed();
			if (SubfoldersLoaded)
			{
				foreach (IFolder child in _subFolders.Values)
				{
					child.UnloadSubfolders();
					child.Detach(false);
				}
				_subFolders = null;
			}
			Check.Ensure(!SubfoldersLoaded);
		}

		/// <summary>
		///		Check whether this folder is attached to a repository
		/// </summary>
		public bool IsDetached
		{ get { return (_repository == null); } }

		/// <summary>
		///		Check whether this folder is a descendant of the specified folder; i.e. whether this folder belongs to
		///		the subtree with <paramref name="folder"/> at its root (including <paramref name="folder"/> itself)
		/// </summary>
		/// <param name="folder">
		///		A folder belonging to the same repository
		/// </param>
		/// <returns>
		///		<see langword="true"/> if this folder is a descendant of the <paramref name="folder"/>
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <remarks>
		///		Throws an exception if either folder instance is detached (<see cref="UnloadSubfolders"/>) or belongs to different repositories.
		/// </remarks>
		/// <exception cref="Exceptions.ApplicationException">
		///		The folder's key (<see cref="FolderKey"/>) is inconsistent with that of its ancestor.
		/// </exception>
		/// <exception cref="Exceptions.DifferentRepositoriesException">
		///		<paramref name="folder"/> belongs to a different repository.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public bool IsDescendantOf(IRepositoryFolder folder)
		{
			CheckNotDetached();
			Check.DoRequireArgumentNotNull(folder, "folder");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, this.Repository);

			for (IRepositoryFolder ancestor = this; ancestor != null; ancestor = ancestor.ParentFolder)
			{
				if (object.ReferenceEquals(folder, ancestor))
				{
					Check.DoAssertLambda(this.FolderKey.StartsWith(ancestor.FolderKey)
						, () => new Exceptions.ApplicationException(
							userMessage: StorageResources.FolderKeyInconsistentWithAncestor
							, technicalInfo: string.Format(
@"Ancestor key: {0}
Descendant key: {1}
Descendant's key must start with its ancestor's key."
								, ancestor.FolderKey
								, this.FolderKey
							)
							, inner: null
						)
					);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		///		[Re]loading subfolders, their data and metadata
		/// </summary>
		/// <param name="reloadIfLoaded">
		///		Whether to reload subfolders if they are already loaded. Use <see langword="true"/> to pick up external changes.
		/// </param>
		/// <param name="recursive">
		///		Whether to load all descendants rather than immediate children only
		/// </param>
		/// <param name="refreshContent">
		///		Whether to refresh content of already loaded folders when reloading (those newly loaded will be up to date regardless).
		///		Contents include metadata (i.e. <see cref="Properties"/>) and data. Note that if <paramref name="reloadIfLoaded"/>
		///		is false the content will not be refreshed: it only has an effect when reloading already loaded folders.
		/// </param>
		/// <remarks>
		///		use cases:
		///			- just make sure that CHILD folders are synced eagerly: LoadSubfolders(false, false, false)
		///			- pick up added externally CHILD folders or purge deleted: LoadSubfolders(true, false, false)
		///			- regardless of current state fully load into memory whole subtree to minimise latency during subsequent calls:
		///				LoadSubfolders(true, true, true)
		///		To just make sure that CHILD folders are synced lazily use <see cref="UnloadSubfolders()"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void LoadSubfolders(bool reloadIfLoaded, bool recursive, bool refreshContent)
		{
			CheckNotDetached();

			if (!SubfoldersLoaded || reloadIfLoaded)
			{
				IEnumerable<string> subFolders = this.DirectoryProvider.EnumerateDirectories(this.FullPath);

				if (null == _subFolders)
				{
					_subFolders = new Dictionary<string, IFolder>();
				}

				// the set is initialised with full list of subfolders' keys and will contain no longer existing
				// folders' keys after enumerating subfolders;
				HashSet<string> missingKeys = new HashSet<string>(_subFolders.Keys);

				foreach (string subDir in subFolders)
				{
					string dirName = this.DirectoryProvider.GetLastPathComponent(subDir);
					_log.DebugFormat("FS subfolder found: {0}", dirName);

					if (dirName != dataFolderName)
					{
						IRepositoryFolder existingFolder = GetSubFolder(dirName);
						if (existingFolder == null)
						{
							_log.DebugFormat("{0} is not in the list, adding", dirName);
							existingFolder = AddSubFolder(dirName);
							// constructor does not load subfolders
							string key = GetFolderPathKey(existingFolder.Name);
							missingKeys.Remove(key);
						}
						else
						{
							_log.DebugFormat("{0} is already in the list", dirName);
							missingKeys.Remove(GetFolderPathKey(existingFolder.Name));
							if (refreshContent)
							{
								existingFolder.RefreshContent(false);
							}
						}
						if (recursive)
						{
							existingFolder.LoadSubfolders(reloadIfLoaded, recursive, refreshContent);
						}
					} // if (dirName != dataFolderName)
				} //foreach (string subDir in subFolders)

				// purging subfolder instances no longer existing on disk
				foreach (string missingKey in missingKeys)
				{
					_log.WarnFormat("Removing subfolder {0} which no longer exists on disk", missingKey);
					RemoveFromChildList(GetSubFolder(missingKey), true);
				}
			}
		}

		/// <summary>
		///		Delete this directory.
		/// </summary>
		/// <param name="recursive">
		///		if this parameter is false and there are subfolders
		///		an exception is thrown
		/// </param>
		/// <param name="deleteUnknown">
		///		Whether to remove extra files/folders found in the folder
		/// </param>
		/// <remarks>
		///		Client should not attempt to use the instance of the folder
		///		for data access after calling this method. If this method
		///		throws an exception the client may retry deletion.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void Delete(bool recursive, bool deleteUnknown)
		{
			CheckNotDetached();

			// TODO: string to resources
			Check.DoAssertLambda(!this.IsRoot, () => new InvalidOperationException("Cannot remove root directory"));

			_log.InfoFormat("Deleting folder {0}", this.FullPath);

			if (!this.Exists)
			{
				return;
			}

			EnsureChildFoldersLoaded();

			using (var scope = StorageTransactionScope.Create(Repository))
			{
				if (_subFolders.Count > 0)
				{
					if (!recursive)
					{
						throw Exceptions.FolderContainsSubfoldersExceptionHelper.GetCannotDelete(this);
					}
	
					foreach (string folderKey in _subFolders.Keys.ToArray())
					{
						_subFolders[folderKey].Delete(recursive, deleteUnknown);
						//already has to be deleted
						//_subFolders.Remove(folderKey);
					}
				}
	
				lock (this)
				{
					// all subfolders if any are already deleted
					CheckDataNotAccessed(false);
	
					this._dataFoldersVirtualRoot.Delete(deleteUnknown);
	
					_folderConfig.Delete();
	
					if (deleteUnknown)
					{
						IEnumerable<string> extraFiles = this.DirectoryProvider.EnumerateFiles(this.FullPath);
						foreach (string filePath in extraFiles)
						{
							this.FileProvider.Delete(filePath);
						}
					}
	
					this.DirectoryProvider.Delete(this.FullPath, deleteUnknown);
	
					// removing this folder from the list of child folders of the parent folder
					// parent folder must not be null because this must not be root folder
					bool detached = _parentFolder.RemoveFromChildList(this, false);
					if (!detached)
					{
						_log.ErrorFormat("The parent folder ({0}) failed to detach this folder ({1}) after its deletion"
							, _parentFolder.FullPath, this.Name);
					}
				}
				scope.Complete();
			}

			_folderName = null;
			_parentFolder = null;
			_repository = null;
			_subFolders = null;
			_dataFolderTraits = null;
			_dataFoldersVirtualRoot = null;
		}

		/// <summary>
		///		Add this folder to the target folders list of the specified <paramref name="reader"/>.
		/// </summary>
		/// <param name="reader">
		///		The reader to include this folder into its target list
		/// </param>
		/// <param name="recursive">
		///		Whether to include all subfolders as well
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void AddToReader(IRepositoryReader reader, bool recursive)
		{
			CheckNotDetached();

			if (recursive)
			{
				EnsureChildFoldersLoaded();

				foreach (IRepositoryFolder childFolder in _subFolders.Values)
				{
					childFolder.AddToReader(reader, recursive);
				}
			}
			reader.AddFolder(this);
		}

		/// <summary>
		///		Get first item timestamp. <seealso cref="LastTimestamp"/>, <seealso cref="FirstTimestamp"/>.
		/// </summary>
		/// <param name="recursive">
		///		Whether to include all subfolders
		/// </param>
		/// <param name="fromEnd">
		/// </param>
		/// <returns>
		///		<see cref="DateTime.MinValue"/> - there's no data and <paramref name="fromEnd"/> is <see langword="true"/>
		///		<see cref="DateTime.MaxValue"/> - there's no data and <paramref name="fromEnd"/> is <see langword="false"/>
		///		Otherwise timestamp of first existing data item.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public DateTime GetFirstItemTimestamp(bool recursive, bool fromEnd)
		{
			CheckNotDetached();

			if (recursive)
			{
				EnsureChildFoldersLoaded();
			}
			
			if (!recursive || _subFolders.Count == 0)
			{
				return this.RootDataFolder.GetFirstItemTimestamp(fromEnd);
			}
			else
			{
				DateTime firstOwnTimestamp = this.RootDataFolder.GetFirstItemTimestamp(fromEnd);
				Func<IRepositoryFolder, DateTime> getFirstTimestampFunc = (f) => f.GetFirstItemTimestamp(recursive, fromEnd);
				DateTime firstDescendantsTimestamp;

				if (fromEnd)
				{
					firstDescendantsTimestamp = this.SubFolders.Max<IRepositoryFolder, DateTime>(getFirstTimestampFunc);
				}
				else
				{
					firstDescendantsTimestamp = this.SubFolders.Min<IRepositoryFolder, DateTime>(getFirstTimestampFunc);
				}

				return CollectionUtils.GetOneByComparison<DateTime>(firstOwnTimestamp, firstDescendantsTimestamp, fromEnd);
			}
		}

		/// <summary>
		///		Update location in the repository.
		/// </summary>
		/// <remarks>
		///		After an ancestor folder is renamed or moved cached logical path and folder key must be synchronised.
		///		Also, the data folders full path is cached in data folders traits and needs updating.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void UpdateLogicalPath()
		{
			CheckNotDetached();

			LogicalPath = GetLogicalPath();
			FolderKey = GetFolderPathKey(LogicalPath);
			_dataFolderTraits.RootPath = GetDataFoldersRootPath();
			if (SubfoldersLoaded)
			{
				foreach (IFolder childFolder in _subFolders.Values)
				{
					childFolder.UpdateLogicalPath();
				}
			}
		}

		/// <summary>
		///		To be called by immediate child folder after being renamed
		/// </summary>
		/// <param name="folder">
		///		Child folder just renamed, <see cref="Name"/> returning new name
		/// </param>
		/// <param name="oldName">
		///		The old folder name
		/// </param>
		public void OnChildFolderRenamed(IFolder folder, string oldName)
		{
			IRepositoryFolder registered = GetSubFolder(oldName);
			// TODO: string to resources
			Check.DoAssertLambda(object.ReferenceEquals(registered, folder)
				, () => new ArgumentException("The supplied folder and/or old name are not recognised"));
			DetachSubfolderImpl(oldName);
			AddToChildList(folder, false);
		}

		#endregion IRepositoryFolder members

		/// <summary>
		///		Detach this folder instance from the repository.
		/// </summary>
		/// <param name="removeFromParentsList">
		///		Whether to remove this folder from its parent's list of child folders.
		/// </param>
		/// <remarks>
		///		The intent is to preserve state to an extent that the instance can be re-attached with minimal overhead.
		///		Subtree remains unchanged but folders in it will not function normally. This method should be used
		///		only internally and the subtree should not be left in the abnormal state after the client call.
		///		After this method completes successfully <see cref="IsDetached"/> will return <see langword="true"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is repository root (<see cref="IRepositoryManager.RootFolder"/>) or already detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void Detach(bool removeFromParentsList)
		{
			CheckNotDetached();
			Check.DoCheckOperationValid(!IsRoot, "Cannot detach root folder.");

			if (removeFromParentsList)
			{
				_parentFolder.RemoveFromChildList(this, false);
			}

			_parentFolder = null;
			_repository = null;

			Check.Ensure(IsDetached);
		}

		/// <summary>
		///		Attach this detached folder to a repository.
		/// </summary>
		/// <param name="parent">
		///		The [parent] folder under which to attach this folder.
		/// </param>
		/// <param name="addToParentsList">
		///		Whether to add this folder to parent's list of child folders.
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		This folder is repository root (<see cref="IRepositoryManager.RootFolder"/>) or not detached.
		/// </exception>
		public void Attach(IFolder parent, bool addToParentsList)
		{
			Check.DoCheckOperationValid(IsDetached && !IsRoot, "Cannot attach root or already attached folder.");

			if (addToParentsList)
			{
				parent.AddToChildList(this, false);
			}

			_parentFolder = parent;
			_repository = _parentFolder.Repository;

			Check.Ensure(!IsDetached);
		}

		/// <summary>
		///		Add a folder to the list of this folder's child folders.
		/// </summary>
		/// <param name="child">
		///		The folder to add to the list.
		/// </param>
		/// <param name="attachChild">
		///		Whether to attach <paramref name="child"/> to its parent (this folder) afterwards (by calling <see cref="Attach(IFolder, bool)"/>)
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		This folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		public void AddToChildList(IFolder child, bool attachChild)
		{
			CheckNotDetached();

			EnsureChildFoldersLoaded();
			_subFolders.Add(GetFolderPathKey(child.Name), child);

			if (attachChild)
			{
				child.Attach(this, false);
			}
		}

		/// <summary>
		///		Remove a folder from the list of this folder's child folders.
		/// </summary>
		/// <param name="child">
		///		The folder to remove from the list.
		/// </param>
		/// <param name="detachChild">
		///		Whether to detach <paramref name="child"/> from its parent (this folder) afterwards (by calling <see cref="Detach(bool)"/>)
		/// </param>
		/// <returns>
		///		<see langword="true"/> - the <paramref name="child"/> was successfully removed from the list
		///		<see langword="false"/> - the <paramref name="child"/> was not found in the list
		/// </returns>
		/// <remarks>
		///		
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///		This <paramref name="child"/> object is a null reference.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		A folder with the same name as that of <paramref name="child"/> was found but it was not the same instance.
		/// </exception>
		public bool RemoveFromChildList(IFolder child, bool detachChild)
		{
			Check.DoRequireArgumentNotNull(child, "child");

			EnsureChildFoldersLoaded();

			IFolder registeredFolder;
			bool retval = false;

			string nameKey = GetFolderPathKey(child.Name);

			if (_subFolders.TryGetValue(nameKey, out registeredFolder))
			{
				Check.DoAssertLambda(object.ReferenceEquals(child, registeredFolder), () => new ArgumentException(StorageResources.ChildFolderInstanceByNameMismatch));

				retval = _subFolders.Remove(nameKey);

				if (detachChild)
				{
					child.Detach(false);
				}
			}

			return retval;
		}

		/// <summary>
		///		Ensure child folders are loaded; eagerly load child folders; do nothing if already loaded.
		/// </summary>
		public void EnsureChildFoldersLoaded()
		{
			LoadChildFolders(false);
		}

		/// <summary>
		///		Get iterator over contained data files.
		/// </summary>
		/// <param name="backwards">
		///		Whether to initialize iterator in backward direction (chronologically).
		/// </param>
		/// <returns>
		///		New iterator.
		/// </returns>
		/// <remarks>
		///		This is a convenience method calling
		///		<see cref="IObjectFactory.GetDataFileIterator(IRepositoryFolder, bool)"/>
		/// </remarks>
		public IDataFileIterator GetDataFileIterator(bool backwards)
		{
			return Repository.ObjectFactory.GetDataFileIterator(this, backwards);
		}

		/// <summary>
		///		Get <see langword="bool" /> value indicating whether the underlyinig file system directory exists.
		/// </summary>
		public bool Exists
		{
			get
			{
				return this.DirectoryProvider.Exists(this.FullPath);
			}
		}
	}

}
