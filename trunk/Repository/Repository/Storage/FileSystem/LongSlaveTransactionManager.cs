using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;

namespace bfs.Repository.Storage.FileSystem
{
	/// <summary>
	///		Utility class manages storage ambient transactions taking care of long running ambient managed transaction.
	/// </summary>
	/// <remarks>
	///		The class keeps track of pending storage transaction if it is a slave of a master managed transaction
	///		and thus makes completion notification reach components dependent on them, such as repository writers.
	/// </remarks>
	internal class LongSlaveTransactionManager : IDisposable, ITransactionNotification
	{
		private static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(LongSlaveTransactionManager).Name);

		private ITransactionNotification _subscriber;
		private IFileSystemProvider _fileSystemProvider;
		private volatile IFileSystemTransaction _pendingTransaction;

		/// <summary>
		///		Create new instance
		/// </summary>
		/// <param name="repository">
		///		The repository transactions will operate in.
		/// </param>
		/// <param name="notificationSubscriber">
		///		Optional external subscriber.
		/// </param>
		public LongSlaveTransactionManager(IRepository repository, ITransactionNotification notificationSubscriber)
		{
			Repository = repository;
			_fileSystemProvider = Repository.ObjectFactory.FileSystemProvider;
			_subscriber = notificationSubscriber;
		}

		private bool IsDisposed
		{ get; set; }

		public IRepository Repository
		{ get; private set; }

		public IFileSystemTransaction PendingTransaction
		{ get { return _pendingTransaction; } private set { _pendingTransaction = value; } }

		public IFileSystemTransaction AmbientTransaction
		{ get { return _fileSystemProvider.AmbientTransaction; } }

		/// <summary>
		/// 	Whether a transaction used to flush data before is still pending.
		/// </summary>
		/// <remarks>
		/// 	Cleans up transaction registered but disposed. If this method returns false, _transactionManager
		/// 	will be null
		/// </remarks>
		/// <returns>
		/// 	true if _transactionManager is not null and active
		/// 	otherwise false.
		/// </returns>
		public bool IsTransactionPending()
		{
			CheckNotDisposed();
			bool retval = PendingTransaction != null;
			if (retval)
			{
				retval = PendingTransaction.IsActive;
				if (!retval)
				{
					PendingTransaction = null;
				}
			}
			return retval;
		}

		/// <summary>
		///		Get non-lazy transaction scope reusing pending transaction if already pending.
		/// </summary>
		/// <returns>
		///		New eager scope.
		/// </returns>
		/// <remarks>
		///		Eager scope is for complete units of work such as <see cref="IRepositoryWriter.Flush()"/>.
		///		Lazy scope is for the methods which need to take part in external [long] transactions but do not represent units of
		///		work themselves, such as <see cref="IRepositoryWriter.Write(IDataItem)"/>
		///		Pending transaction is registered and subscribed to only first time when returned scope is not owning the transaction
		/// </remarks>
		public StorageTransactionScope GetTransactionScope()
		{
			return GetTransactionScope(false);
		}

		/// <summary>
		///		Get lazy transaction scope reusing pending transaction if already pending.
		/// </summary>
		/// <returns>
		///		New lazy scope.
		/// </returns>
		/// <remarks>
		///		Lazy scope is for the methods which need to take part in external [long] transactions but do not represent units of
		///		work themselves, such as <see cref="IRepositoryWriter.Write(IDataItem)"/>.
		///		Eager scope is for complete units of work such as <see cref="IRepositoryWriter.Flush()"/>.
		///		Pending transaction is registered and subscribed to only first time when returned scope is not owning the transaction
		/// </remarks>
		public StorageTransactionScope GetLazyTransactionScope()
		{
			return GetTransactionScope(true);
		}

		/// <summary>
		///		Get transaction scope reusing pending transaction if already pending.
		/// </summary>
		/// <param name="lazy">
		///		Whether the scope needs to be lazy or eager.
		/// </param>
		/// <returns>
		///		New scope.
		/// </returns>
		/// <remarks>
		///		Eager scope is for complete units of work such as <see cref="IRepositoryWriter.Flush()"/>.
		///		Lazy scope is for the methods which need to take part in external [long] transactions but do not represent units of
		///		work themselves, such as <see cref="IRepositoryWriter.Write(IDataItem)"/>
		///		Pending transaction is registered and subscribed to only first time when returned scope is not owning the transaction
		/// </remarks>
		private StorageTransactionScope GetTransactionScope(bool lazy)
		{
			CheckNotDisposed();

			StorageTransactionScope scope;

			if (lazy)
			{
				scope = StorageTransactionScope.CreateLazy(Repository, PendingTransaction);
			}
			else
			{
				scope = StorageTransactionScope.Create(Repository, PendingTransaction);
			}

			if (!scope.IsNullScope && !scope.IsTransactionOwner && PendingTransaction == null)
			{
				PendingTransaction = scope.UnderlyingTransaction;
				if (_subscriber != null)
				{
					PendingTransaction.Subscribe(_subscriber);
				}
				PendingTransaction.Subscribe((ITransactionNotification)this);
				Check.Ensure(!scope.ToBeDisposed, "Pending transaction must not be disposed - we need to keep the single instance of slave pending transaction");
			}
			return scope;
		}

		/// <summary>
		///		Scrap pending transaction
		/// </summary>
		/// <param name="dispose">
		///		Dispose it before throwing away
		/// </param>
		public void ScrapPendingTransaction(bool dispose)
		{
			IFileSystemTransaction pendingTransaction = _pendingTransaction;
			if (pendingTransaction != null)
			{
				if (dispose)
				{
					pendingTransaction.Dispose();
				}
				_pendingTransaction = null;
			}
		}

		public void Dispose()
		{
			IsDisposed = true;
			ScrapPendingTransaction(true);
		}

		/// <summary>
		///		Returns <see langword="true"/> if ambient storage transaction is active and can potentially span multiple client calls to repository.
		/// </summary>
		public bool CanIOTransactionSpanMultipleRepositoryCalls
		{
			get
			{
				return _fileSystemProvider.IsStorageAmbientTransactionActive
					&& !StorageTransactionScope.IsTopLevelScopeTransactionOwner
					&& StorageTransactionScope.TopLevelScope != null
					&& !StorageTransactionScope.TopLevelScope.IsNullScope;
			}
		}

		void ITransactionNotification.TransactionCompleted(IFileSystemTransaction transaction, bool committed)
		{
			_log.Debug("LongSlaveTransactionManager notified");
			Check.Require(_pendingTransaction != null);
			//do not want to dispose when notified by the transaction because it already knows it itself
			ScrapPendingTransaction(false); ;
		}

		void ITransactionNotification.Prepare()
		{
			// no preparation required
		}

		private void CheckNotDisposed()
		{
			Check.DoAssertLambda(!IsDisposed, () => new ObjectDisposedException(GetType().FullName));
		}
	}
}
