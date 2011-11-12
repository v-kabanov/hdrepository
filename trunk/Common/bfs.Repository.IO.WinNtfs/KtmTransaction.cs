/*
 * User: vasily
 * Date: 25/02/2011
 * Time: 9:15 PM
 * 
 */
 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions;
using Microsoft.Win32.SafeHandles;

using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;
using bfs.Repository.Exceptions;

namespace bfs.Repository.IO.WinNtfs
{
	/// <summary>
	/// 	Description of KtmTransaction.
	/// </summary>
	public class KtmTransaction : IEnlistmentNotification, IFileSystemTransaction
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(KtmTransaction).Name);

		private static ThreadLocal<KtmTransaction> _contextTransactions = new ThreadLocal<KtmTransaction>();

		/// <summary>
		/// 	Returns current ambient KTM transaction. Returns <see langword="null"/> if none.
		/// </summary>
		/// <remarks>
		/// 	Supposed to be used by KtmTransactionScope.
		/// </remarks>
		public static KtmTransaction Current
		{
			get
			{
				if (_contextTransactions.IsValueCreated)
					return _contextTransactions.Value;
				else
					return null;
			}
			set
			{
				_contextTransactions.Value = value;
			}
		}
		
		private KtmTransactionHandle _ktmHandle;
		// never null
		private C5.HashSet<ITransactionNotification> _watchers = new C5.HashSet<ITransactionNotification>();
		private TransactionStatus _status;
		
		/// <summary>
		/// 	Create new instance.
		/// </summary>
		/// <param name="handle">
		/// 	Transaction handle
		/// </param>
		/// <param name="masterTransaction">
		///		Managed
		/// </param>
		/// <remarks>
		/// 	The only constructor ensures that the KTM transaction instance is subscribed exactly once to the
		/// 	owning transaction completion if owning transaction is specified.
		/// </remarks>
		protected KtmTransaction(KtmTransactionHandle handle, Transaction masterTransaction)
		{
			Check.DoRequireArgumentNotNull(handle, "handle");
			Check.DoAssertLambda(!handle.IsClosed && !handle.IsInvalid
				, () => new ArgumentException("Transactin handle does not represent an active transaction."));
			Check.DoAssertLambda(
				masterTransaction == null
				|| masterTransaction.TransactionInformation.Status == TransactionStatus.Active
				, () => new ArgumentException("Owning managed transaction is not active."));
			
			_ktmHandle = handle;
			MasterTransaction = masterTransaction;

			_status = TransactionStatus.Active;
			Check.Ensure(IsHandleValid);
		}

		/// <summary>
		///		Subscribe to master's TransactionCompleted event if not yet subscribed
		/// </summary>
		private void EnsureSubscribedToMaster()
		{
			if (IsSlave && !IsSubscribedToMaster)
			{
				MasterTransaction.EnlistVolatile(this, EnlistmentOptions.None);
				IsSubscribedToMaster = true;
			}
		}
		
		#region IFileSystemTransaction
		
		/// <summary>
		/// 	Gets a value indicating whether the calling thread is in transaction.
		/// 	<see cref="Current"/>
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the caller thread is in transaction; otherwise, <c>false</c>.
		/// </value>
		public static bool IsInTransaction
        { get { return Current != null && Current.IsHandleValid; } }
		
		/// <summary>
		/// 	Whether transaction handle is valid.
		/// </summary>
		/// <remarks>
		/// 	This status is different to <see cref="TransactionStatus"/> in that committing or rolling back an active
		/// 	transaction does not make it inactive.
		/// </remarks>
		public bool IsHandleValid
		{ get { return _ktmHandle != null && !_ktmHandle.IsInvalid && !_ktmHandle.IsClosed; } }

		/// <summary>
		/// 	Whether the transaction is under way.
		/// </summary>
		public bool IsActive
		{ get { return IsHandleValid && TransactionStatus == TransactionStatus.Active; } }
		
		/// <summary>
		/// 	Get whether the transaction is part of managed transaction.
		/// </summary>
		/// <remarks>
		/// 	If <see langword="true"/>, the transaction should be committed or rolled back through
		/// 	the owning managed transaction (<see cref="MasterTransaction" />) and should
		/// 	not be committed and rolled back itself.
		/// </remarks>
		public bool IsSlave
		{ get {return null != MasterTransaction; } }
		
		/// <summary>
		/// 	Get owning managed transaction
		/// </summary>
		public Transaction MasterTransaction
		{ get; private set; }
		
		/// <summary>
		/// 	Whether the transaction is part of the <see>System.Transactions.Transaction.Current</see>.
		/// </summary>
		public bool IsPartOfManagedAmbient
		{ get { return IsSlave && MasterTransaction == Transaction.Current; } }
		
		/// <summary>
		/// 	Get transaction status.
		/// </summary>
		/// <remarks>
		/// 	Immediately after creating transaction its status is "Active".
		/// 	Afterwards if transaction if this transaction has been created from the ambient managed one, its status will be
		/// 	the same as status of the <see cref="MasterTransaction"/>. Otherwise (for standalone, local transactions),
		/// 	the status will be set when calling <see cref="Commit()"/> or <see cref="Rollback()"/>
		/// </remarks>
		public TransactionStatus TransactionStatus
		{
			get
			{
				if (MasterTransaction != null)
				{
					return MasterTransaction.TransactionInformation.Status;
				}
				return _status;
			}
		}

		/// <summary>
		///		Whether the transaction is subscribed to the <see cref="MasterTransaction"/>'s transaction ended event.
		/// </summary>
		/// <remarks>
		///		If <see cref="IsSlave"/> is <see langword="false"/> this will always return <see langword="false"/>.
		///		Otherwise the transaction may be <see cref="Transaction.TransactionCompleted"/>'s subscriber and in that case it will be able to notify its listeners
		///		(<see cref="Subscribe(ITransactionNotification)"/>).
		/// </remarks>
		public bool IsSubscribedToMaster
		{ get; private set; }
		
		/// <summary>
		/// 	Unsubscribe from receiving notification when the transaction ends.
		/// </summary>
		/// <param name="subscriber">
		/// 	The subscriber not to be notified when the transaction ends.
		/// </param>
		/// <returns>
		/// 	<see langword="true"/> the subscriber was successfully unsubscribed
		/// 	<see langword="false"/> the subscriber was not subscribed
		/// </returns>
		/// <remarks>
		/// 	The subscription is OK to handle large number of subscribers.
		/// </remarks>
		public bool Unsubscribe(ITransactionNotification subscriber)
		{
			lock (_watchers)
			{
				return _watchers.Remove(subscriber);
			}
		}
		
		/// <summary>
		/// 	Subscribe to receive notification when the transaction ends.
		/// </summary>
		/// <param name="subscriber">
		/// 	The subscriber to be notified when the transaction ends.
		/// </param>
		/// <remarks>
		/// 	The subscription is OK to handle large number of subscribers.
		/// </remarks>
		public void Subscribe(ITransactionNotification subscriber)
		{
			Check.DoRequireArgumentNotNull(subscriber, "subscriber");
			Check.DoCheckOperationValid(IsActive, "Transaction is not active");

			EnsureSubscribedToMaster();

			lock (_watchers)
			{
				if (!_watchers.Contains(subscriber))
				{
					_watchers.Add(subscriber);
				}
			}
		}

		/// <summary>
		/// 	Commit transaction.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	- transaction is not active;
		/// 	- transaction is owned by a managed transaction
		/// </exception>
		/// <exception cref="TransactionNotificationException">
		///		Transaction notification failed. If failure occurred while sending preparation notifications the commit will not proceed (can try later).
		///		Otherwise, when exception is thrown from <see cref="ITransactionNotification.TransactionCompleted(IFileSystemTransaction, bool)"/>, the transaction is already
		///		committed and durable.
		///		<seealso cref="TransactionNotificationException.NotificationType"/>
		/// </exception>
		/// <exception cref="StorageTransactionException">
		///		Call to KTM API failed.
		/// </exception>
		public void Commit()
		{
			Check.DoCheckOperationValid(IsHandleValid, "Transaction is not active");
			Check.DoCheckOperationValid(!IsSlave, "Use owning managed transaction");

			try
			{
				NotifyPrepare();
			}
			catch (TransactionNotificationException e)
			{
				_log.ErrorFormat("Transaction Prepare notification failed, commit aborted: {0}", e);
				throw;
			}

			_log.Debug("Committing KTM transaction");
			try
			{
				_ktmHandle.Commit();
			}
			catch (Win32Exception e)
			{
				_log.ErrorFormat("KtmTransaction Commit failed: {0}", e);
				throw new StorageTransactionException(
					message: string.Format("Failed committing KTM transaction - {0}", e.Message)
					, technicalInfo: string.Empty
					, innerException: e);
			}
			_status = TransactionStatus.Committed;
			// exceptions ok to flow through if any
			NotifyCompletion(true);
		}
		
		/// <summary>
		/// 	Rollback transaction.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	Transaction is not active.
		/// </exception>
		/// <remarks>
		/// 	It is possible to rollback owned transaction, but using exception handling and managed owning transaction
		/// 	is preferrable.
		/// </remarks>
		/// <exception cref="StorageTransactionException">
		///		Call to KTM API failed.
		/// </exception>
		public void Rollback()
		{
			Check.DoCheckOperationValid(IsHandleValid, "Transaction is not active");

			try
			{
				_status = TransactionStatus.Aborted;
				try
				{
					_ktmHandle.Rollback();
				}
				catch (Win32Exception e)
				{
					_log.ErrorFormat("KtmTransaction Rollback failed: {0}", e);
					throw new StorageTransactionException(
						message: string.Format("Failed to roll back KTM transaction - {0}", e.Message)
						, technicalInfo: string.Empty
						, innerException: e);
				}
			}
			finally
			{
				// exceptions ok to flow through if any
				NotifyCompletion(false);
			}
		}
		
		/// <summary>
		/// 	Release KTM handle
		/// </summary>
		/// <remarks>
		/// 	If the instance is active local standalone KTM transaction (created with <see cref="BeginLocal()" />)
		/// 	it will be rolled back. Otherwise (if the transaction is participating in a managed transaction,
		/// 	there will be no immediate effect, <see cref="System.Transactions.Transaction.Current" /> )
		/// </remarks>
		public void Dispose()
		{
			if (!IsSlave && IsActive)
			{
				// notifying only if standalone and not yet finished; if already finished, watchers are already notified
				// if slave, notifications can only go through subscription to MasterTransaction.TransactionCompleted
				try
				{
					NotifyCompletion(false);
				}
				catch (StorageTransactionException e)
				{
					_log.ErrorFormat("Transaction notification when disposing failed: {0}", e);
				}
			}

			CloseHandle();

			Check.Ensure(!IsSubscribedToMaster && !IsActive && !IsHandleValid);
		}
		
		#endregion IFileSystemTransaction
			
		/// <summary>
		/// 	Create and begin new local KTM transaction (no DTC)
		/// </summary>
		/// <returns>
		/// 	New <see cref="KtmTransaction" /> instance representing newly started transaction.
		/// </returns>
		public static KtmTransaction BeginLocal()
		{
			_log.Debug("Starting local standalone KTM transaction");
			KtmTransaction retval = new KtmTransaction(KtmTransactionHandle.CreateLocalTransaction(), null);
			Check.DoEnsure(retval.IsHandleValid, "KtmTransaction failed to initialise");
			return retval;
		}

		/// <summary>
		/// 	Get KtmTransaction from managed transaction
		/// </summary>
		/// <param name="managedTransaction">
		/// 	Owning managed transaction.
		/// </param>
		/// <returns>
		/// 	<see cref="KtmTransaction" /> participating in <paramref name="managedTransaction" />
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// 	<paramref name="managedTransaction" /> is <see langword="null"/>
		/// </exception>
		public static KtmTransaction Get(Transaction managedTransaction)
		{
			_log.Debug("Getting KTM transaction from managed");
			Check.DoRequireArgumentNotNull(managedTransaction, "managedTransaction");
			return new KtmTransaction(KtmTransactionHandle.GetFromManaged(managedTransaction), managedTransaction);
		}
		
		/// <summary>
		/// 	Get KtmTransaction from ambient managed transaction (<see cref="Transaction.Current" />)
		/// </summary>
		/// <returns>
		/// 	<see cref="KtmTransaction" /> participating in <see cref="Transaction.Current" />
		/// </returns>
		/// <remarks>
		/// 	Use <see cref="TransactionScope" /> to manage ambient transaction.
		/// 	The returned transaction will be controlled by the ambient transaction.
		/// </remarks>
		public static KtmTransaction GetAmbient()
		{
			_log.Debug("GetAmbient");
			Check.DoRequire(Transaction.Current != null, "Ambient managed transaction not set");
			return Get(Transaction.Current);
		}
		
		/// <summary>
		/// 	Get kernel transaction handle
		/// </summary>
		public KtmTransactionHandle Hanlde
		{ get { return _ktmHandle; } }

		/// <summary>
		/// 	Notify any watchers; list of watchers is cleared after notification.
		/// </summary>
		/// <param name="committed">
		/// 	Whether the transaction was committed.
		/// </param>
		/// <remarks>
		/// 	The list of subscribers is cleared after notifications are sent. Therefore calling it twice will not result
		/// 	in duplicate notifications unless subscriptions are renewed.
		/// 	Any exceptions thrown from notification methods are logged and suppressed. They do
		/// 	not prevent the rest of the subscribers from receiving the notification.
		/// 	This method must be called after the transaction has completed.
		/// </remarks>
		private void NotifyCompletion(bool committed)
		{
			NotifyImpl((s) => NotifyCompletion(s, committed));
			_watchers.Clear();
		}

		/// <summary>
		///		
		/// </summary>
		/// <remarks>
		///		Exceptions are not suppressed.
		/// </remarks>
		private void NotifyPrepare()
		{
			NotifyImpl((s) => s.Prepare());
		}

		/// <summary>
		/// 	Notify any watchers; list of watchers is cleared after notification. Thread safe.
		/// </summary>
		/// <param name="action">
		/// 	Action to be performed with every subscriber.
		/// </param>
		/// <remarks>
		/// 	The list of subscribers is cleared after notifications are sent. Therefore calling it twice will not result
		/// 	in duplicate notifications unless subscriptions are renewed.
		/// 	Thread safety is achieved by locking the list of watchers.
		/// </remarks>
		private void NotifyImpl(Action<ITransactionNotification> action)
		{
			_log.DebugFormat("Commencing NotifyImpl with {0} subscribers", _watchers.Count);
			lock (_watchers)
			{
				foreach (ITransactionNotification notification in _watchers)
				{
					action(notification);
				}
			}
		}
		
		/// <summary>
		///		Shall not throw exception
		/// </summary>
		private void CloseHandle()
		{
			if (_ktmHandle != null)
			{
				_ktmHandle.Close();
			}
		}

		#region IEnlistmentNotification implementation

		// will rollback notification be called subsequently in that case?
		/// <summary>
		/// 
		/// </summary>
		/// <param name="enlistment"></param>
		/// <remarks>
		///		If this method throw exception it will fly through Transaction.Commit(), but Rollback() method will not be called
		///		on the enlisted volatile resource.
		/// </remarks>
		public void Commit(Enlistment enlistment)
		{
			_log.Debug("Master transaction signalled commit.");

			CloseHandle();
			_status = System.Transactions.TransactionStatus.Committed;
			try
			{
				NotifyCompletion(true);
			}
			catch (StorageTransactionException e)
			{
				_log.ErrorFormat("Commit notification failed: {0}", e);
			}
			finally
			{
				// if this is not done transaction will be left hanging and memory consumption will grow
				enlistment.Done();
			}
		}

		/// <summary>
		///		<see cref="IEnlistmentNotification.InDoubt(Enlistment)"/>
		/// </summary>
		/// <param name="enlistment">
		///		Enlistment
		/// </param>
		public void InDoubt(Enlistment enlistment)
		{
			// PROBLEM: investigate what happens next; should I consider it is a rollback? will Rollback() or Commit() be called afterwards?
			// according to msnd this state occurs when transaction is coordinated by remote MS DTC and connection to it is lost
			// ideally I should stop the work until transaction is forcibly committed/rolled back or connection restored, i.e. transaction
			// manager recovers; it seems not doing anything will implement it already - TransactionScope.Dispose() will probably be blocked
			// 
			_status = System.Transactions.TransactionStatus.InDoubt;
			enlistment.Done();
		}

		/// <summary>
		///		<see cref="IEnlistmentNotification.Prepare(PreparingEnlistment)"/>
		/// </summary>
		/// <param name="preparingEnlistment">
		///		PreparingEnlistment
		/// </param>
		public void Prepare(PreparingEnlistment preparingEnlistment)
		{
			bool prepared = false;
			try
			{
				// notify subscribers; if an exception is thrown preparation fails and transaction cannot be committed
				NotifyPrepare();
				prepared = true;
			}
			catch (StorageTransactionException e)
			{
				_log.ErrorFormat("Master initiated prepare failed: {0}", e);
			}
			finally
			{
				if (prepared)
				{
					// the sequence will be:
					//		this.Prepared begin
					//		this.Commit begin
					//		this.Commit end
					//		this.Prepared end
					preparingEnlistment.Prepared();
				}
				else
				{
					preparingEnlistment.ForceRollback();
					// experiment shows that Rollback will not be invoked (at least on volatile resource), so have to notify here
					NotifyCompletion(false);
				}
			}
		}

		/// <summary>
		///		<see cref="IEnlistmentNotification.Rollback(Enlistment)"/>
		/// </summary>
		/// <param name="enlistment">
		///		Enlistment
		/// </param>
		public void Rollback(Enlistment enlistment)
		{
			_log.Debug("KtmTransaction master rolled back");

			_status = System.Transactions.TransactionStatus.Aborted;
			try
			{
				NotifyCompletion(false);
			}
			catch (StorageTransactionException e)
			{
				_log.ErrorFormat("Master initiated rollback notification failed: {0}", e);
			}
			finally
			{
				CloseHandle();
				// if this is not done transaction will be left hanging and memory consumption will grow
				enlistment.Done();
			}
		}

		private void NotifyCompletion(ITransactionNotification subscriber, bool completed)
		{
			try
			{
				subscriber.TransactionCompleted(this, completed);
			}
			catch (TransactionNotificationException e)
			{
				_log.ErrorFormat("Prepare notification failed: {0}", e);
			}
		}

		#endregion IEnlistmentNotification implementation
	}
}
