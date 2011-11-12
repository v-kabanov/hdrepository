//-----------------------------------------------------------------------------
// <created>2/19/2010 11:30:13 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using log4net;

using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Lazy loading data folder accessor
	/// </summary>
	[DebuggerDisplay("{RelativePath}, Exists = {Exists}, IsLeaf = {IsLeafFolder}")]
	public class DataFolder : IDataFolder
	{
		// There's no way to unload subfolders here; thinking it may not be necessary because means to unload repo folders are implemented
		// and it's better to unload the whole repo folder in an unlikely event that the loaded metadata will represent too much of a memory consumption

		#region fields --------------------------------------------------------

		private static readonly ILog _log = LogManager.GetLogger(typeof(DataFolder).Name);

		private IFolder _repoFolder;
		private IIndexedRangeCollection<DateTime, IDataFolder> _childDataFolders;
		private IDataFolder _parentDataFolder;
		private IRepoFileContainerDescriptor _folderDescriptor;
		private IRepoFileContainerBrowser _dataFileBrowser;

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		/// <summary>
		///		Create new instance of virtual data folder (root)
		/// </summary>
		/// <param name="repoFolder">
		/// </param>
		/// <remarks>
		///		The reported level will be equal to _repoFolder.DataFoldersExplorer.LevelCount
		/// </remarks>
		public DataFolder(IFolder repoFolder)
		{
			_repoFolder = repoFolder;
		}

		/// <summary>
		///		Real existing data folder
		/// </summary>
		/// <param name="parentDataFolder"></param>
		/// <param name="folderDescriptor"></param>
		public DataFolder(DataFolder parentDataFolder, IRepoFileContainerDescriptor folderDescriptor)
		{
			_parentDataFolder = parentDataFolder;
			_folderDescriptor = folderDescriptor;
			_repoFolder = parentDataFolder.RepoFolder;
		}

		#endregion constructors -----------------------------------------------

		#region IDataFolder members

		#region IRepoFileContainerDescriptor Members

		/// <summary>
		///		Get the left time boundary (inclusive) of the data items in the asoociated
		///		data folder. For virtual root DateTime.MinValue is returned.
		/// </summary>
		public DateTime Start
		{
			get
			{
				if (this.IsVirtualRoot)
				{
					return DateTime.MinValue;
				}
				return _folderDescriptor.Start;
			}
		}

		/// <summary>
		///		Get the right time boundary (exclusive) of the data items in the asoociated
		///		data folder. For virtual root DateTime.MaxValue is returned.
		/// </summary>
		public DateTime End
		{
			get
			{
				if (this.IsVirtualRoot)
				{
					return DateTime.MaxValue;
				}
				return _folderDescriptor.End;
			}
		}

		/// <summary>
		///		Path relative to the data folders root as indicated by contained IRepoFileContainerDescriptor.
		/// </summary>
		public string RelativePath
		{
			get
			{
				if (this.IsVirtualRoot)
				{
					return string.Empty;
				}
				return _folderDescriptor.RelativePath;
			}
		}

		/// <summary>
		///		Get the data folder level (<see cref="bfs.Repository.Interfaces.IRepoFileContainerDescriptor.Level"/>.
		///		For virtual root it will be equal to LevelCount.
		/// </summary>
		public int Level
		{
			get
			{
				if (this.IsVirtualRoot)
				{
					return _repoFolder.DataFoldersExplorer.LevelCount;
				}
				return _folderDescriptor.Level;
			}
		}

		#endregion  IRepoFileContainerDescriptor Members

		#region properties

		/// <summary>
		///		Get boolean flag indicationg whether the folder
		///		is on leaf level (contains data files)
		/// </summary>
		public bool IsLeafFolder
		{
			get
			{
				return this.Level == 0;
			}
		}

		/// <summary>
		///		Get full path to the underlying folder.
		/// </summary>
		public string FullPath
		{
			get
			{
				return Path.Combine(_repoFolder.DataFolderFullPath, this.RelativePath);
			}
		}

		/// <summary>
		///		Get boolean flag indicating whether this is a leaf folder and
		///		the data files have been loaded. <see cref="LoadFiles"/>
		/// </summary>
		public bool DataFilesLoaded
		{
			get
			{
				return this.IsLeafFolder && _dataFileBrowser != null;
			}
		}

		/// <summary>
		///		Get boolean flag indicating that this data folder is not a leaf folder
		///		and the immediate subfolders have been loaded. <see cref="LoadSubFolders"/>
		/// </summary>
		public bool SubfoldersLoaded
		{
			get
			{
				return !this.IsLeafFolder && _childDataFolders != null;
			}
		}

		/// <summary>
		///		The object represents virtual root (<see cref="bfs.Repository.Interfaces.IRepositoryFolder.DataFolderFullPath"/>)
		///		- the container for all top-level data folders. 
		/// </summary>
		public bool IsVirtualRoot
		{
			get
			{
				return _folderDescriptor == null;
			}
		}

		/// <summary>
		///		Get the containing repository folder instance.
		/// </summary>
		public IFolder RepoFolder
		{ get { return _repoFolder; } }

		/// <summary>
		///		Get data file browser for this leaf folder.
		///		If this is not a leaf folder an InvalidOperationException is thrown.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		This is not a leaf folder
		/// </exception>
		public IRepoFileContainerBrowser DataFileBrowser
		{
			get
			{
				if (!this.DataFilesLoaded)
				{
					LoadFiles(false);
				}
				return _dataFileBrowser;
			}
		}

		/// <summary>
		///		Whether the target directory exist on disk.
		/// </summary>
		public bool Exists
		{
			get
			{
				return this.DirectoryProvider.Exists(this.FullPath);
			}
		}

		/// <summary>
		///		Get parent data folder.
		/// </summary>
		/// <remarks>
		///		This will return <see langword="null" /> if <see cref="IsVirtualRoot"/> returns <see langword="true" />.
		/// </remarks>
		public IDataFolder ParentDataFolder
		{
			get
			{
				return _parentDataFolder;
			}
		}

		/// <summary>
		///		Get last (the newest) child data folder; returns <see langword="null"/> if none
		/// </summary>
		public IDataFolder LastChild
		{
			get
			{
				Check.RequireLambda(!IsLeafFolder, () => StorageResources.LeafFoldersCannotHaveChildFolders);
				return _childDataFolders.GetMax();
			}
		}

		/// <summary>
		///		Get first (the oldest) child data folder; returns <see langword="null"/> if none
		/// </summary>
		public IDataFolder FirstChild
		{
			get
			{
				Check.RequireLambda(!IsLeafFolder, () => StorageResources.LeafFoldersCannotHaveChildFolders);
				return _childDataFolders.GetMin();
			}
		}

		/// <summary>
		///		Get path relative to the repository root.
		/// </summary>
		public string PathInRepository
		{
			get
			{
				return Path.Combine(_repoFolder.DataFolderRootRelativePath, RelativePath);
			}
		}

		#endregion properties

		#region methods

		/// <summary>
		///		Read the list of immediately contained data files
		///		from disk.
		///		Must not be called for non-leaf folders.
		/// </summary>
		/// <param name="reloadIfLoaded">
		///		if <see langword="false"/> and files were loaded before
		///		the previous result is kept
		///		otherwise file list is read from disk
		/// </param>
		public void LoadFiles(bool reloadIfLoaded)
		{
			Util.Check.RequireLambda(this.Level == 0
				, () => new InvalidOperationException("Only leaf level can contain files"));

			if (_log.IsDebugEnabled)
			{
				_log.DebugFormat("LoadFiles {0} commencing", this.PathInRepository);
			}

			bool browserCreated = false;
			if (!this.DataFilesLoaded)
			{
				_log.Debug("Creating file browser");
				_dataFileBrowser = _repoFolder.Repository.ObjectFactory.GetDataFileBrowser(
					_repoFolder, this);
				browserCreated = true;
			}

			if (reloadIfLoaded || browserCreated)
			{
				_log.Debug("Refreshing file browser");
				_dataFileBrowser.Refresh();
			}
		}

		/// <summary>
		///		Read the list of immediately contained subfolders
		///		from disk.
		///		For leaf folders does nothing.
		/// </summary>
		/// <param name="reloadIfLoaded"></param>
		public void LoadSubFolders(bool reloadIfLoaded)
		{
			Invariant();

			if (!this.IsLeafFolder)
			{
				if (!this.SubfoldersLoaded || reloadIfLoaded)
				{
					_log.DebugFormat("LoadSubFolders: Scanning <{0}> for subfolders", RelativePath);

					// in the year-month scheme this will be year or root data folder
					// i.e. _repoFolder.DataFoldersExplorer will be parent at most
					// note that current implementation requires this to be a child
					// how will it be affected with the addition of another level?
					// it will not be valid; need to make it more explicit; there's coupling between DataFolder and folder traits
					// or, if I add more levels I will need to lift the limitation in DataFoldersExplorer implementation
					List<IRepoFileContainerDescriptor> subFolders = _repoFolder.DataFoldersExplorer.Enumerate(_folderDescriptor, this.Level - 1);

					IIndexedRangeCollection<DateTime, IDataFolder> oldList = _childDataFolders;
					_childDataFolders = new IndexedRangeTreeDictionary<DateTime, IDataFolder>(f => f.Start, f => f.End);

					foreach (IRepoFileContainerDescriptor descr in subFolders)
					{
						_log.DebugFormat("Found subfolder {0}", descr.ToString());
						IDataFolder newSubFolder = null;
						if (null != oldList)
						{
							newSubFolder = oldList.GetExact(descr.Start);
						}
						if (null == newSubFolder)
						{
							newSubFolder = new DataFolder(this, descr);
							_log.Debug("The found subfolder was not in the list");
						}
						else
						{
							_log.Debug("The subfolder was already in the list");
						}
						_childDataFolders.Add(newSubFolder);
					}
				}
			}
			else
			{
				_log.Debug("LoadSubFolders called for leaf folder");
			}
		}

		/// <summary>
		///		Free memory
		/// </summary>
		public void UnloadFiles()
		{
			_dataFileBrowser = null;
		}

		/// <summary>
		///		Find first (chronologically) data file in the subtree.
		/// </summary>
		/// <param name="fromEnd">
		///		<see langword="true"/> - search for last (chronologically) data file
		///		<see langword="false"/> - search for first (chronologically) data file
		/// </param>
		/// <remarks>
		///		To reload use <see cref="Refresh()"/>
		/// </remarks>
		public IRepositoryFile FindFirstDataFile(bool fromEnd)
		{
			_log.DebugFormat("Scanning for first data file, fromEnd = {0}", fromEnd);

			DateTime fromTime = fromEnd ? DateTime.MaxValue : DateTime.MinValue;

			return Seek(fromTime, fromEnd);
		}

		/// <summary>
		///		Get sequence of child folders which are covering, after or before the specified <paramref name="seekTime"/>,
		///		depending on direction (<paramref name="backwards"/>), inclusive of <paramref name="seekTime"/>.
		/// </summary>
		/// <param name="seekTime">
		///		Seek timestamp, inclusive.
		/// </param>
		/// <param name="backwards">
		///		Whether to go backwards (<see langword="true"/>) or forward (<see langword="false"/>) from the <paramref name="seekTime"/>.
		/// </param>
		/// <returns>
		///		<see cref="IEnumerable&lt;IDataFolder&gt;"/>
		/// </returns>
		public IEnumerable<IDataFolder> GetSubfolders(DateTime seekTime, bool backwards)
		{
			return GetSubfoldersSequence(seekTime: seekTime, backwards: backwards);
		}

		/// <summary>
		///		Get sequence of child folders which are covering, after or before the specified <paramref name="seekTime"/>,
		///		depending on direction (<paramref name="backwards"/>), inclusive of <paramref name="seekTime"/>.
		/// </summary>
		/// <param name="seekTime">
		///		Seek timestamp, inclusive
		/// </param>
		/// <param name="backwards">
		///		Whether to go backwards (<see langword="true"/>) or forward (<see langword="false"/>) from the <paramref name="seekTime"/>
		/// </param>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;IDataFolder&gt;"/>
		/// </returns>
		internal Util.IDirectedEnumerable<IDataFolder> GetSubfoldersSequence(DateTime seekTime, bool backwards)
		{
			Check.Require(!this.IsLeafFolder);

			LoadSubFolders(false);

			if (backwards)
			{
				// need to adjust to make it inclusive
				seekTime = seekTime.AddTicks(-1);
			}
			else
			{
				seekTime = _repoFolder.DataFoldersExplorer.GetRangeStart(seekTime, this.Level - 1);
			}

			return _childDataFolders.Select(seekTime, backwards);
		}

		/// <summary>
		///		Find first leaf data [sub]folder and data file in it containing items dated at, earlier or later than the
		///		<paramref name="seekTime"/>
		/// </summary>
		/// <param name="seekTime">
		///		Specifies the start of date-time range (inclusive) to find data in
		/// </param>
		/// <param name="backwards">
		///		To which side from <paramref name="seekTime"/> to look for data
		/// </param>
		/// <returns>
		///		First (according to <paramref name="seekTime"/>) data file containing the required data;
		///		<see langword="null"/> if none found
		/// </returns>
		public IRepositoryFile Seek(DateTime seekTime, bool backwards)
		{
			_log.DebugFormat("Seeking data folder for reader, descr: {0}"
				, this.IsVirtualRoot ? "Virtual Root" : this._folderDescriptor.ToString());

			IRepositoryFileName dataFile = null;
			IRepositoryFile retval = null;

			if (IsLeafFolder)
			{
				LoadFiles(false);
				dataFile = _dataFileBrowser.GetFile(seekTime, backwards);
				if (null != dataFile)
				{
					_log.DebugFormat("Seek found file: {0}", dataFile.FileName);
					retval = GetContainedRepositoryFile(dataFile);
				}
			}
			else
			{
				LoadSubFolders(false);

				IDirectedEnumerable<IDataFolder> sequence = GetSubfoldersSequence(seekTime, backwards);

				for (
					IEnumerator<IDataFolder> scanner = sequence.GetEnumerator();
					scanner.MoveNext() && null == retval;
					)
				{
					retval = scanner.Current.Seek(seekTime, backwards);
				}
			}
			return retval;
		}

		/// <summary>
		///		Find data file owning/covering the <paramref name="seekTime"/>
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		Existing <see cref="IRepositoryFile"/> covering by its date-time range the <paramref name="seekTime"/>.
		///		<see langword="null"/> if such file does not exist.
		/// </returns>
		public IRepositoryFile SeekOwner(DateTime seekTime)
		{
			IRepositoryFile file = Seek(seekTime, false);
			if (file != null && file.Name.FirstItemTimestamp > seekTime)
			{
				// file is not covering the timestamp
				file = null;
			}
			return file;
		}

		/// <summary>
		///		Get child data file next to the specified data file.
		/// </summary>
		/// <param name="file">
		///		The data file which neighbour to find
		/// </param>
		/// <param name="backwards">
		///		The direction in which to go from <paramref name="file"/>:
		///		<see langword="true"/>: to the past
		///		<see langword="false"/>: to the future
		/// </param>
		/// <returns>
		///		Existing data file which is contained in this data folder or <see langword="null"/> if the sought data file
		///		does not exist.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		///		This is not a leaf folder
		/// </exception>
		public IRepositoryFileName GetNextDataFile(IRepositoryFileName file, bool backwards)
		{
			Check.RequireLambda(IsLeafFolder, () => new InvalidOperationException("Operation not supported on non-leaf folders"));

			DateTime seekTime = backwards ? file.FirstItemTimestamp.AddTicks(-1) : file.End;

			return _dataFileBrowser.GetFile(seekTime, backwards);
		}

		/// <summary>
		///		Delayed load
		/// </summary>
		public void Refresh()
		{
			Invariant();

			_dataFileBrowser = null;
			_childDataFolders = null;
		}

		/// <summary>
		///		Find existing leaf data folder and [optionally] create if it is
		///		missing. Primarily intended for use by writers.
		/// </summary>
		/// <param name="itemTimestamp">
		///		The timestamp of an item for which to find the existing
		///		or create new leaf folder
		/// </param>
		/// <returns>
		///		Existing leaf folder
		///		null if the folder is missing and <paramref name="createIfMissing"/>
		///		is <see langword="false"/>
		/// </returns>
		public IDataFolder GetLeafFolder(DateTime itemTimestamp, bool createIfMissing)
		{
			IDataFolder retval = null;

			if (this.IsLeafFolder)
			{
				if (this.Start <= itemTimestamp && this.End > itemTimestamp)
				{
					retval = this;
				}
			}
			else // if (this.IsLeafFolder)
			{
				LoadSubFolders(false);
				retval = _childDataFolders.GetOwner(itemTimestamp);
				if (retval == null && createIfMissing)
				{
					// creating target subfolder; this will be a child folder of this data folder
					IRepoFileContainerDescriptor descr = _repoFolder.DataFoldersExplorer.GetTargetFolder(
						itemTimestamp, this.Level - 1);

					retval = new DataFolder(this, descr);

					retval.EnsureDirectoryExists();

					_childDataFolders.Add(retval);
				}
				if (retval != null)
				{
					if (retval.Level > 0)
					{
						// not a leaf folder yet - drilling down all the way
						retval = retval.GetLeafFolder(itemTimestamp, createIfMissing);
					}
				}
			}

			Check.Ensure(null != retval || !createIfMissing);
			Check.Ensure(null == retval || retval.IsLeafFolder);
			Check.Ensure(null == retval || (retval.Start <= itemTimestamp && retval.End > itemTimestamp));

			return retval;
		}

		/// <summary>
		///		Ensure the underlying file system directory exists
		/// </summary>
		public void EnsureDirectoryExists()
		{
			if (!this.IsVirtualRoot && !this.Exists)
			{
				this.ParentDataFolder.EnsureDirectoryExists();
				// note that IFileProvider.Create creates only last directory, no recursion
				this.DirectoryProvider.Create(this.FullPath);
			}
		}

		/// <summary>
		///		Delete data folder from disk, always recursive
		/// </summary>
		/// <param name="deleteUnknown">
		///		Delete any extra, externally created files and folders found
		/// </param>
		public void Delete(bool deleteUnknown)
		{
			if (!this.IsLeafFolder)
			{
				LoadSubFolders(false);

				foreach (IDataFolder childFolder in _childDataFolders.ToArray())
				{
					childFolder.Delete(deleteUnknown);
					_childDataFolders.Remove(childFolder);
				}
			}
			else
			{
				try
				{
					foreach (IRepositoryFileName file in _dataFileBrowser.Files)
					{
						string dataFilePath = GetDataFilePath(file);
						_log.InfoFormat("Deleting data file {0}", dataFilePath);
						FileProvider.Delete(dataFilePath);
					}
				}
				catch (IOException)
				{
					// so that next time it can be re-synced
					UnloadFiles();
					throw;
				}
			}

			this.DirectoryProvider.Delete(this.FullPath, deleteUnknown);
		}

		/// <summary>
		///		Get timestamp of the first (chronologically) data item in the subtree with this data folder at its root.
		/// </summary>
		/// <param name="fromEnd">
		///		The direction of search.
		///		<see langword="true"/> - search for last (chronologically) data item
		///		<see langword="false"/> - search for first (chronologically) data item
		/// </param>
		/// <returns>
		///		<see cref="DateTime.MinValue"/> - there's no data and <paramref name="fromEnd"/> is <see langword="true"/>
		///		<see cref="DateTime.MaxValue"/> - there's no data and <paramref name="fromEnd"/> is <see langword="false"/>
		///		Otherwise timestamp of first existing data item.
		/// </returns>
		public DateTime GetFirstItemTimestamp(bool fromEnd)
		{
			IRepositoryFile firstFile = FindFirstDataFile(fromEnd);
			if (null == firstFile)
			{
				return fromEnd ? DateTime.MinValue : DateTime.MaxValue;
			}
			return fromEnd ? firstFile.Name.LastItemTimestamp : firstFile.Name.FirstItemTimestamp;
		}

		/// <summary>
		///		Get child data folder which next to the specified child folder.
		/// </summary>
		/// <param name="childFolder">
		///		The child folder which neighbour to find
		/// </param>
		/// <param name="backwards">
		///		Whether to find next folder older than the <paramref name="childFolder"/> (<see langword="true"/>)
		///		or newer (<see langword="false"/>). Older means smaller timestamp.
		/// </param>
		/// <returns>
		///		Existing data folder which is a child of this data folder or <see langword="null"/> if the sought data folder
		///		does not exist.
		/// </returns>
		public IDataFolder GetNextChild(IDataFolder childFolder, bool backwards)
		{
			LoadSubFolders(false);
			if (backwards)
			{
				return _childDataFolders.GetPredecessor(childFolder.Start);
			}
			else
			{
				// GetSuccessor accepts item key, adjusting End as it's exclusive
				return _childDataFolders.GetSuccessor(childFolder.End.AddTicks(-1));
			}
		}

		/// <summary>
		///		Get first or last child data folder
		/// </summary>
		/// <param name="fromEnd">
		///		if <see langword="true"/> returns chronologically last, otherwise first
		/// </param>
		/// <returns>
		///		<see langword="null"/> if there are no child data folders
		/// </returns>
		public IDataFolder GetFirstChildFolder(bool fromEnd)
		{
			LoadSubFolders(false);
			return fromEnd ? LastChild : FirstChild;
		}

		/// <summary>
		///		Get first child data folder covering data in the period starting from the specified timestamp.
		/// </summary>
		/// <param name="dataTimestamp">
		///		The start of the time period (inclusive) for which to find covering child data folder.
		/// </param>
		/// <param name="backwards">
		///		The direction in which the time period goes from <paramref name="dataTimestamp"/>
		/// </param>
		/// <returns>
		///		<see langword="null"/> if there are no child data folders covering data in the specified period
		/// </returns>
		public IDataFolder GetFirstChildFolder(DateTime dataTimestamp, bool backwards)
		{
			return GetSubfoldersSequence(dataTimestamp, backwards).First();
		}

		/// <summary>
		///		Get successor or predecessor of this folder at the same level of the folders hierarchy
		/// </summary>
		/// <param name="backwards">
		///		<see langword="true"/> for predecessor
		///		<see langword="false"/> for successor
		/// </param>
		/// <returns>
		///		<see langword="null"/> if none exist
		/// </returns>
		public IDataFolder GetNextSiblingInTree(bool backwards)
		{
			Check.RequireLambda(!IsVirtualRoot, () => new InvalidOperationException(StorageResources.VirtualRootInvalidOperation));

			IDataFolder retval = ParentDataFolder.GetNextChild(this, backwards);
			if (null == retval)
			{
				if (!ParentDataFolder.IsVirtualRoot)
				{
					for (
						IDataFolder nst = ParentDataFolder.GetNextSiblingInTree(backwards);
						nst != null && retval == null;
						nst = nst.GetNextSiblingInTree(backwards))
					{
						retval = nst.GetFirstChildFolder(backwards);
					}
				}
			}
			return retval;
		}

		/// <summary>
		///		Cut data files in the subtree
		/// </summary>
		/// <param name="timestamp">
		///		Cutting data timesatamp
		/// </param>
		/// <param name="predecessor">
		///		Output, the immediate predecessor of the data file (if any) covering <paramref name="owner"/>.
		/// </param>
		/// <param name="owner">
		///		Output, the data file covering the <paramref name="owner"/>.
		/// </param>
		/// <param name="successor">
		///		Output, the immediate successor of the data file (if any) covering <paramref name="owner"/>.
		/// </param>
		public void CutDataFiles(DateTime timestamp, out IRepositoryFile predecessor, out IRepositoryFile owner, out IRepositoryFile successor)
		{
			IDataFolder ownerLeafFolder;
			IDataFolder predecessorLeafFolder;
			IDataFolder successorLeafFolder;

			predecessor = owner = successor = null;

			if (this.IsLeafFolder)
			{
				LoadFiles(false);

				IRepositoryFileName fnPred, fnOwner, fnSucc;
				this.DataFileBrowser.GetDataFiles(timestamp, out fnPred, out fnOwner, out fnSucc);

				predecessor = GetContainedRepositoryFile(fnPred);
				owner = GetContainedRepositoryFile(fnOwner);
				successor = GetContainedRepositoryFile(fnSucc);
			}
			else
			{
				CutDescendantFolders(timestamp, Constants.DataFolderLevelLeaf, out predecessorLeafFolder, out ownerLeafFolder, out successorLeafFolder);

				if (ownerLeafFolder != null)
				{
					ownerLeafFolder.CutDataFiles(timestamp, out predecessor, out owner, out successor);
				}

				if (predecessor == null && predecessorLeafFolder != null)
				{
					predecessor = predecessorLeafFolder.FindFirstDataFileInSegment(true);
				}

				if (successor == null && successorLeafFolder != null)
				{
					successor = successorLeafFolder.FindFirstDataFileInSegment(false);
				}
			}
		}

		/// <summary>
		///		Find first (chronologically) data file starting from the start or the end of this subtree until the end of the whole
		///		data folders tree.
		/// </summary>
		/// <param name="fromEnd">
		///		<see langword="true"/> - search from the last (the newest) leaf folder in the subtree towards the start of the whole data folders tree
		///		<see langword="false"/> - search from the first (the oldest) leaf folder in the subtree towards the end of the whole data folders tree
		/// </param>
		/// <returns>
		///		First existing file found in the segment or <see langword="null"/> if none exists
		/// </returns>
		public IRepositoryFile FindFirstDataFileInSegment(bool fromEnd)
		{

			IRepositoryFile retval;
			IDataFolder leafFolder;

			if (IsLeafFolder)
			{
				// segment starts right here
				leafFolder = this;
			}
			else
			{
				// segment starts at the edge of the subtree
				leafFolder = GetFirstDescendant(Constants.DataFolderLevelLeaf, fromEnd);
			}

			for (
				retval = null;
				retval == null && leafFolder != null;
			)
			{
				retval = leafFolder.FindFirstDataFile(fromEnd);
				if (retval == null)
				{
					_log.InfoFormat("The leaf folder {0} is empty", leafFolder.ToString());
					leafFolder = leafFolder.GetNextSiblingInTree(fromEnd);
				}
			}
			return retval;
		}

		/// <summary>
		///		Cut descendant folders at the specified level.
		/// </summary>
		/// <param name="timestamp">
		///		Cutting data timesatamp
		/// </param>
		/// <param name="level">
		///		Level at which to cut. Must point to a descendant (i.e. be less than <see cref="Level"/> and greater or equal to zero
		///		(which is leaf level).
		/// </param>
		/// <param name="predecessor">
		///		Output, the existing predecessor at <paramref name="level"/> of a folder, covering <paramref name="timestamp"/>;
		///		<see langword="null"/> if none exists.
		/// </param>
		/// <param name="owner">
		///		Output, the data folder at <paramref name="level"/>, covering <paramref name="timestamp"/>.
		///		<see langword="null"/> if none exists.
		/// </param>
		/// <param name="successor">
		///		Output, the existing successor at <paramref name="level"/> of a folder, covering <paramref name="timestamp"/>;
		///		<see langword="null"/> if none exists.
		/// </param>
		public void CutDescendantFolders(DateTime timestamp, int level, out IDataFolder predecessor, out IDataFolder owner, out IDataFolder successor)
		{
			Check.RequireLambda(level >= 0 && level < this.Level, () => new InvalidOperationException(StorageResources.LevelOfDescendantRequired));

			LoadSubFolders(false);

			predecessor = owner = successor = null;

			IDataFolder childPredecessor;
			IDataFolder childOwner;
			IDataFolder childSuccessor;

			_childDataFolders.GetItems(timestamp, out childPredecessor, out childOwner, out childSuccessor);

			if (level < (this.Level - 1))
			{
				// need to dig deeper
				if (childOwner != null)
				{
					childOwner.CutDescendantFolders(timestamp, level, out predecessor, out owner, out successor);
				}

				// even if childOwner is found it may not be the root of the subtree containing the predecessor at the specified level
				// because there may not be existing descendants at that level;
				if (predecessor == null && childPredecessor != null)
				{
					predecessor = GetFirstDescendant(childPredecessor.End.AddTicks(-1), level, true);
				}
				if (successor == null && childSuccessor != null)
				{
					successor = GetFirstDescendant(childSuccessor.Start, level, false);
				}
			}
			else
			{
				predecessor = childPredecessor;
				owner = childOwner;
				successor = childSuccessor;
			}
		}

		/// <summary>
		///		Get first (chronologically) descendant at the specified level
		/// </summary>
		/// <param name="level">
		///		Level at which to find descendant. Must point to a descendant (i.e. be less than <code>this.Level</code> and greater or
		///		equal to zero (which is leaf level).
		/// </param>
		/// <param name="backwards">
		///		Whether to find oldest (<see langword="false"/>) or newest (<see langword="true"/>) descendant.
		/// </param>
		/// <returns>
		///		First descendant at <paramref name="level"/> or <see langword="null"/> if none exists.
		/// </returns>
		public IDataFolder GetFirstDescendant(int level, bool backwards)
		{
			RequireDescendant(level);

			IDataFolder descendant = GetFirstChildFolder(backwards);
			if (descendant != null && level < (this.Level - 1))
			{
				descendant = descendant.GetFirstDescendant(level, backwards);
			}
			else if (level == (this.Level - 1))
			{
				// reached target level
				return descendant;
			}
			return null;
		}

		/// <summary>
		///		Get first (chronologically) descendant at the specified level
		/// </summary>
		/// <param name="dataTimestamp">
		///		The start of the time period (inclusive) for which to find covering data folder
		/// </param>
		/// <param name="level">
		///		Level at which to find descendant. Must point to a descendant (i.e. be less than <code>this.Level</code> and greater or
		///		equal to zero (which is leaf level).
		/// </param>
		/// <param name="backwards">
		///		The direction in which the time period goes from <paramref name="dataTimestamp"/>
		/// </param>
		/// <returns>
		///		First descendant at <paramref name="level"/> or <see langword="null"/> if none exists.
		/// </returns>
		/// <remarks>
		///		For example GetFirstDescendant(DateTime.Now, 1, true) will find first existing descendant at level 1
		///		which covers time starting from now and going to the past.
		/// </remarks>
		public IDataFolder GetFirstDescendant(DateTime dataTimestamp, int level, bool backwards)
		{
			RequireDescendant(level);
			Check.RequireLambda(dataTimestamp >= this.Start && dataTimestamp <= this.End
				, () => new ArgumentException("The timestamp is outside of the range of the folder", "dataTimestamp"));

			if (IsChild(level))
			{
				return GetFirstChildFolder(dataTimestamp, backwards);
			}

			IDataFolder retval = null;

			var enumerator = GetSubfoldersSequence(dataTimestamp, backwards).GetEnumerator();

			if (enumerator.MoveNext())
			{
				if (IsCovering(enumerator.Current, dataTimestamp))
				{
					// enumerator.Current is the "owner"
					retval = enumerator.Current.GetFirstDescendant(dataTimestamp, level, backwards);
				}
				else
				{
					// there's no owner, will look for first descendant
					enumerator.Reset();
				}
			}

			if (retval == null)
			{
				// enumerator will now point past any owner (covering) child folder; so no need to use (filter by) the target timestamp
				// just find first descendant in those folders; need to enumerate because an existing child folder may not have any
				// descendants at the target level
				while (enumerator.MoveNext())
				{
					retval = enumerator.Current.GetFirstDescendant(level, true);
					if (null != retval)
					{
						break;
					}
				}
			}

			return retval;
		}

		#endregion methods

		#endregion IDataFolder members

		#region private properties --------------------------------------------

		private IFileProvider FileProvider
		{
			get
			{
				return _repoFolder.Repository.ObjectFactory.FileSystemProvider.FileProvider;
			}
		}

		private IDirectoryProvider DirectoryProvider
		{
			get
			{
				return _repoFolder.Repository.ObjectFactory.FileSystemProvider.DirectoryProvider;
			}
		}

		#endregion private properties -----------------------------------------

		private IRepositoryFile GetContainedRepositoryFile(IRepositoryFileName fileName)
		{
			IRepositoryFile retval = null;
			if (null != fileName)
			{
				retval = new RepositoryFile(containingFolder: this, fileName: fileName);
			}
			return retval;
		}

		private void RequireDescendant(int level)
		{
			Check.RequireLambda(level >= 0 && level < this.Level, () => new InvalidOperationException(StorageResources.LevelOfDescendantRequired));
		}

		private bool IsChild(int level)
		{
			return level == (this.Level - 1);
		}

		private bool IsCovering(IDataFolder folder, DateTime timestamp)
		{
			return timestamp >= folder.Start && timestamp < folder.End;
		}

		/// <summary>
		///		Get full path of the contained data file
		/// </summary>
		/// <param name="file">
		/// </param>
		/// <returns>
		/// </returns>
		/// <remarks>
		///		This must be a leaf folder
		/// </remarks>
		private string GetDataFilePath(IRepositoryFileName file)
		{
			Check.Require(IsLeafFolder);
			return Path.Combine(FullPath, file.FileName);
		}

		/// <summary>
		///		Check invariant constraints
		/// </summary>
		[Conditional("DBC_CHECK_ALL")]
		[Conditional("DBC_CHECK_INVARIANT")]
		private void Invariant()
		{
			// check root-non-root constraints are ok
			Util.Check.Invariant(
				(
					_folderDescriptor == null										// virtual root folder
					&& _repoFolder.DataFoldersExplorer.LevelCount == this.Level		// virtual root folder
				)
				|| (
					_folderDescriptor != null										// non-root folder
					&& _repoFolder.DataFoldersExplorer.LevelCount > this.Level		// non-root folder
				)
			);

			Util.Check.Invariant(
				(
					this.IsVirtualRoot
					&& _parentDataFolder == null
				)
				|| (
					!this.IsVirtualRoot
					&& _parentDataFolder != null
				)
			);

			Check.Invariant(
				IsLeafFolder
				|| (
					(_childDataFolders == null && !SubfoldersLoaded)
					|| (_childDataFolders != null && SubfoldersLoaded)
				)
				, "For non-leaf folders if SubfoldersLoaded is true, _childDataFolders must not be null");
		}

	}
}
