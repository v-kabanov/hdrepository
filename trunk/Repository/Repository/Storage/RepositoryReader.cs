//-----------------------------------------------------------------------------
// <created>3/26/2010 2:41:10 PM</created>
// <author>Vasily Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Events;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Default implementation of <see cref="IRepositoryReader"/>.
	///		Loads repository files lazily.
	/// </summary>
	public class RepositoryReader : IReader
	{
		/// <summary>
		///		All folders' readers
		/// </summary>
		private Dictionary<string, RepositoryFolderReader> _readers;

		/// <summary>
		///		The list contains readers whithout loaded files.
		///		Sorted by next file's first item timestamp
		/// </summary>
		private LinkedList<RepositoryFolderReader> _offlineQueue;

		/// <summary>
		///		The list contains readers with loaded files.
		///		Unsorted.
		/// </summary>
		private LinkedList<RepositoryFolderReader> _onlineQueue;

		private LinkedList<RepositoryFolderReader> _exhaustedReaders;

		// compares by current item
		private ReaderComparerByCurrentItem _onlineReaderComparer;

		// compares by next file (first item timestamp)
		private ReaderComparerByNextFile _offlineReaderComparer;

		private ReadingPosition _position;

		private FastSmartWeakEvent<EventHandler<PositionRestoreStatusEventArgs>> _seekStatusEvent;

		#region constructors --------------------------------------------------

		/// <summary>
		///		Create new instance
		/// </summary>
		internal RepositoryReader(IRepository repository)
		{
			Repository = repository;

			_seekStatusEvent = new FastSmartWeakEvent<EventHandler<PositionRestoreStatusEventArgs>>();

			_readers = new Dictionary<string, RepositoryFolderReader>();
			_onlineQueue = new LinkedList<RepositoryFolderReader>();
			_offlineQueue = new LinkedList<RepositoryFolderReader>();
			_exhaustedReaders = new LinkedList<RepositoryFolderReader>();

			_onlineReaderComparer = new ReaderComparerByCurrentItem();
			_offlineReaderComparer = new ReaderComparerByNextFile();

			_position = new ReadingPosition();

			EnumerationDirection defaultDirection = EnumerationDirection.Forwards;

			ChangeDirection(defaultDirection, true);

			Reset();
		}

		#endregion constructors -----------------------------------------------

		/// <summary>
		///		Get target repository
		/// </summary>
		public IRepository Repository
		{ get; private set; }

		#region IRepositoryReader Members -------------------------------------

		/// <summary>
		///		Get target repository
		/// </summary>
		IRepositoryManager IRepositoryDataAccessor.Repository
		{ get { return Repository; } }

		/// <summary>
		///		The event is raised when an issue is detected during deferred position restoration.
		/// </summary>
		/// <remarks>
		///		This is a weak event, it does not hold strong references to the listeners allow them to be garbage collected.
		///		As a result the client does not have to unsubscribe from the event when last reference is removed.
		///		As a consequence, do not provide an anonymous method as an event handler that captures a variable.
		///		In this case, the delegate's target object is the closure, which can be immediately collected because there are no other references to it.
		///		<code>
		///			string localVariable = "Problem detected";
		///			reader.SeekStatus += delegate { Console.WriteLine(localVariable); };
		///		</code>
		///		Does not work in partial trust because it uses reflection on private methods.
		/// </remarks>
		/// <seealso cref="PositionRestoreStatusEventArgs"/>
		public event EventHandler<PositionRestoreStatusEventArgs> SeekStatus
		{
			add { _seekStatusEvent.Add(value); }
			remove { _seekStatusEvent.Remove(value); }
		}

		/// <summary>
		///		Add a folder to the list of folders being read and prepare it for reading if the reader has data
		/// </summary>
		/// <param name="folder">
		///		Repository folder
		/// </param>
		/// <returns>
		///		<see langword="false"/> - the folder is already being read
		///		<see langword="true"/> otherwise
		/// </returns>
		/// <remarks>
		///		Starting new folder from last read item time, not next item time to be consistent
		///		if no items has been read yet, using last seek time; this works transparently with restoring position;
		///		say a reader was sought but there was no data or reading was interrupted; the reader should provide a position
		///		upon restoration of which later all items added since the position was saved and which would have been read
		///		had the seek been called after they were added, would be read by the reader.
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public bool AddFolder(IRepositoryFolder folder)
		{
			CheckNotDisposed();
			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, Repository);
			// position if reading is under way; otherwise Seek will have to be called before reading
			// all additions should go through AddFolderImpl
			FolderReadingPosition position = null;
			if (IsPositioned)
			{
				position = new FolderReadingPosition(folder, _position.Time);
			}
			return AddFolderImpl(folder, position);
		}

		/// <summary>
		///		Add a folder to the list of folders being read and prepare it for reading from the specified position
		/// </summary>
		/// <param name="folder">
		///		Repository folder
		/// </param>
		/// <param name="seekTime">
		///		Seek timestamp for the folder
		/// </param>
		/// <returns>
		///		<see langword="false"/> - the folder is already being read
		///		<see langword="true"/> otherwise
		/// </returns>
		/// <remarks>
		///		Any subsequent Seek overrides the position used here
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public bool AddFolder(IRepositoryFolder folder, DateTime seekTime)
		{
			CheckNotDisposed();
			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, Repository);
			// all additions should go through AddFolderImpl
			return AddFolderImpl(folder, new FolderReadingPosition(folder, seekTime));
		}

		/// <summary>
		///		Add folder and restore its reading position. Folder is identified by its <see cref="IRepositoryFolder.FolderKey"/>
		///		, which is specified by <see cref="IFolderReadingPosition.FolderKey"/>
		/// </summary>
		/// <param name="folderPosition">
		///		Folder reading position
		/// </param>
		/// <returns>
		///		<see langword="true"/> - success
		///		<see langword="false"/> - folder is already being read
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public bool AddFolder(IFolderReadingPosition folderPosition)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(folderPosition, "folderPosition");
			IRepositoryFolder folder = Repository.RootFolder.GetDescendant(folderPosition.FolderKey, false);
			Check.DoAssertLambda(folder != null
				, () => new ArgumentException(string.Format(StorageResources.FolderNotFound, folderPosition.FolderKey)));

			return AddFolderImpl(folder, folderPosition);
		}

		/// <summary>
		///		Check whether the reader is accessing data in the <paramref name="folder"/> or any of its descendants.
		///		Descandants include <paramref name="folder" /> itself.
		/// </summary>
		/// <param name="folder">
		///		<see cref="IRepositoryFolder"/> instance representing the subtree (the folder and all its descendants) root
		/// </param>
		/// <param name="subtree">
		///		<code>bool</code> indicating whether to check access to any of the descendants of <paramref name="folder"/> (<see langword="true"/>)
		///		or just <paramref name="folder"/> itself.
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the accessor is accessing data in the specified folder or folders
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="folder"/> is <see langword="null"/>
		/// </exception>
		/// <exception cref="DifferentRepositoriesException">
		///		<paramref name="folder"/> is not attached to the same <see cref="IRepositoryManager"/> instance
		///		(<see cref="Repository"/>)
		/// </exception>
		public bool IsAccessing(IRepositoryFolder folder, bool subtree)
		{
			Check.DoRequireArgumentNotNull(folder, "folder");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, Repository);

			bool retval = false;
			if (subtree)
			{
				for (
					var e = _readers.Values.GetEnumerator();
					!retval && e.MoveNext();
					retval = e.Current.TargetFolder.IsDescendantOf(folder))
				{ }
			}
			else
			{
				retval = null != GetExistingReader(folder.FolderKey);
			}
			return retval;
		}

		/// <summary>
		///		<see cref="IRepositoryReader.RemoveFolder"/>
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public void RemoveFolder(IRepositoryFolder folder)
		{
			CheckNotDisposed();
			Util.Check.DoRequireArgumentNotNull(folder, "folder");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, Repository);
			RepositoryFolderReader reader = GetExistingReader(folder.FolderKey);
			Util.Check.DoRequire(reader != null, "The folder is not being read by the reader");

			_position.Remove(reader.Position);

			reader.Unload();

			bool topReaderRemoved = this.HasData && TopReader == reader;

			bool success = !(!_exhaustedReaders.Remove(reader) && !_offlineQueue.Remove(reader) && !_onlineQueue.Remove(reader));
			Util.Check.Ensure(success, "Internal error: reader not found.");

			success = _readers.Remove(folder.FolderKey);
			Util.Check.Ensure(success);

			if (topReaderRemoved)
			{
				SyncOfflineQueueForReading();
			}

			Invariant();
		}

		/// <summary>
		///		<see cref="IRepositoryReader.Folders"/>
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public ICollection<IRepositoryFolder> Folders
		{
			get
			{
				CheckNotDisposed();
				return (from r in _readers.Values select r.TargetFolder).ToArray();
			}
		}

		/// <summary>
		///		The comparer to use for sorting data items with equal timestamps.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		Setting value after reader has been disposed.
		/// </exception>
		public IComparer<IDataItem> DataItemComparer
		{
			get
			{
				return _onlineReaderComparer.DataItemComparer;
			}
			set
			{
				CheckNotDisposed();
				_onlineReaderComparer.DataItemComparer = value;
			}
		}

		/// <summary>
		///		Get the time comparer according to <see cref="Direction"/>
		/// </summary>
		public IDirectedTimeComparison TimeComparer
		{
			get { return _onlineReaderComparer.PrimaryComparer; }
		}

		/// <summary>
		///		Get or set reading direction (chronologically)
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		Setting value after reader has been disposed.
		/// </exception>
		public EnumerationDirection Direction
		{
			get
			{
				return _onlineReaderComparer.Direction;
			}
			set
			{
				CheckNotDisposed();
				Invariant();
				ChangeDirection(value, false);
			}
		}

		/// <summary>
		///		Check whether <see cref="Direction"/> can be changed
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public bool CanChangeDirection
		{
			get
			{
				CheckNotDisposed();
				foreach (RepositoryFolderReader reader in _onlineQueue)
				{
					if (!reader.CanChangeDirection)
					{
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>
		///		Get next item (which will be returned by <see cref="Read"/>) timestamp.
		///		Returns <code>this.TimeComparer.MaxValue</code> is there's no more data
		///		(<see cref="HasData"/>) or <see cref="Seek"/> has not yet been called.
		///		<see cref="IRepositoryReader.NextItemTimestamp"/>
		/// </summary>
		public DateTime NextItemTimestamp
		{
			get
			{
				if (this.HasData)
				{
					return CurrentItemTimestamp;
				}
				else
				{
					return TimeComparer.MaxValue;
				}
			}
		}

		/// <summary>
		///		Get current reading position
		/// </summary>
		/// <remarks>
		///		This is a singleton property so if you save the reference and then call <see cref="Read"/>
		///		the referenced instance will be updated. Call <see cref="ICloneable.Clone"/> to get a copy of it.
		///		The returned object is serializable.
		/// </remarks>
		public IReadingPosition Position
		{ get { return _position; } }

		/// <summary>
		///		Get whether there is data item to read.
		/// </summary>
		public bool HasData
		{
			get
			{
				return (_onlineQueue.Count > 0 && this.TopReader.CurrentItem != null);
			}
		}

		/// <summary>
		///		Get ready to read starting from the specified timestamp (see <paramref name="seekTime"/>)
		/// </summary>
		/// <param name="seekTime">
		///		The timestamp from which to start reading, inclusive
		/// </param>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <see cref="Folders"/> collection is empty.
		/// </exception>
		public void Seek(DateTime seekTime)
        {
			CheckNotDisposed();
			Check.DoCheckOperationValid(_readers.Count > 0, () => StorageResources.ReaderTargetFoldersMissing);

			Reset();

			_position.Update(seekTime, Direction);

			IsPositioned = true;

            foreach (RepositoryFolderReader rdr in _readers.Values)
            {
				SeekFolderReader(rdr, new FolderReadingPosition(rdr.TargetFolder, seekTime));
				// TODO: think how to avoid reloading the same file
            }

			// now all potent readers are in sorted offline queue
			// need to open, load and seek top file in the offline queue
			SyncOfflineQueueForReading();
		}

		/// <summary>
		///		Restore reading position.
		/// </summary>
		/// <param name="position">
		///		Position to restore.
		/// </param>
		/// <remarks>
		///		The <paramref name="position"/> will usually contain positions for individual folders, in which case
		///		the reader will be closed and position will be restored completely; regardless of the state of the reader
		///		before the call to this method reader will get ready to read the folders from the <paramref name="position"/>.
		///		However, the position may be used to store information about time and direction only
		///		(<see cref="IReadingPosition.ContainsFolderPositions"/> will return <see langword="false"/> in this case).
		///		In such scenario the reader will continue reading folders it was reading before the call to this method, only time
		///		and direction will be changed.
		///		If the <paramref name="position"/> does contain precise positions of individual folders, the full restoration
		///		will be deferred and the outcome can be monitored by subscribing to <see cref="SeekStatus"/>.
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		/// <seealso cref="RepositoryReader.Position"/>
		/// <seealso cref="PositionRestoreStatusEventArgs"/>
		public void Seek(IReadingPosition position)
		{
			CheckNotDisposed();
			Check.Invariant(null != _position);

			Direction = position.Direction;

			if (!position.ContainsFolderPositions)
			{
				Seek(position.Time);
			}
			else
			{
				CloseReading();
				_position.Time = position.Time;

				IsPositioned = true;

				foreach (IFolderReadingPosition folderPosition in position.FolderPositions.Values)
				{
					AddFolder(folderPosition);
				}

				// now all potent readers are in sorted offline queue
				// need to open, load and seek top file in the offline queue
				SyncOfflineQueueForReading();
			}
		}

		/// <summary>
		///		Read next available data item
		/// </summary>
		/// <returns>
		///		<see cref="IDataItemRead"/>
		///		<see langword="null"/> if the end is reached (<see cref="HasData"/> will return <see langword="false"/>)
		/// </returns>
		/// <remarks>
		///		The order of data items coming from multiple folders is guaranteed only if item timestamps are different.
		///		If more than one folder being read contains data item with a particular timestamp
		///		the order in which those data items are read may change next time you read same data from those same folders.
		///		Use <see cref="IRepositoryFolder.LastTimestamp"/> to position precisely every single data source.
		///		<seealso cref="IRepositoryReader.GetLastItemTimestamp"/>
		///		<seealso cref="IDataItemRead.RepositoryFolder"/>
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public IDataItemRead Read()
		{
			CheckNotDisposed();
			IDataItemRead retval = null;
			if (this.HasData)
			{
				retval = new DataItemRead
					{
						DataItem = TopReader.CurrentItem,
						RepositoryFolder = TopReader.TargetFolder
					};

				TopReader.UpdatePosition();
				_position.Update(retval.DataItem);

				MoveTopReaderToNextItem();
			}
			return retval;
		}

		/// <summary>
		///		Get last (logically, according to <see cref="Direction"/> data item timestamp in all folders.
		/// </summary>
		/// <returns>
		///		<code>System.DateTime</code>
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		public DateTime GetLastItemTimestamp()
		{
			CheckNotDisposed();
			Util.Check.DoRequire(_readers.Count > 0, "The collection of folders being read is empty");

			if (Direction == EnumerationDirection.Forwards)
			{
				return _readers.Values.Select((r) => r.TargetFolder.LastTimestamp).Max();
			}
			return _readers.Values.Select((r) => r.TargetFolder.FirstTimestamp).Min();
		}

		/// <summary>
		///		Stop reading, unload data, clear collection of folders being read. This is the interface method. Equivalent to Dispose.
		/// </summary>
		/// <remarks>
		///		Subscriptions to <see cref="SeekStatus"/> are cancelled here.
		/// </remarks>
		public void Close()
		{
			if (!IsDisposed)
			{
				CloseReading();
				_seekStatusEvent.RemoveAllSubscribers();
				//TODO: maybe registering and unregistering need to be done in the same class, in one place
				Repository.UnRegisterReader(this);
				IsDisposed = true;
			}
		}

		/// <summary>
		///		Callback for reporting deferred seek status for folder readers
		/// </summary>
		/// <param name="status">
		///		Status
		/// </param>
		/// <remarks>
		///		To minimise resources consumption data is loaded just before it is required. Therefore when restoring positions
		///		<see cref="Seek(IReadingPosition)"/> the result may become available long after the call to the <see cref="Seek(IReadingPosition)"/>.
		///		This callback provides folder readers a way of reporting any issues when they are encountered.
		/// </remarks>
		public void SeekStatusCallback(Storage.FolderSeekStatus status)
		{
			_seekStatusEvent.Raise(this, new PositionRestoreStatusEventArgs(status));
		}

		#endregion IRepositoryReader Members ----------------------------------

		#region private properties --------------------------------------------

		/// <summary>
		///		Whether the reader is at all positioned; it's not until the first seek or after <see cref="Reset"/>
		/// </summary>
		private bool IsPositioned
		{ get; set; }

		private DateTime CurrentItemTimestamp
		{ get { return TopReader.CurrentItem.DateTime; } }

		/// <summary>
		///		Get reader at the top of online queue. It will contain next item to read. Online queue must be sorted.
		///		Returns <see langword="null"/> if the online queue is empty
		/// </summary>
		private RepositoryFolderReader TopReader
		{
			get
			{
				if (_onlineQueue.Count == 0)
				{
					return null;
				}
				return _onlineQueue.First.Value;
			}
		}

		/// <summary>
		///		Returns first offline reader waiting to be brought online. Returns <see langword="null"/> if the offline queue
		///		is empty.
		/// </summary>
		private RepositoryFolderReader FirstOfflineReader
		{
			get
			{
				if (_offlineQueue.Count == 0)
				{
					return null;
				}
				return _offlineQueue.First.Value;
			}
		}

		/// <summary>
		///		Get first available item timestamp in the offline queue.
		///		Returns <code>this.TimeComparer.MaxValue</code> if the queue is empty.
		/// </summary>
		private DateTime FirstTimestampInOfflineQueue
		{
			get
			{
				if (_offlineQueue.Count > 0)
				{
					Util.Check.Require(FirstOfflineReader.NextFileFound
						, "Offline queue must only contain successfully sought readers");
					return FirstOfflineReader.NextFileFirstTimestampToRead.Value;
				}
				return TimeComparer.MaxValue;
			}
		}

		private IComparer<DateTime> TimestampComparer
		{ get { return _offlineReaderComparer.Comparer; } }

		#endregion private properties -----------------------------------------

		#region private methods -----------------------------------------------

		/// <summary>
		///		After advancing current position to next item in online queue
		///		check the offline queue and open top reader if it contains
		///		items with timestamp lower or equal to the <see cref="TopReader"/>
		///		or just open the first file in the queue if the online queue
		///		is empty
		/// </summary>
		/// <remarks>
		///		Call it after returning item from top online reader or after
		///		initial seek populating the offline queue.
		/// </remarks>
		private void SyncOfflineQueueForReading()
		{
			// need to repeat because FirstTimestampInOfflineQueue is not necessarily what we will get after loading the file
			// because it's potential; the actual item timestamp may be further from the seek time but we only find it out
			// after loading the file
			while (_offlineQueue.Count > 0
				&& (
					TopReader == null																			// whatever is in the offline queue is is better than nothing
					|| TimestampComparer.Compare(CurrentItemTimestamp, this.FirstTimestampInOfflineQueue) >= 0	// offline is potentially prior to top reader
				))
			{
				DateTime firstOfflineTimestamp = this.FirstTimestampInOfflineQueue;
				// if DataItemComparer is set we need to load all readers with the top timestamp
				// to enforce order
				do
				{
					RepositoryFolderReader readerToGoOnline = _offlineQueue.First.Value;
					if (readerToGoOnline.LoadNextFile())
					{
						_onlineQueue.AddFirst(readerToGoOnline);
						// may need to update position when timestamps are equal and the DataItemComparer is set
						UpdateTopReaderPosition();
					}
					else
					{
						AddToExhaustedImpl(readerToGoOnline);
					}
					_offlineQueue.RemoveFirst();
				}
				while (this.FirstTimestampInOfflineQueue == firstOfflineTimestamp && this.DataItemComparer != null);
			}
		}

		private int OnlineQueueSize
		{ get { return _onlineQueue.Count; } }

		/// <summary>
		///		Move top reader up the online queue to restore sorting order.
		///		All readers except the top one must be in sorted order.
		/// </summary>
		private void UpdateTopReaderPosition()
		{
			if (OnlineQueueSize > 1)
			{
				LinkedListNode<RepositoryFolderReader> predecessor =
					FindPredecessorInQueue(_onlineQueue.First.Next, TopReader, _onlineReaderComparer);

				if (null != predecessor)
				{
					_onlineQueue.AddAfter(predecessor, TopReader);
					_onlineQueue.RemoveFirst();
				}
			}
		}

		/// <summary>
		///		Move top reader to the offline queue maintaining its sorting
		///		The queue must be sorted.
		/// </summary>
		private void MoveTopReaderToOfflineQueue()
		{
			AddReaderToOfflineQueue(TopReader);
			_onlineQueue.RemoveFirst();
		}

		private void AddToExhaustedImpl(RepositoryFolderReader reader)
		{
			reader.Unload();
			_exhaustedReaders.AddLast(reader);
		}

		private void MoveTopReaderToExhaustedList()
		{
			Util.Check.Require(TopReader.EndReached, "Internal error: reader is not yet exhausted");

			AddToExhaustedImpl(TopReader);

			_onlineQueue.RemoveFirst();
		}

		/// <summary>
		///		Add the specified reader to the offline queue
		/// </summary>
		/// <param name="reader">
		///		The reader to add. Its <see cref="RepositoryFolderReader.NextFileFound"/>
		///		must equal <see langword="true"/>.
		/// </param>
		private void AddReaderToOfflineQueue(RepositoryFolderReader reader)
		{
			Util.Check.Require(reader.NextFileFound, "Internal error: reader unsuitable for offline queue");

			// free memory
			reader.Unload();

			LinkedListNode<RepositoryFolderReader> predecessor = null;
			if (_offlineQueue.Count > 0)
			{
				predecessor = FindPredecessorInQueue(_offlineQueue.First, reader, _offlineReaderComparer);
			}
			if (null == predecessor)
			{
				_offlineQueue.AddFirst(reader);
			}
			else
			{
				_offlineQueue.AddAfter(predecessor, reader);
			}
		}

		/// <summary>
		///		Find predecessor for the <paramref name="reader"/> in the queue
		///		of readers sorted by <paramref name="readerComparer"/> ascending starting
		///		from <paramref name="queueStart"/>
		/// </summary>
		/// <param name="queueStart">
		///		The first element in the queue to start lookup from; all readers
		///		in the queue must have current item
		/// </param>
		/// <param name="reader">
		///		The reader to find place for in the queue
		/// </param>
		/// <returns>
		///		<see langword="null"/> if the <paramref name="queueStart"/> is
		///		positioned to a greater item than <paramref name="reader"/>
		///		i.e. <paramref name="reader"/> must be before <paramref name="queueStart"/>
		///		otherwise the first element in the queue which is less than the
		///		<paramref name="reader"/>
		/// </returns>
		private LinkedListNode<RepositoryFolderReader> FindPredecessorInQueue(
			LinkedListNode<RepositoryFolderReader> queueStart
			, RepositoryFolderReader reader
			, IComparer<RepositoryFolderReader> readerComparer)
		{
			LinkedListNode<RepositoryFolderReader> retval;
			if (readerComparer.Compare(queueStart.Value, reader) >= 0)
			{
				// the reader must be before the first item in the queue
				return null;
			}

			for (
				retval = queueStart
				; retval.Next != null && readerComparer.Compare(retval.Next.Value, reader) < 0
				; retval = retval.Next)
			{
				Util.Check.Ensure(readerComparer.Compare(retval.Value, reader) < 0
					, "Internal logic error; queue may not be sorted properly");
			}
			return retval;
		}

		/// <summary>
		///		After top reader returned last data item in a file
		/// </summary>
		private void OnTopReaderFileFinished()
		{
			if (!this.TopReader.EndReached)
			{
				if (_onlineQueue.First.Next == null
					|| TimestampComparer.Compare(
						_onlineQueue.First.Next.Value.CurrentItem.DateTime
						, TopReader.NextFileFirstTimestampToRead.Value) >= 0)
				{
					// there's no next reader or it contains older or equal item as the first item in
					// the top reader's next file
					// second in the queue must remain second; load next data file in the top reader
					TopReader.LoadNextFile();

					// update may be required because if data item timestamps are equal the DataItemComparer may change the order
					UpdateTopReaderPosition();
				}
				else
				{
					// second reader is present and it has to move to the top
					MoveTopReaderToOfflineQueue();
				}
			}
			else
			{
				MoveTopReaderToExhaustedList();
			}
		}

		private RepositoryFolderReader GetExistingReader(string folderKey)
		{
			RepositoryFolderReader retval;
			_readers.TryGetValue(folderKey, out retval);
			return retval;
		}

		/// <summary>
		///		Unload all readers and reset position. After a call to this method
		///		you need to call <see cref="Seek"/> to start reading again. It is different from Close in that it keeps all
		///		folder readers and allows to start reading the same folders again.
		/// </summary>
		private void Reset()
		{
			foreach (RepositoryFolderReader rdr in _readers.Values)
			{
				rdr.Unload();
			}
			_exhaustedReaders.Clear();
			_offlineQueue.Clear();
			_onlineQueue.Clear();

			IsPositioned = false;

			Util.Check.Ensure(!this.HasData);
			Util.Check.Ensure(this.NextItemTimestamp == TimeComparer.MaxValue);

		}

		/// <summary>
		///		Close reading operations; subscribers to <see cref="SeekStatus"/> remain subscribed.
		/// </summary>
		private void CloseReading()
		{
			Reset();
			_readers.Clear();
			_position.Clear();
		}

		/// <summary>
		///		Initiate reading from the <paramref name="reader"/>.
		///		It must not be in any of the 2 queues (online, offline) or exhausted list.
		///		If the file contains data since <paramref name="seekTime"/> the reader will
		///		be placed into offline queue. Otherwise it will go into exhausted readers list.
		/// </summary>
		/// <param name="reader">
		///		Reader to initiate. It must not be in any of the 2 queues (online, offline) or exhausted list.
		/// </param>
		/// <param name="position">
		///		Position to start reading from.
		/// </param>
		/// <remarks>
		///		The method ensures that currently loaded reader's data is unloaded first.
		///		Seeking is done lazily - all go to offline queue and loaded immediately
		///		before reading.
		/// </remarks>
		private void SeekFolderReader(RepositoryFolderReader reader, IFolderReadingPosition position)
		{
			Check.RequireArgumentNotNull(position, "position");
			Check.RequireArgumentNotNull(reader, "reader");
			Check.Require(!_offlineQueue.Contains(reader) && !_exhaustedReaders.Contains(reader) && !_onlineQueue.Contains(reader));

			reader.Unload();
			reader.SeekDataFile(position);
			AddPositionedOfflineReaderToAQueue(reader);

			// as reader.Position is singleton, changes to the reader's position do not require modifying _position
			_position.SetFolderPosition(reader.Position);
		}

		/// <summary>
		///		Add a positioned offline reader to an appropriate queue
		/// </summary>
		/// <param name="reader">
		///		The reader must be added to all readers collection as necessary; not added here
		/// </param>
		/// <remarks>
		///		The reader will go into either offline queue or exhausted readers collection
		/// </remarks>
		private void AddPositionedOfflineReaderToAQueue(RepositoryFolderReader reader)
		{
			if (reader.NextFileFound)
			{
				AddReaderToOfflineQueue(reader);
			}
			else
			{
				AddToExhaustedImpl(reader);
			}
		}

		private void MoveTopReaderToNextItem()
		{
			if (this.TopReader.HasMoreItemsInCurrentFile)
			{
				this.TopReader.MoveNext();
				UpdateTopReaderPosition();
			}
			else
			{
				// no more items in the top reader's current file
				OnTopReaderFileFinished();
			}

			SyncOfflineQueueForReading();
		}

		private void ChangeDirection(EnumerationDirection newDirection, bool force)
		{
			Check.DoAssertLambda(CanChangeDirection
				, () => new InvalidOperationException(StorageResources.CannotChangeReadingDirectionWhenNonSequential));

			if (newDirection != Direction || force)
			{
				_offlineReaderComparer.Direction = newDirection;
				_onlineReaderComparer.Direction = newDirection;

				LinkedList<RepositoryFolderReader> onlineQueue = new LinkedList<RepositoryFolderReader>(_onlineQueue);

				LinkedList<RepositoryFolderReader> exhaustedReaders = new LinkedList<RepositoryFolderReader>(_exhaustedReaders);
				LinkedList<RepositoryFolderReader> offlineQueue = new LinkedList<RepositoryFolderReader>(_offlineQueue);
				_exhaustedReaders.Clear();
				_onlineQueue.Clear();
				_offlineQueue.Clear();

				// first, reverse all online readers
				foreach (RepositoryFolderReader reader in onlineQueue)
				{
					reader.Direction = newDirection;
					if (!reader.HasMoreItemsInCurrentFile)
					{
						AddReaderToOfflineQueue(reader);
					}
					else
					{
						// make reader top; it has data loaded and we need to preserve position
						_onlineQueue.AddFirst(reader);
						// get ready to start from last read item
						MoveTopReaderToNextItem();
					}
				}

				// next, reverse offline and exhausted readers
				foreach (RepositoryFolderReader reader in offlineQueue.Concat(exhaustedReaders))
				{
					reader.Direction = newDirection;
					AddPositionedOfflineReaderToAQueue(reader);
				}

				_position.Direction = newDirection;

				SyncOfflineQueueForReading();
			}
			Invariant();
		}

		/// <summary>
		///		Add folder to read and optionally prepare it for reading
		/// </summary>
		/// <param name="folder">
		///		Folder to add
		/// </param>
		/// <param name="position">
		///		<see langword="null"/> means do not prepare it for reading
		/// </param>
		/// <returns>
		///		<see langword="false"/> - the folder is already being read
		///		<see langword="true"/> otherwise
		/// </returns>
		/// <remarks>
		///		If reader has data (<see cref="HasData"/>) <paramref name="position"/> must have value
		/// </remarks>
		private bool AddFolderImpl(IRepositoryFolder folder, IFolderReadingPosition position)
		{
			Check.RequireArgumentNotNull(folder, "folder");

			IFolder folderTyped = RepositoryFolder.CastFolder(folder);

			Check.Require(object.ReferenceEquals(folder.Repository, Repository));
			Check.Require(!HasData || position != null, "If we have data we cannot leave a reader unpositioned");

			if (IsAccessing(folder, false))
			{
				return false;
			}

			Check.Require(!_position.FolderPositions.ContainsKey(folder.FolderKey)
				, "Folder position found in repository reader position for a folder not being read");

			RepositoryFolderReader reader = new RepositoryFolderReader(folderTyped, this);
			reader.Direction = this.Direction;

			_readers.Add(folder.FolderKey, reader);

			if (position != null)
			{
				SeekFolderReader(reader, position);
			}

			return true;
		}

		/// <summary>
		///		Check invariant assertions
		/// </summary>
		[Conditional("DBC_CHECK_ALL"),
		Conditional("DBC_CHECK_INVARIANT")]
		private void Invariant()
		{
			const string inconsistentDirection = "Inconsistent direction";
			Util.Check.Invariant(Direction == _offlineReaderComparer.Direction, inconsistentDirection);
			Util.Check.Invariant(_onlineReaderComparer.Direction == _offlineReaderComparer.Direction, inconsistentDirection);
			Util.Check.Invariant(Direction == _position.Direction, inconsistentDirection);
			Util.Check.Invariant(_position.FolderPositions.Count == _readers.Count, "Folder and positions counts do not match");

			foreach (RepositoryFolderReader reader in _readers.Values)
			{
				Util.Check.Invariant(reader.Direction == Direction, inconsistentDirection);
			}
		}

		public bool IsDisposed
		{ get; private set; }

		/// <summary>
		///		Throws ObjectDisposedException if this reader or the repository instance has been disposed.
		/// </summary>
		private void CheckNotDisposed()
		{
			CheckHelper.CheckRepositoryNotDisposed(this);
			Check.DoAssertLambda(!IsDisposed, () => new ObjectDisposedException(StorageResources.ReaderIsDisposed, (Exception)null));
		}

		#endregion private methods --------------------------------------------

		public void Dispose()
		{
			Close();
		}
	}
}
