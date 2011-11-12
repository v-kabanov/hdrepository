//-----------------------------------------------------------------------------
// <created>3/18/2010 10:29:13 AM</created>
// <author>Vasily Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;
using System.Collections.ObjectModel;
using bfs.Repository.Storage.FileSystem;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Writes to exactly one repo folder
	/// </summary>
	[DebuggerDisplay("{_currentAccessor};unsaved:{UnsavedItems}")]
	internal class DirectSingleFolderWriter : ITransactionNotification, IDisposable
	{
		private IFolder _targetFolder;

		private IDataFileAccessor _currentAccessor;

		private IDataFolder _currentFolder;

		private bool _dirty;

		private ICoder _coder;

		private ICoder _encryptor;

		private IComparer<IDataItem> _equalTimestampedItemsComparer;

		private List<IDataItem> _unsavedItems;

		private bool _trackUnsavedItems;

		private int _desiredItemsPerFile;

		private LongSlaveTransactionManager _transactionManager;

		private bool _clearDirtyFlagWhenTransactionCompletes;
		
		internal DirectSingleFolderWriter(
			IFolder repoFolder
			, ICoder coder
			, ICoder encryptor)
		{
			this.Coder = coder;
			this.Encryptor = encryptor;
			_targetFolder = repoFolder;
			_dirty = false;

			_desiredItemsPerFile = repoFolder.Properties.DesiredItemsPerFile;
			if (0 >= _desiredItemsPerFile)
			{
				_desiredItemsPerFile = Constants.DefaultDataItemsPerFile;
			}

			_trackUnsavedItems = false;
			SetUpUnsavedItemsTracking();

			_transactionManager = new LongSlaveTransactionManager(_targetFolder.Repository, (ITransactionNotification)this);
		}

		/// <summary>
		///		Get the target repository folder
		/// </summary>
		public IRepositoryFolder Folder
		{ get { return _targetFolder; } }

		public ICoder Coder
		{
			get { return _coder; }
			set
			{
				Check.RequireArgumentNotNull(value, "value");
				_coder = value;
			}
		}

		public bool TrackUnsavedItems
		{
			get { return _trackUnsavedItems; }
			set
			{
				if (_trackUnsavedItems != value)
				{
					_trackUnsavedItems = value;
					SetUpUnsavedItemsTracking();
				}
			}
		}

		private void SetUpUnsavedItemsTracking()
		{
			if (TrackUnsavedItems)
			{
				if (_unsavedItems == null)
				{
					_unsavedItems = new List<IDataItem>(this.DesiredFileSize);
				}
				else
				{
					_unsavedItems.Capacity = this.DesiredFileSize;
				}
			}
			else
			{
				// save memory
				_unsavedItems = null;
			}
		}

		internal ICoder Encryptor
		{
			get
			{
				return _encryptor;
			}
			set
			{
				if (null != _currentAccessor)
				{
					_currentAccessor.Encryptor = value;
				}
				_encryptor = value;
			}
		}

		public IList<IDataItem> UnsavedItems
		{
			get
			{
				Check.DoCheckOperationValid(TrackUnsavedItems, () => StorageResources.UnsavedItemsTrackingIsOff);
				return new ReadOnlyCollection<IDataItem>(_unsavedItems);
			}
		}

		/// <summary>
		///		Get current target data file (the file which received
		///		last written data item. Note that the descriptor will contain
		///		current file name as it exists on disk or as it was instantiated.
		///		There may be in-memory changes to the file content which will
		///		alter the name when the accessor is flushed
		/// </summary>
		internal IRepositoryFileName CurrentFile
		{
			get
			{
				IRepositoryFileName retval = null;
				if (null != _currentAccessor)
				{
					retval = _currentAccessor.TargetFile;
				}
				return retval;
			}
		}

		/// <summary>
		///		Get or set comparer to use when sorting data items for items with equal timestamps.
		///		When timestamps are not equal the comparer has no effect.
		/// </summary>
		public IComparer<IDataItem> EqualTimestampedItemsComparer
		{
			get { return _equalTimestampedItemsComparer; }
			set
			{
				_equalTimestampedItemsComparer = value;
				if (_currentAccessor != null)
				{
					_currentAccessor.EqualTimestampItemsComparer = value;
				}
			}
		}


		internal void Write(IDataItem dataItem)
		{
			Check.Require(DesiredFileSize > 0, "Desired file size is invalid");
			Check.RequireArgumentNotNull(dataItem, "dataItem");

			bool written = false;
			// locking to prevent simultaneous flush
			// transaction is necessary within Seek, there's no IO outside it
			lock (this)
			{
				// scope which is dummy unless there's client ambient transaction (pending)
				using (var scope = _transactionManager.GetLazyTransactionScope())
				{

					_clearDirtyFlagWhenTransactionCompletes = false;

					if (null != _currentAccessor)
					{
						if (_currentAccessor.ItemCount < this.DesiredFileSize || _currentAccessor.IsTimestampCovered(dataItem.DateTime))
						{
							written = _currentAccessor.Add(dataItem);
						}
					}

					if (!written)
					{
						// item goes into another file
						// ensure data is stored
						CloseAccessor();

						Seek(dataItem.DateTime);
						// this MUST succeed, checked in postcondition
						written = _currentAccessor.Add(dataItem);
					}

					// it could be false here after flush
					_dirty = true;

					if (TrackUnsavedItems)
					{
						_unsavedItems.Add(dataItem);
					}

					scope.Complete();
				} // using (var scope = _transactionManager.GetLazyTransactionScope())
			} // lock (this)

			Check.Ensure(_dirty && written);
		}

		internal void Close()
		{
			CloseAccessor();
			_targetFolder = null;
			if (_transactionManager != null)
			{
				_transactionManager.Dispose();
			}
		}

		private IDataFileAccessor GetAccessor(IRepositoryFileName targetFile)
		{
			var retval = _targetFolder.Repository.ObjectFactory.GetDataFileAccessor(_currentFolder, targetFile);
			retval.Coder = Coder;
			retval.EqualTimestampItemsComparer = EqualTimestampedItemsComparer;
			return retval;
		}

		/// <summary>
		///		Find a place for data item with the specified timestamp
		/// </summary>
		/// <exception cref="System.IO.FileNotFoundException">
		///		File found in the data file container was not found on disk, by virtue of <see cref="RepositoryFileAccessor.ReadFromFile"/>.
		///		Possible concurrent update; better shut down.
		/// </exception>
		private void Seek(DateTime seekTimestamp)
		{
			if (null == _currentFolder
				|| !IsCovering(_currentFolder, seekTimestamp))
			{
				// need to change current leaf data folder
				_currentFolder = _targetFolder.RootDataFolder.GetLeafFolder(seekTimestamp, true);
			}

			IRepositoryFileName targetFile;
			IRepositoryFileName predecessor;
			IRepositoryFileName successor;

			_currentFolder.DataFileBrowser.GetDataFiles(seekTimestamp, out predecessor, out targetFile, out successor);

			bool openingExistingFile = targetFile != null;

			if (!openingExistingFile)
			{
				// new file needed
				targetFile = _targetFolder.Repository.ObjectFactory.CreateNewFile(_targetFolder);
			}

			_currentAccessor = GetAccessor(targetFile);

			if (openingExistingFile)
			{
				// will throw exception if file was not found or failed to read
				_currentAccessor.ReadFromFile();
			}

			_currentAccessor.Encryptor = this.Encryptor;

			// set timestamp limits
			if (null != predecessor)
			{
				_currentAccessor.MinTimestampToAccept = predecessor.LastItemTimestamp.AddTicks(1);
			}
			else
			{
				_currentAccessor.MinTimestampToAccept = _currentFolder.Start;
			}

			if (null != successor)
			{
				_currentAccessor.MaxTimestampToAccept = successor.FirstItemTimestamp.AddTicks(-1);
			}
			else
			{
				_currentAccessor.MaxTimestampToAccept = _currentFolder.End.AddTicks(-1);
			}

			Check.Ensure(null != _currentAccessor);
		}

		/// <summary>
		///		Get or set desired number of data items per file.
		///		Note that the size will be used as a guide. During normal sequential writing
		///		the target size will be observed exactly. But when inserting data not in order
		///		the actual file size may differ.
		/// </summary>
		internal int DesiredFileSize
		{
			get { return _desiredItemsPerFile; }
			set
			{
				Check.DoRequire(value > 0);
				_desiredItemsPerFile = value;
				SetUpUnsavedItemsTracking();
			}
		}

		private static bool IsCovering(IDataFolder dataFolder, DateTime itemTimestamp)
		{
			return dataFolder.Start <= itemTimestamp && dataFolder.End > itemTimestamp;
		}
		
		/// <summary>
		///		If previous Seek failed and writer is not in a ready
		///		state (file accessor is null) Flush does nothing
		/// </summary>
		/// <remarks>
		///		DOCO:
		///		Note that if storage transaction is rolled back the changes cannot be resubmitted if transaction is managed outside of the repository
		///		and therefore can span multiple flushes. If on the other hand the transaction is managed in repository, and therefore guaranteed to
		///		span one flush at most, the dirty flag is only cleared when the transaction ends and flush can be re-tried.
		///		Thus in the former scenario whenever transaction fails writer should be disposed immediately (possibly after salvaging unsaved
		///		items from it) much like with NHibernate's session.
		///		In the latter scenario it may possible to re-flush data after IO failure but the previous pattern may still be preferable.
		/// </remarks>
		internal void Flush()
		{
			_clearDirtyFlagWhenTransactionCompletes = false;
			bool dropUnsavedItemsExplicitly;

			// locking to prevent simultaneous Write()
			lock (this)
			{
				using (var scope = _transactionManager.GetTransactionScope())
				{
					// Transaction scope is not necessary when called from the RepositoryWriter as it must have already set the scope.
					// However, when called as a preparation for commitment of externally controlled transaction (especially master managed tran-n
					// when enlistment methods are executed on a worker thread) the scope is necessary to ensure transaction context is set up.
					dropUnsavedItemsExplicitly = scope.NoTransaction || scope.IsTransactionOwner;

					if (null != _currentAccessor && _dirty)
					{
						_currentAccessor.Encryptor = this.Encryptor;
						_currentAccessor.Coder = this.Coder;
						_currentAccessor.Flush();
					}

					scope.Complete();
				}

				// the dirty flag does not depend on transaction spanning multiple flushes; it can only be reliably left ON when local transaction is started and finished
				// in repository during a single flush;
				if (dropUnsavedItemsExplicitly || _transactionManager.CanIOTransactionSpanMultipleRepositoryCalls)
				{
					// transaction already committed or no transaction was used (which is the same)
					// or transaction will live outside repository and dirty state cannot be used for resubmition anyways
					_dirty = false;
				}
				else
				{
					// shall recieve notification of transaction completion and will have to clear dirty flag there
					_clearDirtyFlagWhenTransactionCompletes = true;
				}

				if (dropUnsavedItemsExplicitly)
				{
					DropUnsavedItems();
				}
				else
				{
					Check.Ensure(_transactionManager.PendingTransaction != null, "If there's transaction and it lives londer than scope it must be registered as pending and subscribed to");
				}
			}
		}
		
		internal void DropUnsavedItems()
		{
			if (_unsavedItems != null)
			{
				_unsavedItems.Clear();
			}
		}

		private void CloseAccessor()
		{
			if (null != _currentAccessor)
			{
				Flush();
				_currentAccessor.Close();
				_currentAccessor = null;
			}
			else
			{
				DropUnsavedItems();
			}
		}

		/// <summary>
		/// 	Must not throw exceptions
		/// </summary>
		/// <param name="committed">
		/// 	Whether transaction was committed successfully.
		/// </param>
		/// <remarks>
		///		Note that this method may be executed in a concurrent worker thread.
		/// </remarks>
		void ITransactionNotification.TransactionCompleted(IFileSystemTransaction transaction, bool committed)
		{
			if (committed)
			{
				DropUnsavedItems();
			}
			if (_clearDirtyFlagWhenTransactionCompletes)
			{
				_dirty = false;
			}
		}

		/// <summary>
		///		Prepare for transaction commitment - flush any unsaved data. To be called by pending transaction before committing
		///		if it lives and commits outside repository.
		/// </summary>
		/// <remarks>
		///		It must not be called as a result of committing the transaction in <see cref="Flush(void)"/>, otherwise
		///		there will be infinite recursion. It is currently facilitated by LongSlaveTransactionManager's implementation.
		///		Note that this method may be executed in a concurrent worker thread.
		/// </remarks>
		void ITransactionNotification.Prepare()
		{
			// how to ensure notification comes from _transactionManager.PendingTransaction?
			Check.Require(
				_transactionManager.PendingTransaction != null
				&& (
					_transactionManager.AmbientTransaction == null
					|| _transactionManager.AmbientTransaction == _transactionManager.PendingTransaction
				), "The notification is expected to come from _transactionManager.PendingTransaction and context must not contradict pending transaction.");

			//DOCO: The above check means that if this method is called pending transaction must be under way and it will be used to perform Flush
			// if required. It is provided by the transaction manager implementation contract.
			// If the pending transaction is a slave of managed one the call will be executed on its worker thread which should not have
			//  its own storage transaction context.
			// If the pending transaction is standalone storage transaction (e.g. KtmTransaction) created by client app manually outside of the repository
			//  the call to this method will come from Commit method of, say, KtmTransaction and on the same thread; the client app could potentially
			//  change the transactional context and set another storage transaction as ambient before committing the previous one. In this case the notification will
			//  fail.

			// locking because this method may be executed on a System.Transactions.Transaction's worker thread - to prevent conflict between this and
			// Write() or Flush() called by the client application
			lock (this)
			{
				if (_dirty)
				{
					Flush();
				}
			}
		}

		public void Dispose()
		{
			Close();
		}
	}
}
