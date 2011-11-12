//-----------------------------------------------------------------------------
// <created>3/26/2010 3:34:00 PM</created>
// <author>Vasily Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using log4net;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	public class RepositoryFolderReader
    {
        #region fields --------------------------------------------------------

		private static readonly ILog _log = LogManager.GetLogger(typeof(RepositoryFolderReader));

		private EnumerationDirection _direction;
		private IDataFileIterator _fileIterator;
		private Util.IListReader<IDataItem> _listReader;
		private Func<IRepositoryFileName, DateTime> _getFirstItemToReadFromFileName;
		private Func<IRepositoryFileName, DateTime> _getLastItemToReadFromFileName;
		private FolderReadingPosition _position;

		// comparing timestamps according to the reading direction
		private Util.IDirectedTimeComparison _timeComparison;
        
        #endregion fields -----------------------------------------------------

        internal RepositoryFolderReader(IFolder folder, IReader reader)
		{
			_position = new FolderReadingPosition(folder);
			this.TargetFolder = folder;
			MasterReader = reader;
			// creating iterator; its direction will be set in SetDirection()
			_fileIterator = folder.Repository.ObjectFactory.GetDataFileIterator(folder, false);
			SetDirection(EnumerationDirection.Forwards);
		}

        #region public properties ---------------------------------------------

        /// <summary>
        ///     Get target repository folder
        /// </summary>
        public IFolder TargetFolder
		{ get; private set; }

		/// <summary>
		///		Get repository reader owning this folder reader
		/// </summary>
		public IReader MasterReader
		{ get; private set; }

		/// <summary>
		///		Get target repository.
		/// </summary>
		internal IRepository Repository
		{
			get
			{
				return (IRepository)MasterReader.Repository;
			}
		}

        /// <summary>
        ///     Get whether <see cref="MoveNext"/> will fetch next item in the currently
        ///     loaded file; i.e. returns whether a file is loaded and not yet finished.
        ///     If file is not loaded, returns <see langword="false"/>
        /// </summary>
        public bool HasMoreItemsInCurrentFile
        {
            get
            {
                Util.Check.Require(this.IsDataLoaded);

				return _listReader.HasMore;
            }
        }

        /// <summary>
        ///     Get next data file to be read.
		/// </summary>
		/// <remarks>
		///     The file will be opened automatically after currently loaded file is finished.
		/// </remarks>
		public IRepositoryFile NextDataFile
        {
            get
            {
                return _fileIterator.Current;
            }
        }

        /// <summary>
        ///     Get the repository file which is currently loaded.
        /// </summary>
        public IRepositoryFile LoadedFile
        {
            get;
			private set;
        }

        /// <summary>
        ///		Get boolean value indicating there's no more data in the folder.
        /// </summary>
        public bool EndReached
        {
			get
			{
				return !this.HasMoreItemsInCurrentFile && !this.NextFileFound;
			}
        }

        /// <summary>
        ///     Get boolean flag indicating whether the reader has a valid <see cref="CurrentItem"/>
        /// </summary>
        public bool HasItem
        {
            get
            {
                return this.IsDataLoaded && _listReader.HasItem;
            }
        }

        /// <summary>
        ///		Get current data item. If the last <see cref="MoveNext"/> returned <see langword="false"/>
		///		this property will throw an exception.
        /// </summary>
		/// <exception cref="InvalidOperationException">
		///		- The reader is positioned before first or after last item ;
		///		- No data is loaded (such as before calling <see cref="LoadNextFile"/>)
		/// </exception>
        public IDataItem CurrentItem
        {
            get
            {
				Check.RequireLambda(this.IsDataLoaded, () => new InvalidOperationException(StorageResources.DataIsNotLoaded));
                return _listReader.Current;
            }
        }

		/// <summary>
		///		Get seek time used last time when calling <see cref="SeekDataFile"/>
		/// </summary>
		/// <remarks>
		///		Usage scenario:
		///			- client (e.g. repo reader) first calls <see cref="SeekDataFile"/> and data file to be loaded next is set;
		///				no data gets loaded at this stage because there may be other readers to be read first; the seek time is
		///				captured in this property;
		///			- client uses <see cref="NextFileFirstTimestampToRead"/> to determine when to bring the reader online; that is when
		///				current reading position (data timestamp) equals or passes first item-to-be-read from this reader's timestamp;
		///			- when brought online and if the sought position has been beyond the start of the file (logical rather than physical,
		///				 according to <see cref="Direction"/>), the reading postion is restored.
		/// </remarks>
		public DateTime LastSeekTime
		{ get; private set; }

        /// <summary>
        ///     Get potential timestamp of the first data item in the data file to be loaded next.
		/// </summary>
		/// <remarks>
		///		Honours <see cref="Direction"/>, i.e. if it is <see langword="true"/>, the property will return last (physically) data item
		///		in the next file. This is intended to be used for deferred loading.
        ///     <see cref="SeekDataFile"/> can be called explicitly to find next file. Note that if found file starts logically before
		///     the seek time this property will return the seek time and there may turn out to be no item with matching timestamp really.
		///     But this can only be determined and handled after loading the file.
        ///     It is also found automatically in <see cref="MoveNext"/> when positioned to last item in the current file.
		/// </remarks>
		public DateTime? NextFileFirstTimestampToRead
        {
			// next file is searched in MoveNext when end of current file is reached
			get;
			private set;
        }

        /// <summary>
        ///		Get a boolean flag indicating whether current target file is loaded into memory.
        /// </summary>
        public bool IsDataLoaded
        {
            get
            {
                return _listReader != null;
            }
        }

        /// <summary>
        ///     Get flag indicating whether there's next file to read.
		/// </summary>
		/// <remarks>
		///     If the property returns <see langword="false"/> the reader will stop after reaching the end of
        ///     the currently loaded file. Call <see cref="SeekDataFile"/> to set next file or restart.
		///     While reading next file is updated automatically.
		/// </remarks>
		public bool NextFileFound
        {
            get { return null != this.NextDataFile; }
        }

		/// <summary>
		///		Get or set reading direction (chronologically)
		/// </summary>
		public EnumerationDirection Direction
		{
			get { return _direction; }
			set
			{
				if (_direction != value)
				{
					Check.DoAssertLambda(CanChangeDirection
						, () => new InvalidOperationException(StorageResources.CannotChangeReadingDirectionWhenNonSequential));
					SetDirection(value);
				}
			}
		}

		/// <summary>
		///		Check whether <see cref="Direction"/> can be changed
		/// </summary>
		public bool CanChangeDirection
		{
			get
			{
				return IsReadingSequential;
			}
		}

		/// <summary>
		///		Get current reading position.
		/// </summary>
		/// <remarks>
		///		For performance reasons the position is singleton.
		///		The position points to the <see cref="CurrentItem"/> as of the last time <see cref="MoveNext"/> was called.
		/// </remarks>
		public IFolderReadingPosition Position
		{ get { return _position; } }

        #endregion public properties ------------------------------------------

        #region public methods ------------------------------------------------

        /// <summary>
        ///     Advance to the next available item.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if position moved successfully to the next item
        ///     pointed to by <see cref="CurrentItem"/>
        ///     <see langword="false"/> if the end was reached
        /// </returns>
        /// <remarks>
		///		If this method returns <see langword="false"/> the current item remains unchanged (<see cref="CurrentItem"/>).
		///		Use <see cref="EndReached"/> to find out if this method can succeed.
        ///     usage scenario:
        ///         calling MoveNext and checking HasMoreItemsInCurrentFile and if it returns false
        ///         not calling MoveNext until NextFileFirstTimestampToRead is the smallest timestamp
        ///         available
        /// </remarks>
        public bool MoveNext()
		{
			Check.RequireLambda(this.CurrentItem != null, () => new InvalidOperationException());

			if (this.HasMoreItemsInCurrentFile)
			{
				_listReader.MoveNext();
			}
			else
			{
				OnLastItemReadFromFile();
				if (NextFileFound)
				{
					LoadNextFile();
				}
			}
			return !this.EndReached;
		}

		/// <summary>
		///		Update <see cref="Position"/> with the <see cref="CurrentItem"/>. Normally it has to be called just before returning <see cref="CurrentItem"/>
		/// </summary>
		public void UpdatePosition()
		{
			_position.Update(CurrentItem);
		}

		/// <summary>
		/// </summary>
		/// <returns>
        ///     List of items until the end of current file.
		/// </returns>
        /// <remarks>
        ///     This is not likely to be used. Normally it will be done one by one.
        /// </remarks>
		public IList<IDataItem> GetAllUntilEndOfCurrentFile()
		{
			Util.Check.Require(this.IsDataLoaded);
			IList<IDataItem> retval = _listReader.GetAllRemaining();

            if (!LoadNextFileIfFound())
            {
                Unload();
            }

			return retval;
		}

		/// <summary>
		///		Load file found during last call to <see cref="SeekDataFile"/>.
		/// </summary>
		/// <returns>
		///		<see langword="true"/> if reader has data after loading next file
		///		<see langword="false"/> otherwise
		///		<seealso cref="HasItem"/>
		/// </returns>
		/// <remarks>
		///		After the call <see cref="NextDataFile"/> will be updated and <see cref="CurrentItem"/> will point to the
		///		first data item to be read. If next file was set by <see cref="SeekDataFile"/> the position in it will be
		///		restored as required. If next file fails to load and present data items the process continues until
		///		either we have data item to be read or the end is reached.
		/// </remarks>
		public bool LoadNextFile()
		{
			Check.Require(_fileIterator.Current != null);

			do
			{
				ReadNextDataFileImpl();

				if (!this.SoughtFileLoaded)
				{
					if (!SeekLoadedFile())
					{
						_log.InfoFormat("No more items to be read from {0} after restoring position", NextDataFile.Path);
					}
				}

				MoveIteratorToNextFile();

				this.SoughtFileLoaded = true;
			}
			while (!HasItem && NextFileFound);

			Check.Require(HasItem || EndReached);

			return HasItem;
		}

		/// <summary>
		///		Seek with position not loading data immediately.
		/// </summary>
		/// <param name="position">
		///		The position to restore
		/// </param>
		/// <returns>
		///		Success boolean flag; whether a data file to be read was found. It may not be exact match for the position, however.
		/// </returns>
		/// <remarks>
		///		The position is saved and completely restored when <see cref="LoadNextFile"/> is called; issues are reported
		///		via <see cref="IRepositoryReader.SeekStatusCallback"/>, success is not reported, only warnings
		///		, <see cref="FolderSeekStatus.PositionStatus"/>.
		///		Note that you cannot change reading direction after the call to this method until you load the file and restore
		///		the reading position.
		/// </remarks>
		public bool SeekDataFile(IFolderReadingPosition position)
		{
			// save position copy
			_position.FromSeek(position);

			bool retval = null != _fileIterator.Seek(position.Time, Backwards);

			if (position.IsExact && (!retval || !_fileIterator.Current.Name.IsCovering(position.Time)))
			{
				_log.WarnFormat("Seek on folder {0} have not found data file containing item referenced in the position: {1}"
					, position.FolderKey
					, position.Time);

				MasterReader.SeekStatusCallback(new FolderSeekStatus(TargetFolder.FolderKey, FolderSeekStatus.PositionStatus.FileNotFound));
			}

			if (retval)
			{
				DateTime firstTimeInFile = _getFirstItemToReadFromFileName(NextDataFile.Name);
				if (_timeComparison.Compare(position.Time, firstTimeInFile) > 0)
				{
					// file starts before the seek time, so will need to skip some items in it
					firstTimeInFile = _position.Time;
				}
				NextFileFirstTimestampToRead = firstTimeInFile;
			}

			WasPositioned = true;
			LastSeekTime = position.Time;
			SoughtFileLoaded = false;

			return retval;
		}

		/// <summary>
		///		Get whether current reading direction (<see cref="Direction"/>) is <see cref="EnumerationDirection.Backwards"/>
		/// </summary>
		public bool Backwards
		{
			get { return this.Direction == EnumerationDirection.Backwards; }
		}

        /// <summary>
        ///     Unload currently loaded file. To load another file
        ///     it can be found with <see cref="SeekDataFile"/> and opened
        ///     with <see cref="LoadNextFile"/> or use <see cref="Seek"/>
        ///     do do it all at once.
        /// </summary>
		public void Unload()
		{
			if (this.IsDataLoaded)
			{
				_listReader = null;
				LoadedFile = null;
			}
			Util.Check.Ensure(!this.IsDataLoaded);
            Util.Check.Ensure(this.LoadedFile == null);
        }


        #endregion public methods ---------------------------------------------

		/// <summary>
		///		After loading file found during last <see cref="SeekDataFile"/> restore position inside the file.
		/// </summary>
		/// <returns>
		///		<see langword="true"/> if there are more items in the loaded file to read after restoring position
		///		<see langword="false"/> if there are no more items to read after restoring position
		///		<seealso cref="HasItem"/>
		/// </returns>
		/// <remarks>
		///		If the current <see cref="Position"/> is an exact position (<see cref="IFolderReadingPosition.IsExact"/> equals <see langword="true"/>)
		///		and during its restoration the item recorded in the position cannot be found and verified (by <see cref="IFolderReadingPosition.Time"/>,
		///		<see cref="IFolderReadingPosition.NumberOfItemsWithTheTimestampRead"/> and <see cref="IFolderReadingPosition.VerificationLastReadItemHash"/>,
		///		the issue is reported via <see cref="IRepositoryReader.SeekStatusCallback"/> and reader will be positioned to continue reading anyway.
		///		If the item was not found by <see cref="IFolderReadingPosition.Time"/> and <see cref="IFolderReadingPosition.NumberOfItemsWithTheTimestampRead"/>,
		///		reading will continue from the first item following <see cref="IFolderReadingPosition.Time"/> (timestamp will be logically greater in this
		///		case). If it was found, but verification by hash failed, reading will continue as if verification did not fail. To interfere clients will
		///		have to react to notifications from the reader.
		///		After this method executes there may be no current item available
		/// </remarks>
		protected bool SeekLoadedFile()
		{
			Check.Require(this.IsDataLoaded);

			Check.Require(!this.SoughtFileLoaded
				, "No need to seek loaded file when iterating to the next file during sequential reading");

			_listReader.Reset();

			// rolling forward to make item with time equal or greater (logically) than the position time current
			while (_listReader.HasItem && _timeComparison.Compare(_listReader.Current.DateTime, _position.Time) < 0)
			{
				_listReader.MoveNext();
			}

			// if file covers the position time and the position is exact we need to restore and verify
			// exact position; if file does not cover the exact position time there's no point skipping those items with the position
			// timestamp because they do not exist in the file and the warning must have already been sent in SeekDataFile
			if (_position.IsExact && LoadedFile.Name.IsCovering(_position.Time))
			{
				SkipItemsByExactPosition();
			}

			Check.Ensure(!_listReader.HasItem || _timeComparison.Compare(_listReader.Current.DateTime, _position.Time) >= 0);

			return this.HasItem;
		}

		private void SkipItemsByExactPosition()
		{
			Check.Require(_position.IsExact, "Position must be exact");
			Check.Require(!_listReader.HasItem || _timeComparison.Compare(_listReader.Current.DateTime, _position.Time) >= 0
				, "Reader must be alredy rolled forward to the position time");

			IDataItem itemFromPosition = null;

			string warningMessage = string.Empty;

			// skipping items with the timestamp equal to the position time which had already been read
			// note that in exact positions (where IsExact equals true) NumberOfItemsWithTheTimestampRead is always greater than zero,
			// otherwise it's not an exact position and versification is not required;
			// this loop will be executed for exact positions only, at least once (thus we either detect position restore issue or move forward
			// until past the last read item as recorded in the position);
			// if the file does not contain item with timestamp equal to the last read item timestamp in the exact position
			// the item from position will not be found
			FolderSeekStatus.PositionStatus status = FolderSeekStatus.PositionStatus.Success;
			for (
				int n = 0;
				status == FolderSeekStatus.PositionStatus.Success && n < _position.NumberOfItemsWithTheTimestampRead;
				++n)
			{
				if (!_listReader.HasItem || _listReader.Current.DateTime != _position.Time)
				{
					// using Round-trip date/time pattern: 2009-06-15T13:45:30.0900000
					_log.WarnFormat("Restoring position for {0} found item by time ({1:O}) and number ({2})"
						, TargetFolder.FolderKey
						, _position.Time
						, _position.NumberOfItemsWithTheTimestampRead);
					// loop will break
					status = FolderSeekStatus.PositionStatus.DataItemNotFound;
				}
				else
				{
					if (_position.NumberOfItemsWithTheTimestampRead - 1 == n)
					{
						// last iteration of the loop: found item by time and number
						itemFromPosition = _listReader.Current;
					}
					// moving to the next only if time matches with position time
					_listReader.MoveNext();
				}
			}

			if (itemFromPosition != null)
			{
				// have item from position; check hashes
				if (itemFromPosition.GetBusinessHashCode() != _position.VerificationLastReadItemHash)
				{
					warningMessage = string.Format(
						"Restoring position for {0} found item by time and number, but its verification failed: hash in position: {1:X8}, actual: {2:X8}"
						, TargetFolder.FolderKey
						, _position.VerificationLastReadItemHash
						, itemFromPosition.GetBusinessHashCode());
					_log.Warn(warningMessage);
					status = FolderSeekStatus.PositionStatus.DataItemHashMismatch;
				}
				else
				{
					_log.InfoFormat("Exact position for {0} restored successfully", TargetFolder.FolderKey);
				}
			}
			else
			{
				Check.Ensure(status == FolderSeekStatus.PositionStatus.DataItemNotFound);
			}

			// reporting issues only
			if (status != FolderSeekStatus.PositionStatus.Success)
			{
				MasterReader.SeekStatusCallback(new FolderSeekStatus(TargetFolder.FolderKey, status, warningMessage));
			}
		}

		/// <summary>
		///		Whether the last file found with <see cref="SeekDataFile"/> was loaded
		/// </summary>
		/// <remarks>
		///		If <see langword="true"/> reading is sequential; it may be disrupted by seeking data file in which case currently loaded file
		///		is read until end and then the new sequence begins with the file found by <see cref="SeekDataFile"/>.
		///		If <see langword="false"/> and <see cref="IsDataLoaded"/> is <see langword="true"/> then the reader in non-sequential state
		///		and cannot change direction.
		///		Set to <see langword="true"/> in <see cref="LoadNextFile()"/>, to <see langword="false"/> - in <see cref="SeekDataFile"/>.
		///		This is oppesite to <see cref="NextFileSought"/> because when next file is noaded the file iterator is moved and Next
		///		file is no longer found by seek.
		/// </remarks>
		private bool SoughtFileLoaded
		{ get; set; }

		/// <summary>
		///		Whether the reading is sequential and therefore can be reversed
		/// </summary>
		private bool IsReadingSequential
		{
			get { return SoughtFileLoaded || !IsDataLoaded; }
		}

		/// <summary>
		///		Get first logical item timestamp in the next file; that's either first or last item timestamp depending on
		///		the reading direction
		/// </summary>
		/// <remarks>
		///		Next file must be found when calling this property.
		/// </remarks>
		private DateTime NextFileFirstLogicalTimestamp
		{
			get
			{
				Check.Require(NextFileFound);
				return _getFirstItemToReadFromFileName(NextDataFile.Name);
			}
		}

		/// <summary>
		///		Whether next file (<see cref="NextDataFile"/>) will have to be read from start.
		///		When next file was found with <see cref="SeekDataFile"/> this property returns whether the next file is fully beyond
		///		the seek timestamp.
		/// </summary>
		/// <remarks>
		///		Next file must be found when calling this property.
		/// </remarks>
		private bool IsNextFileToBeReadInFull
		{
			get
			{
				// if sought file was already loaded there can be no position to restore, just sequential reading
				return SoughtFileLoaded || _position.IsFileToBeReadInFull(NextFileFirstLogicalTimestamp, _timeComparison);
			}
		}

		/// <summary>
		///		Whether seek (such as <see cref="SeekDataFile"/>) was ever called
		/// </summary>
		private bool WasPositioned
		{ get; set; }

        #region private methods -----------------------------------------------

        /// <summary>
		///		Load next file
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> - next file loaded
        ///     <see langword="false"/> - there's no next file
        /// </returns>
        private bool LoadNextFileIfFound()
        {
            bool retval = this.NextFileFound;
            if (retval)
            {
                LoadNextFile();
            }
            return retval;
        }


		private IDataFileAccessor GetAccessor(IRepositoryFile targetFile)
		{
			return Repository.ObjectFactory.GetDataFileAccessor(targetFile.ContainingFolder, targetFile.Name);
		}

		/// <summary>
		///		Loads the specified file and positions at the first data item.
		/// </summary>
		/// <exception cref="System.IO.FileNotFoundException">
		///		The <paramref name="dataFile"/> does not exist on disk.
		/// </exception>
		private void ReadFile(IRepositoryFile dataFile)
		{
			// passing null for equally timestamped items comparer, data items are read in the same
			// order as they were written; no sorting when reading by design
			IDataFileAccessor accessor = GetAccessor(dataFile);
			// will throw exception if file not found or failed to read
			accessor.ReadFromFile();

			IList<IDataItem> dataList = accessor.GetAllItems();
			if (Backwards)
			{
				_listReader = new BackwardListReader<IDataItem>(dataList);
			}
			else
			{
				_listReader = new ForwardListReader<IDataItem>(dataList);
			}
			this.LoadedFile = dataFile;

			Check.Ensure(_listReader.HasItem);
			Check.Ensure(CurrentItem != null);
		}

		/// <summary>
		///		Force the specified direction
		/// </summary>
		private void SetDirection(EnumerationDirection direction)
		{
			Check.Require(IsReadingSequential, StorageResources.CannotChangeReadingDirectionWhenNonSequential);
			bool hadData = this.HasItem;
			_direction = direction;
			_timeComparison = TimeComparer.GetComparer(direction);
			if (!Backwards)
			{
				_getFirstItemToReadFromFileName = (f) => f.FirstItemTimestamp;
				_getLastItemToReadFromFileName = (f) => f.LastItemTimestamp;
			}
			else
			{
				_getFirstItemToReadFromFileName = (f) => f.LastItemTimestamp;
				_getLastItemToReadFromFileName = (f) => f.FirstItemTimestamp;
			}
			if (IsDataLoaded && _listReader.Direction != direction)
			{
				_listReader = _listReader.Reverse();
				Check.Ensure(_listReader.Direction == Direction);
			}

			int moveCount = 0;
			if (IsDataLoaded)
			{
				// in sequential mode and when data is loaded iterator points to the data file to load next, so need to move by 2
				// (skipping currently loaded file)
				moveCount = 2;
			}
			else
			{
				// data is not loaded; 2 cases: 1) seek found file with data beyond seek time and 2) seek time is contained in the file
				if (!NextFileFound || IsNextFileToBeReadInFull)
				{
					// case 1
					moveCount = 1;
				}
				// case 2: next file remains valid because will have to read some data from it in the opposite direction
			}

			_fileIterator.Backwards = Backwards;
			for (int n = 0; n < moveCount; ++n)
			{
				MoveIteratorToNextFile();
			}

			if (!hadData)
			{
				_position.SetEmpty(direction);
			}
		}

		/// <summary>
		///		Move file iterator forward (according to <see cref="Direction"/>)
		/// </summary>
		/// <remarks>
		///		Updates dependent property <see cref="NextFileFirstTimestampToRead"/>
		/// </remarks>
		private void MoveIteratorToNextFile()
		{
			_fileIterator.MoveNext();

			NextFileFirstTimestampToRead = NextFileFound ? _getFirstItemToReadFromFileName(NextDataFile.Name) : (DateTime?)null;
		}

		/// <summary>
		///		Read next data file and handle legitimate concurrency scenarious
		/// </summary>
		/// <remarks>
		///		An attempt is made to synchronise list of data files if data file is not found and reading is forward;
		///		optimistically assuming writing is sequential; if not, it's a deployment issue.
		/// </remarks>
		/// <exception cref="ConcurrencyException">
		///		Next file was not found on disk (possibly after an attempt to re-synch)
		/// </exception>
		private void ReadNextDataFileImpl()
		{
			try
			{
				// scenario when data is being inserted into next file but there are more files after that is not handled
				// possible simultaneous writing; optimistically assuming writing is sequential
				// if not, it's a deployment issue; difficult to gracefully handle this without complex filesystem locking.
				ReadFile(NextDataFile);
			}
			catch (System.IO.FileNotFoundException e)
			{
				throw Exceptions.ConcurrencyExceptionHelper.GetFileNotFound(TargetFolder, e);
			}
		}

		/// <summary>
		///		Called when end of file is reached in <see cref="MoveNext"/>.
		/// </summary>
		private void OnLastItemReadFromFile()
		{
			Check.Require(HasItem);
			DateTime expectedTime = _getLastItemToReadFromFileName(this.LoadedFile.Name);
			if (expectedTime != CurrentItem.DateTime)
			{
				throw Exceptions.ConcurrencyExceptionHelper.GetLastReadItemTimestampMismatch(
					TargetFolder, this.LoadedFile.Name.FileName, expectedTime, CurrentItem.DateTime);
			}
		}

        #endregion private methods --------------------------------------------

    }
}
