//-----------------------------------------------------------------------------
// <created>2/2/2010 2:52:16 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using bfs.Repository.Exceptions;
using System.Collections.ObjectModel;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Default implementation of <see cref="IDataFileAccessor"/>
	/// </summary>
	internal class RepositoryFileAccessor : IDataFileAccessor
	{
		/// <summary>
		///		Data items comparer by timestamps only
		/// </summary>
		class TimestampComparer : IComparer<IDataItem>
		{
			public int Compare(IDataItem x, IDataItem y)
			{
				return DateTime.Compare(x.DateTime, y.DateTime);
			}
		}

		/// <summary>
		///		Data items comparer delegating comparison to provided custom comparer if timestamps are equal.
		/// </summary>
		class CustomisedComparer : IComparer<IDataItem>
		{
			private IComparer<IDataItem> _customComparer;
			
			internal CustomisedComparer(IComparer<IDataItem> customComparer)
			{
				Check.RequireArgumentNotNull(customComparer, "customComparer");
				_customComparer = customComparer;
			}

			public int Compare(IDataItem x, IDataItem y)
			{
				int retval = DateTime.Compare(x.DateTime, y.DateTime);
				if (0 == retval)
				{
					return _customComparer.Compare(x, y);
				}
				return retval;
			}
		}

		#region fields --------------------------------------------------------

		/// <summary>
		///		target file path before possible update;
		///		may need to rename/delete etc
		/// </summary>
		private string _existinglFilePath;

		private IFolder _folder;

		/// <summary>
		///		The file descriptor is always in sync with existing repo file.
		///		Whenever its properties are modified the repo file is renamed
		///		accordingly
		/// </summary>
		private IRepositoryFileName _repoFile;

		private List<IDataItem> _dataItems;

		private IDataFolder _dataFolder;

		private DateTime _minTimestampToAccept;

		private DateTime _maxTimestampToAccept;

		private ICoder _coder;
		private ICoder _encryptor;
		private bool _isSorted;

		private IComparer<IDataItem> _equalTimestampedItemsComparer;

		// should be set only in EqualTimestampItemsComparer setter
		// should be immutable to ensure Sort is atomic
		private IComparer<IDataItem> _currentComparer;

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		/// <summary>
		///		
		/// </summary>
		/// <param name="dataFolder">
		///		Leaf data folder in which the data files will be accessed.
		/// </param>
		/// <param name="repoFile">
		///		Target repo file, optional. New instance will be created using object factory if null is supplied.
		/// </param>
		/// <param name="coder">
		///		Compressor instance
		/// </param>
		/// <param name="equalTimestampedItemsComparer">
		///		Comparer to use when sorting data items for items with equal timestamps.
		///		When timestamps are not equal the comparer has no effect.
		/// </param>
		internal RepositoryFileAccessor(
			IDataFolder dataFolder
			, IRepositoryFileName repoFile
			, ICoder coder
			, IComparer<IDataItem> equalTimestampedItemsComparer)
		{
			Check.DoRequireArgumentNotNull(dataFolder, "dataFolder");
			Check.DoRequireArgumentNotNull(repoFile, "repoFile");
			Check.DoRequireArgumentNotNull(coder, "coder");
			RepositoryFolder.CheckNotDetached(dataFolder.RepoFolder);

			_folder = dataFolder.RepoFolder;
			_repoFile = repoFile;
			_dataFolder = dataFolder;

			if (_repoFile == null)
			{
				_repoFile = _folder.Repository.ObjectFactory.CreateNewFile(_folder);
			}
			this.Coder = coder;
			
			if (repoFile.Encrypted)
			{
				this.Encryptor = _folder.Repository.ObjectFactory.GetEncryptor(repoFile.EncryptorCode);
			}

			if (_folder.Repository.ObjectFactory.FileSystemProvider.FileProvider.Exists(this.FilePath))
			{
				_existinglFilePath = this.FilePath;
			}
			else
			{
				_dataItems = new List<IDataItem>();
			}
			_isSorted = true;
			this.EqualTimestampItemsComparer = equalTimestampedItemsComparer;

			FirstItemTimestamp = DateTime.MaxValue;
			LastItemTimestamp = DateTime.MinValue;
		}

		/// <summary>
		///		Create accessor for a file and retrieve compressor from repository's
		///		object factory by the file extension
		/// </summary>
		/// <param name="folder">
		///		Containing repo folder
		/// </param>
		/// <param name="repoFile">
		///		Target data file, optional.
		/// </param>
		/// <param name="dataFolder">
		///		Leaf data folder containing <paramref name="repoFile"/>
		/// </param>
		/// <param name="equalTimestampedItemsComparer">
		///		Comparer for data items with equal timestamps, optional.
		/// </param>
		internal RepositoryFileAccessor(
			IFolder folder
			, IRepositoryFileName repoFile
			, IDataFolder dataFolder
			, IComparer<IDataItem> equalTimestampedItemsComparer)
			: this(dataFolder, repoFile
				, folder.Repository.ObjectFactory.GetCompressor(repoFile.CompressorCode)
				, equalTimestampedItemsComparer)
		{
		}

		/// <summary>
		///		Create new instance.
		/// </summary>
		/// <param name="folder">
		///		Data folder where file will be stored.
		/// </param>
		/// <param name="file">
		///		File name instance, optional.
		/// </param>
		/// <remarks>
		///		If <paramref name="file"/> is not supplied new instance will be created using the object factory (<see cref="IRepository.ObjectFactory"/>).
		///		Coder and encryptor will be set from the object factory as well. They may be overridden afterwards.
		/// </remarks>
		public RepositoryFileAccessor(IDataFolder folder, IRepositoryFileName file)
			: this(folder: folder.RepoFolder, repoFile: file, dataFolder: folder, equalTimestampedItemsComparer: null)
		{
		}

		#endregion constructors -----------------------------------------------

		#region public properties ---------------------------------------------

		public int ItemCount
		{
			get { return _dataItems.Count; }
		}

		public ICoder Coder
		{
			get { return _coder; }
			set
			{
				Check.RequireArgumentNotNull(value, "value");
				_coder = value;
				_repoFile.CompressorCode = _coder.KeyCode;
			}
		}
		
		/// <summary>
		/// 	Direct access to the list of data items.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	<see cref="OverrideMode" /> is OFF <see langword="false"/>
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// 	Setting <see langword="null"/> value.
		/// </exception>
		/// <remarks>
		/// 	The property never returns <see langword="null"/>.
		/// </remarks>
		public IList<IDataItem> ItemListDirect
		{
			get
			{
				Check.DoCheckOperationValid(OverrideMode, () => StorageResources.RepoFileAccessorMustBeInOverrideMode);
				Check.Ensure(_dataItems != null);
				return _dataItems;
			}
			set
			{
				Check.DoCheckOperationValid(OverrideMode, () => StorageResources.RepoFileAccessorMustBeInOverrideMode);
				Check.DoRequireArgumentNotNull(value, "value");
				_dataItems = value as List<IDataItem>;
				if (null == _dataItems)
				{
					_dataItems = new List<IDataItem>(value);
				}
			}
		}
		
		/// <summary>
		/// 	Whether the list of data items is sorted by timestamp.
		/// </summary>
		/// <remarks>
		/// 	This flag is not maintained when accessing and modifying the <see cref="ItemListDirect"/>.
		/// 	The accessor must be in override mode (<see cref="OverrideMode" />) to be able to set this property.
		/// </remarks>
		public bool IsSorted
		{
			get { return _isSorted; }
			set
			{
				Check.DoCheckOperationValid(OverrideMode, () => StorageResources.RepoFileAccessorMustBeInOverrideMode);
				_isSorted = value;
			}
		}

		public ICoder Encryptor
		{
			get { return _encryptor; }
			set
			{
				_encryptor = value;
				_repoFile.EncryptorCode = null != _encryptor ? _encryptor.KeyCode : string.Empty;
			}
		}

		/// <summary>
		///		Instructs the accessor to reject items which are older than the specified date-time
		/// </summary>
		public DateTime MinTimestampToAccept
		{
			get
			{
				return _minTimestampToAccept < _dataFolder.Start
					? _dataFolder.Start
					: _minTimestampToAccept;
			}
			set
			{
				Util.Check.RequireLambda(value >= _dataFolder.Start, () => StorageResources.DataFolderTimeRangeViolation);
				_minTimestampToAccept = value;
			}
		}

		/// <summary>
		///		Instructs the accessor to reject items which are younger than the specified date-time. Inclusive.
		/// </summary>
		public DateTime MaxTimestampToAccept
		{
			get
			{
				return _maxTimestampToAccept > _dataFolder.End ? _dataFolder.End : _maxTimestampToAccept;
			}
			set
			{
				Util.Check.RequireLambda(value <= _dataFolder.End, () => StorageResources.DataFolderTimeRangeViolation);
				_maxTimestampToAccept = value;
			}
		}

		/// <summary>
		/// 	Get path of the currently open file as it would be after flushing the accessor (<see cref="Flush()". />)
		/// </summary>
		public string FilePath
		{
			get
			{
				return System.IO.Path.Combine(_dataFolder.FullPath, _repoFile.FileName);
			}
		}
		
		/// <summary>
		/// 	Get or set whether the accessor is in a mode allowing to manually override certain functions and restrictions.
		/// </summary>
		/// <remarks>
		/// 	When the mode is ON:
		/// 	- <see cref="Sort" /> will sort the list of items regardless of the <see cref="IsSorted" />;
		/// 	- <see cref="ItemListDirect" /> can be used to access and modify the list of items directly;
		/// </remarks>
		public bool OverrideMode
		{ get; set; }
		
		/// <summary>
		/// 	Get path of the currently open file as it exists now.
		/// </summary>
		public string ExistinglFilePath {
			get { return _existinglFilePath; }
			private set { _existinglFilePath = value; }
		}

		/// <summary>
		///		The data items collection must be sorted, otherwise InvalidOperationException is thrown.
		///		Returns null for empty collection.
		/// </summary>
		public IDataItem FirstItem
		{
			get
			{
				IDataItem retval = null;
				if (_dataItems.Count > 0)
				{
					if (!OverrideMode && !IsSorted)
					{
						throw new InvalidOperationException("The collection is not sorted");
					}
					retval = _dataItems[0];
				}
				return retval;
			}
		}

		/// <summary>
		///		The data items collection must be sorted, otherwise InvalidOperationException is thrown.
		///		Returns null for empty collection.
		/// </summary>
		public IDataItem LastItem
		{
			get
			{
				IDataItem retval = null;
				if (_dataItems.Count > 0)
				{
					if (!OverrideMode && !IsSorted)
					{
						throw new InvalidOperationException("The collection is not sorted");
					}
					retval = _dataItems[_dataItems.Count - 1];
				}
				return retval;
			}
		}

		/// <summary>
		///		Does not require data collection to be sorted
		/// </summary>
		public DateTime FirstItemTimestamp
		{ get; private set; }

		/// <summary>
		///		Does not require data collection to be sorted
		/// </summary>
		public DateTime LastItemTimestamp
		{ get; private set; }

		/// <summary>
		///		Get target file descriptor.
		/// </summary>
		/// <remarks>
		///		Note that the descriptor contains current file name as it exists on disk or as it was instantiated.
		///		There may be in-memory changes to the file content which will alter the name when the accessor is flushed.
		/// </remarks>
		public IRepositoryFileName TargetFile
		{ get { return _repoFile; } }

		/// <summary>
		///		Get or set comparer to use when sorting data items for items with equal timestamps.
		///		When timestamps are not equal the comparer has no effect.
		/// </summary>
		/// <remarks>
		///		Setter initialises the current comparer.
		/// </remarks>
		public IComparer<IDataItem> EqualTimestampItemsComparer
		{
			get { return _equalTimestampedItemsComparer; }
			set
			{
				_equalTimestampedItemsComparer = value;
				// should be creating new instance to keep current comparer immutable to ensure Sort is atomic
				if (value == null)
				{
					_currentComparer = new TimestampComparer();
				}
				else
				{
					_currentComparer = new CustomisedComparer(value);
				}
			}
		}

		#endregion public properties ------------------------------------------

		#region public methods ------------------------------------------------

		/// <summary>
		///		Get read only collection of items in chronological order.
		/// </summary>
		/// <param name="rangeStart">
		///		The start of the datetime range to return, inclusive
		/// </param>
		/// <param name="rangeEnd">
		///		The end of the date-time range, exclusive
		/// </param>
		/// <returns>
		///		Read-only collection of data items which may reflect
		///		further updates to the data items collection contained in
		///		this instance of file accessor
		/// </returns>
		public IList<IDataItem> GetItems(DateTime rangeStart, DateTime rangeEnd)
		{
			if (this.ItemCount == 0)
			{
				return new ReadOnlyCollection<IDataItem>(new List<IDataItem>());
			}

			if (rangeStart <= this.FirstItem.DateTime && rangeEnd >= this.LastItem.DateTime)
			{
				return GetAllItems();
			}
			SortOnTheFly();
			return _dataItems.SkipWhile(di => di.DateTime < rangeStart).TakeWhile(di => di.DateTime < rangeEnd).ToList();
		}

		/// <summary>
		///		Get read only collection of all contained items in chronological order.
		/// </summary>
		/// <returns>
		///		Read-only collection of data items which may reflect further updates to the data items collection contained in
		///		this instance of file accessor.
		/// </returns>
		/// <remarks>
		/// 	The collection is sorted if necessary if <see cref="OverrideMode" /> is OFF. Otherwise it will be returned as is.
		/// </remarks>
		public IList<IDataItem> GetAllItems()
		{
			SortOnTheFly();
			return new ReadOnlyCollection<IDataItem>(_dataItems);
		}

		/// <summary>
		///		Add data item to the file buffer; no IO involved
		/// </summary>
		/// <param name="dataItem"></param>
		/// <returns>
		///		<see langword="true"/> - successfully added
		///		<see langword="false"/> - item's timestamp does fall into configured datetime range
		///		<see cref="MinTimestampToAccept"/>, <see cref="MaxTimestampToAccept"/>
		/// </returns>
		public bool Add(IDataItem dataItem)
		{
			if (null == dataItem)
			{
				throw new ArgumentNullException("dataItem");
			}
			if (dataItem.DateTime < this.MinTimestampToAccept || dataItem.DateTime > this.MaxTimestampToAccept)
			{
				return false;
			}
			else
			{
				if (IsSorted && this.ItemCount > 0 && _dataItems[this.ItemCount - 1].DateTime > dataItem.DateTime)
				{
					_isSorted = false;
				}

				if (dataItem.DateTime > LastItemTimestamp)
				{
					LastItemTimestamp = dataItem.DateTime;
				}
				else if (dataItem.DateTime < FirstItemTimestamp)
				{
					FirstItemTimestamp = dataItem.DateTime;
				}

				_dataItems.Add(dataItem);
				return true;
			}
		}

		/// <summary>
		///		Add all items to the file
		/// </summary>
		/// <param name="dataItems">
		///		Data items to add (null references not allowed in the collection)
		/// </param>
		/// <returns>
		///		List of rejected (due to configured timestamp range) items
		///		<see cref="MinTimestampToAccept"/>, <see cref="MaxTimestampToAccept"/>
		/// </returns>
		public IList<IDataItem> Add(IEnumerable<IDataItem> dataItems)
		{
			List<IDataItem> rejectedItems = new List<IDataItem>();
			foreach (IDataItem item in dataItems)
			{
				if (!Add(item))
				{
					rejectedItems.Add(item);
				}
			}
			return rejectedItems;
		}

		protected void OnFileDeleted()
		{
			try
			{
				_dataFolder.DataFileBrowser.FileDeleted(_repoFile.FirstItemTimestamp);
			}
			catch (FileContainerNotificationException e)
			{
				throw ConcurrencyExceptionHelper.GetDataFileDeletionNotificationFailed(_folder, _repoFile.FileName, e);
			}
		}

		protected void OnFileChanged(DateTime oldFileKey)
		{
			_dataFolder.DataFileBrowser.FileChanged(oldFileKey, _repoFile);
		}

		protected void OnFileCreated()
		{
			_dataFolder.DataFileBrowser.FileAdded(_repoFile);
		}

		protected bool IsFileRegistered()
		{
			return _repoFile != null && _dataFolder.DataFileBrowser.GetFile(_repoFile.FirstItemTimestamp) != null;
		}

		/// <summary>
		///		Flush unsaved changes.
		/// </summary>
		/// <remarks>
		///		If the number of items is zero, the file is deleted from disk.
		///		If saving over existing file it first gets deleted and the saved. Deletion and addition are reported to the corresponding
		///		file container browser to synchronise the files collection. Any discrepancies (like when deleting existing and not finding it
		///		on disk or in the file container's collection result in an exception. Data access should be stopped as soon as possible
		///		to prevent data corruption in such cases.
		/// 	The collection of data items is sorted if necessary if <see cref="OverrideMode" /> is OFF. Otherwise the caller is
		/// 	responsible for sorting the collection (may call <see cref="Sort()" /> when necessary).
		/// </remarks>
		/// <exception cref="FileContainerNotificationException">
		///		The file cannot be found in the container; possible concurrency issue
		/// </exception>
		/// <exception cref="OverlappingFileInContainer">
		///		The <paramref name="newRepoFile"/> overlaps with an existing file; possible concurrency issue or internal error.
		/// </exception>
		public void Flush()
		{
			using (var scope = StorageTransactionScope.Create(_folder.Repository))
			{
				FlushImpl();
				scope.Complete();
			}
		}
		
		/// <summary>
		/// 	Delete currently open file.
		/// </summary>
		public void DeleteCurrentFile()
		{
			DeleteExistingFileImpl();
			OnFileDeleted();
		}

		/// <summary>
		///		Discarding changes if dirty. Call <see cref="Flush"/> to save changes.
		/// </summary>
		public void Close()
		{
			FirstItemTimestamp = DateTime.MaxValue;
			LastItemTimestamp = DateTime.MinValue;
			_existinglFilePath = null;
			_dataItems = null;
			_dataFolder = null;
			_repoFile = null;
		}

		/// <summary>
		///		Read data from disk.
		/// </summary>
		/// <exception cref="FileNotFoundException">
		///		<see cref="FileKnownToExist"/> is <see langword="false"/>;
		///		The file (<see cref="_existinglFilePath"/>) does not exist
		/// </exception>
		public void ReadFromFile()
		{
			Check.DoAssertLambda(FileKnownToExist, () => new FileNotFoundException());
			_dataItems = ReadFromFileImpl();
			FirstItemTimestamp = FirstItem.DateTime;
			LastItemTimestamp = LastItem.DateTime;
		}

		/// <summary>
		/// 	Sort the list of data items by their timestamp (and by <see cref="EqualTimestampItemsComparer" /> if set) if it is not
		/// 	sorted <see cref="IsSorted" /> is <see langword="false"/> or if <see cref="OverrideMode" /> is ON.
		/// </summary>
		/// <remarks>
		/// 	Note that in manual mode call to this method will force sorting.
		/// </remarks>
		public void Sort()
		{
			Check.Invariant(null != _currentComparer, "_currentComparer must never be null");

			if (!IsSorted || OverrideMode)
			{
				SortImpl();
			}
		}
		
		/// <summary>
		/// 	Sort data items on the fly when necessary.
		/// </summary>
		/// <remarks>
		/// 	In override mode (<see cref="OverrideMode" />) the method does nothing - the client is responsible for the sorting.
		/// </remarks>
		private void SortOnTheFly()
		{
			if (!OverrideMode && !IsSorted)
			{
				SortImpl();
			}
		}
		
		private void SortImpl()
		{
			Check.Invariant(null != _currentComparer, "_currentComparer must never be null");

			_dataItems.Sort(_currentComparer);
			_isSorted = true;
		}

		/// <summary>
		///		Whether the file being accessed is known to exist
		/// </summary>
		/// <remarks>
		///		The actual existence is subject to further check for the existence of the physical file on disk.
		///		If file is known to exist but does not actually exist it means that there's a cuncurrent access interfering
		///		with this accessor and the work should not continue.
		/// </remarks>
		public bool FileKnownToExist
		{ get { return !string.IsNullOrEmpty(_existinglFilePath); } }

		/// <summary>
		///		Check whether corresponding repo file already exists on disk
		/// </summary>
		public bool FileExists()
		{
			return FileKnownToExist && _folder.Repository.ObjectFactory.FileSystemProvider.FileProvider.Exists(_existinglFilePath);
		}

		/// <summary>
		///		Determine whether the specified timestamp is within the range of contained data items
		///		timestamps. If this method returns true the data item with the specified timestamp
		///		must be stored in this file
		/// </summary>
		/// <param name="itemTimestamp">
		///		The data item timestamp
		/// </param>
		/// <returns>
		///		true if te file contains at least 2 data items and the specified timestamp
		///			falls in the datetime range in between first and last contained data items
		///			inclusive
		///		false otherwise
		/// </returns>
		public bool IsTimestampCovered(DateTime itemTimestamp)
		{
			return this.ItemCount > 1
				&& this.FirstItemTimestamp <= itemTimestamp
				&& this.LastItemTimestamp >= itemTimestamp;
		}

		public override string ToString()
		{
			StringBuilder bld = new StringBuilder(50);
			bld.Append(_dataFolder.PathInRepository);
			bld.Append(RepositoryFolder.logicalPathSeparator).Append(_repoFile.FileName);
			if (FileKnownToExist)
			{
				bld.Append("(exists)");
			}
			return bld.ToString();
		}

		public void Dispose()
		{
			Close();
		}

		#endregion public methods ---------------------------------------------

		#region private methods -----------------------------------------------

		/// <summary>
		///		Throws exception if not actually deleted
		/// </summary>
		private void DeleteExistingFileImpl()
		{
			_folder.Repository.ObjectFactory.FileSystemProvider.FileProvider.Delete(_existinglFilePath);
			_existinglFilePath = string.Empty;
		}

		/// <summary>
		///		Read, decode and deserialize the contents of the associated repo file.
		/// </summary>
		/// <returns>
		/// </returns>
		/// <exception cref="FileNotFoundException">
		///		<see cref="_existinglFilePath"/> does not refer to an existing file.
		/// </exception>
		private List<IDataItem> ReadFromFileImpl()
		{
			List<IDataItem> retval;

			if (_folder.Repository.ObjectFactory.FileSystemProvider.FileProvider.Exists(this.FilePath))
			{
				using (FileStream fs = _folder.Repository.ObjectFactory.FileSystemProvider.FileProvider.Open(
					this.FilePath, FileMode.Open, FileAccess.Read))
				{
					int version;
					int dataItemCount;

					using (BinaryReader rdr = new BinaryReader(fs))
					{
						version = rdr.ReadInt32();
						if (version > Constants.RepoFileHeaderCurrentVersion)
						{
							throw new IncompatibleVersionException(
								string.Format(
									StorageResources.UnsupportedDataFileFormatVersion
									, version
									, Constants.RepoFileHeaderCurrentVersion
								)
								, string.Empty
								, null
							);
						}
						dataItemCount = rdr.ReadInt32();
						Stream source = Encryptor == null ? fs : Encryptor.WrapInDecodingStream(fs);
						using (source = Coder.WrapInDecodingStream(source))
						{
							BinaryFormatter formatter = new BinaryFormatter();
							retval = (List<IDataItem>)formatter.Deserialize(source);
						}

						Check.DoAssertLambda(dataItemCount == retval.Count
							, () => new DataIntegrityException(
								StorageResources.DataIntegrityViolationDetected
								, string.Format(
									StorageResources.TechInfoDataItemCountMismatch
									, FilePath, dataItemCount, retval.Count)));

					} // using (BinaryReader rdr = new BinaryReader(fs))
				}
			}
			else // if (_folder.Repository.ObjectFactory.FileProvider.Exists(this.FilePath))
			{
				throw new FileNotFoundException("ReadFileImpl", _existinglFilePath);
			}

			Invariant();

			return retval;
		}

		[Conditional("DBC_CHECK_ALL"),
		Conditional("DBC_CHECK_INVARIANT")]
		private void Invariant()
		{
			Check.Invariant(_repoFile != null);
		}
		
		private void FlushImpl()
		{
			if (0 == _dataItems.Count)
			{
				// removing file
				if (FileKnownToExist)
				{
					DeleteCurrentFile();
				}
			}
			else
			{
				SortOnTheFly();

				DateTime oldFileKey = _repoFile.FirstItemTimestamp;

				// note: [VK] not sure if checking the timestamps to fall into the allowed date-time range is needed here
				// if not in override mode the check will not be necessary as it's performed each time data item is added
				// otherwise, in override mode it's not clear whether such enforcement is desirable.
				_repoFile.FirstItemTimestamp = this.FirstItem.DateTime;
				_repoFile.LastItemTimestamp = this.LastItem.DateTime;
				_repoFile.EncryptorCode = null != _encryptor ? _encryptor.KeyCode : string.Empty;

				bool fileExisted = FileKnownToExist;

				if (fileExisted)
				{
					// this will throw an exception if file does not exist
					DeleteExistingFileImpl();
				}

				using (FileStream fs = _folder.Repository.ObjectFactory.FileSystemProvider.FileProvider.Open(
					this.FilePath, FileMode.CreateNew, FileAccess.Write))
				using (BinaryWriter writer = new BinaryWriter(fs))
				{
					// header
					writer.Write(Constants.RepoFileHeaderCurrentVersion);
					writer.Write((int)_dataItems.Count);
					// data
					BinaryFormatter fmt = new BinaryFormatter();
					Stream receiver = Encryptor == null ? fs : Encryptor.WrapInEncodingStream(fs);
					using (receiver = Coder.WrapInEncodingStream(receiver))
					{
						fmt.Serialize(receiver, _dataItems);
					}
				}

				_existinglFilePath = FilePath;

				if (fileExisted)
				{
					OnFileChanged(oldFileKey);
				}
				else
				{
					OnFileCreated();
				}

				Check.Ensure(FileExists());
			}
		}

		#endregion private methods --------------------------------------------
	}
}
