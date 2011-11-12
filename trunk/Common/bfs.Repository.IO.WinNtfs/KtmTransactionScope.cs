/*
 * User: vasily
 * Date: 26/02/2011
 * Time: 7:49 AM
 * 
 */
using System;
using System.Collections.Generic;
using System.Transactions;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;

namespace bfs.Repository.IO.WinNtfs
{
	/// <remarks>
	/// 	Makes a file IO block on Windows Vista+ and NTFS transactional.
	/// 	Uses Kernel Transaction Manager.
	/// 	KTM transaction context is changed unless the transaction explicitly supplied for use is same as that which is already
	/// 	in the context. Context is restored when the scope is disposed and if it was changed when starting the scope.
	/// 	Transaction is disposed when disposing the scope unless the scope was explicitly advised not to dispose the transaction
	/// 	when being created. Note that transaction is disposed independently of the fact whether the context was changed by the scope.
	/// 	If transaction is to be disposed and it is not owned by a managed transaction (<see cref="KtmTransaction.IsSlave"/>)
	/// 	and the scope has been marked as completed it is committed before disposing. If it is not committed disposing results in
	/// 	rollback.
	/// </remarks>
	public class KtmTransactionScope : IStorageTransactionScope
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(KtmTransactionScope).Name);

		private KtmTransaction _previous;
		private KtmTransaction _current;
		private bool _completed;
		private bool _disposed = false;
		
		/// <summary>
		/// 	Wrap an existing KTM transaction.
		/// </summary>
		/// <param name="transaction">
		/// 	Existing KTM transaction; must be active or <see langword="null"/>.
		/// </param>
		/// <param name="dispose">
		/// 	Whether to dispose the <paramref name="transaction"/> when disposing scope; disposing will result in commit or rollback
		/// 	unless transaction is owned by a managed transaction <see cref="KtmTransaction.IsSlave"/>.
		/// </param>
		protected KtmTransactionScope(KtmTransaction transaction, bool dispose)
		{
			Initialize(transaction, dispose);
		}
		
		/// <summary>
		/// 	Create scope with transaction managed (owned) by the specified managed transaction.
		/// </summary>
		/// <param name="transaction">
		/// 	Managed transaction to join.
		/// </param>
		/// <param name="dispose">
		/// 	Whether to dispose the KTM transaction created by the scope when disposing the scope; disposing will not result in commit
		/// 	or rollback.
		/// </param>
		/// <returns>
		/// 	<see cref="KtmTransactionScope"/>
		/// </returns>
		/// 	The scope will create new <see cref="KtmTransaction"/> and install it as ambient, thread context transaction. At the end of the scope
		/// 	(when scope is disposed) the transaction will be disposed according to <paramref name="dispose"/> and the thread context will be restored.
		/// 	The created KTM transaction will not be committed or rolled back, <see cref="Complete()"/> will have no effect.
		/// 	When restoring context a disposed [previous] transaction (with invalid handle) will be replaced by <see langword="null"/>.
		public static KtmTransactionScope Create(Transaction transaction, bool dispose)
		{
			return Create(KtmTransaction.Get(transaction), dispose);
		}
		
		/// <summary>
		/// 	Create scope using the specified KTM transaction.
		/// </summary>
		/// <param name="dispose">
		/// 	Whether to dispose transaction created by the scope at the end of the scope.
		/// </param>
		/// <returns>
		/// 	<see cref="KtmTransactionScope"/>
		/// </returns>
		/// <remarks>
		/// 	The scope will create new <see cref="KtmTransaction"/> as part of the ambient managed transaction <see cref="Transaction.Current"/>
		/// 	and install it as ambient, thread context transaction. At the end of the scope
		/// 	(when scope is disposed) the transaction will be disposed according to <paramref name="dispose"/> and thread context will be
		/// 	restored. The created KTM transaction will not be committed or rolled back, <see cref="Complete()"/> will have no effect.
		/// 	When restoring context a disposed [previous] transaction (with invalid handle) will be replaced by <see langword="null"/>.
		/// </remarks>
		public static KtmTransactionScope CreateJoinAmbientManaged(bool dispose)
		{
			return Create(KtmTransaction.GetAmbient(), dispose);
		}
		
		/// <summary>
		/// 	Create scope preserving existing KTM context.
		/// </summary>
		/// <returns>
		/// 	<see cref="KtmTransactionScope"/>
		/// </returns>
		/// <remarks>
		/// 	Active KTM transaction must exist in the context (<see cref="KtmTransaction.Current"/>).
		/// 	The created scope will not
		/// 	change the context.
		/// </remarks>
		public static KtmTransactionScope CreateWrapAmbientKtm()
		{
			Check.DoCheckOperationValid(KtmTransaction.Current != null && KtmTransaction.Current.IsActive, "Ambient KTM transaction is not active");
			return Create(KtmTransaction.Current, false);
		}
		
		/// <summary>
		/// 	Create scope using the specified KTM transaction.
		/// </summary>
		/// <param name="transactionToUse">
		/// 	Transaction instance to install into thread context; <see langword="null"/> allowed.
		/// </param>
		/// <param name="dispose">
		/// 	Whether to dispose the <paramref name="transactionToUse"/> at the end of the scope.
		/// </param>
		/// <returns>
		/// 	<see cref="KtmTransactionScope"/>
		/// </returns>
		/// <remarks>
		/// 	The scope will install <paramref name="transactionToUse"/> as ambient, thread context transaction. At the end of the scope
		/// 	(when scope is disposed) the transaction will be disposed according to <paramref name="dispose"/> and thread context will be
		/// 	restored unless the <paramref name="transactionToUse"/> is already ambient at the start of the scope.
		/// 	Before disposing the transaction it will be committed if the <paramref name="transactionToUse"/> is not owned
		/// 	by a managed transaction (<see cref="IFileSystemTransaction.IsSlave"/>) and the scope has been marked as completed
		/// 	(<see cref="Complete()"/>) and the <paramref name="transactionToUse"/> is not null.
		/// 	When restoring context a disposed [previous] transaction (with invalid handle) will be replaced by <see langword="null"/>.
		/// </remarks>
		public static KtmTransactionScope Create(KtmTransaction transactionToUse, bool dispose)
		{
			return new KtmTransactionScope(transactionToUse, dispose);
		}

		/// <summary>
		/// 	Create standalone KTM transaction scope not joining ambient managed transaction (if present).
		/// </summary>
		/// <param name="dispose">
		/// 	Whether to dispose transaction created by the scope at the end of the scope.
		/// </param>
		/// <returns>
		/// 	<see cref="KtmTransactionScope"/>
		/// </returns>
		/// <remarks>
		/// 	The scope will create new <see cref="KtmTransaction"/> and install it as ambient, thread context transaction. At the end of the scope
		/// 	(when scope is disposed) the transaction will be disposed according to <paramref name="dispose"/> and the thread context will be restored.
		/// 	Before disposing the transaction it will be committed if the scope has been marked as completed (<see cref="Complete()"/>).
		/// 	When restoring context a disposed [previous] transaction (with invalid handle) will be replaced by <see langword="null"/>.
		/// </remarks>
		public static KtmTransactionScope CreateLocal(bool dispose)
		{
			return new KtmTransactionScope(KtmTransaction.BeginLocal(), dispose);
		}
		
		/// <summary>
		/// 	Create standalone KTM transaction scope not joining ambient managed transaction (if present); dispose created transaction
		/// 	at the end.
		/// </summary>
		/// <returns>
		/// 	<see cref="KtmTransactionScope"/>
		/// </returns>
		/// <remarks>
		/// 	The scope will create new <see cref="KtmTransaction"/> and install it as ambient, thread context transaction. At the end of the scope
		/// 	(when scope is disposed) the transaction will be disposed and the thread context will be restored.
		/// 	When restoring context a disposed [previous] transaction (with invalid handle) will be replaced by <see langword="null"/>.
		/// </remarks>
		public static KtmTransactionScope CreateLocal()
		{
			return CreateLocal(true);
		}
		
		/// <summary>
		/// 	
		/// </summary>
		/// <param name="transaction">
		/// 	Transaction to install into the context. If it is equal to the current context KTM transaction the scope will not
		/// 	change or restore context at all.
		/// </param>
		/// <param name="dispose">
		/// 	Whether to dispose the <paramref name="transaction"/> when disposing scope; disposing will result in commit or rollback
		/// 	unless transaction is owned by a managed transaction <see cref="KtmTransaction.IsSlave"/>.
		/// </param>
		protected void Initialize(KtmTransaction transaction, bool dispose)
		{
			Check.DoCheckArgument(transaction == null || transaction.IsActive, "Transaction must be active");
			
			_completed = false;
			_previous = KtmTransaction.Current;
			_current = transaction;
			ToBeDisposed = transaction != null && dispose;
			HasChangedContext = transaction != KtmTransaction.Current;
			if (HasChangedContext)
			{
				KtmTransaction.Current = transaction;
			}
		}
		
		/// <summary>
		/// 	Get underlying KTM transaction.
		/// </summary>
		protected KtmTransaction KtmTransaction
		{ get { return _current; } }

		/// <summary>
		/// 	Whether the underlying transaction is going to be disposed when the scope is being disposed. Note that scope can be disposed
		/// 	without ending transaction even if it it is not transaction owner (<see cref="IsTransactionOwner"/>).
		/// </summary>
		/// <remarks>
		///		Usage example: need scope joining managed ambient transaction and do not want to reuse the storage (e.g. KTM) transaction
		///		instance. The KTM transaction instance will be installed into the context and removed/disposed at the end of the scope, but
		///		the actual transaction will continue beyond the end of the scope.
		/// </remarks>
		public bool ToBeDisposed
		{ get; private set; }

		/// <summary>
		/// 	Whether the context (ambient KTM transaction) has been changed by the scope.
		/// </summary>
		public bool HasChangedContext
		{ get; private set; }

		/// <summary>
		/// 	Whether the scope owns transaction, i.e. it will end it when disposed.
		/// </summary>
		/// <remarks>
		/// 	If the scope owns the transaction it will be disposed (<see cref="ToBeDisposed"/> will be <see langword="true"/>) and committed
		/// 	or rolled back when the scope is disposed.
		/// </remarks>
		public bool IsTransactionOwner
		{ get { return ToBeDisposed && !KtmTransaction.IsSlave; } }
		
		/// <summary>
		/// 	Get underlying file system transaction.
		/// </summary>
		public IFileSystemTransaction UnderlyingTransaction
		{ get { return this.KtmTransaction; } }

		/// <summary>
		/// 	Get file system transaction which was active before the scope started.
		/// </summary>
		public IFileSystemTransaction PreviousTransaction
		{ get { return _previous; } }
		
		/// <summary>
		/// 	Detach the underlying transaction from the context. This will ensure that the transaction will not be disposed
		/// 	but context handling will not change.
		/// </summary>
		/// <returns>
		/// 	<see cref="UnderlyingTransaction"/>
		/// </returns>
		/// <remarks>
		/// 	After detaching <see cref="UnderlyingTransaction"/> will return <see langword="null"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		<see cref="UnderlyingTransaction"/> is <see langword="null"/>
		/// </exception>
		public IFileSystemTransaction DetachTransaction()
		{
			Check.DoCheckOperationValid(_current != null, "No transaction to detach");
			ToBeDisposed = false;
			IFileSystemTransaction retval = _current;
			_current = null;
			return retval;
		}
		
		/// <summary>
		/// 	Mark scope for completion.
		/// </summary>
		/// <remarks>
		/// 	If the KTM transaction is standalone it will be committed at the end of the scope -
		/// 	when disposing transaction scope.
		/// </remarks>
		public void Complete()
		{
			//no need to complicate code of StorageTransactionScope; no shame if this is marked completed
			//Check.DoCheckOperationValid(!_current.IsOwned, "Use owning managed transaction");
			_completed = true;
		}
		
		/// <summary>
		/// 	Dispose the scope.
		/// </summary>
		/// <remarks>
		/// 	Will dispose transaction unless asked not to do so when being created. During disposal will commit transation
		/// 	if it is not owned by managed transaction and marked completed.
		/// 	Will restore context if it was changed by the scope, regardless of transaction disposing scenario.
		/// 	When restoring context a disposed [previous] transaction (with invalid handle) will be replaced by null.
		/// </remarks>
		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				if (ToBeDisposed)
				{
					if (!_current.IsSlave)
					{
						if (_completed)
						{
							_current.Commit();
						}
						// rollback does not seem necessary - will be done automatically when handle is closed
					}
					_current.Dispose();
				}

				if (HasChangedContext)
				{
					if (_previous != null && !_previous.IsHandleValid)
					{
						_previous = null;
					}
					KtmTransaction.Current = _previous;
					HasChangedContext = false;
				}
			}
		}
	}
}
