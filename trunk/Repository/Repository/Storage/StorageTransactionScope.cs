/*
 * User: vasily
 * Date: 28/02/2011
 * Time: 9:43 PM
 * 
 */
using System;
using System.Collections.Generic;
using System.Threading;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;
using System.Transactions;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Implements transaction management inside repository. Not thread safe. Not for use outside repository.
	/// </summary>
	/// <remarks>
	///		The class must be internal, <see cref="StorageTransactionScope.IsWithinStorageTransactionScope"/> for the reason.
	/// </remarks>
	internal class StorageTransactionScope : IStorageTransactionScope
	{

		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(StorageTransactionScope).Name);

		// this was originally used to ensure only 1 transaction is created in the repository if repository is configured to always start its own transaction
		// which may make sense even if storage transaction then has to be slave of the managed ambient transaction - the client may use storage transaction's
		// context for its own purposes outside repository and wants it to be isolated from the repository
		private static ThreadLocal<int> _scopeNestLevel = new ThreadLocal<int>();
		// registering top level scopes so that we know whether the transaction is owned by 
		private static ThreadLocal<StorageTransactionScope> _topLevelScopes = new ThreadLocal<StorageTransactionScope>();
		
		// Wrapped scope will never be null.
		// Having wrapped scope does not necessarily mean we have new transaction created.
		private IStorageTransactionScope _wrappedScope;
		private bool _disposed = false;
		

		/// <summary>
		/// 	Create new instance and start ambient KTM transaction if necessary
		/// </summary>
		/// <param name="repository">
		/// 	Repository manager
		/// </param>
		/// <param name="disposeIfSlave">
		/// 	Dispose transaction created in scope when disposing the scope even if it is part of the ambient managed transaction.
		/// </param>
		/// <remarks>
		/// 	The created scope will be configured to dispose underlying transaction if it is not NULL scope, <see cref="IsNullScope"/>.
		/// </remarks>
		protected StorageTransactionScope(IRepository repository, bool disposeIfSlave)
			: this(
				repository : repository
				, disposeIfSlave : disposeIfSlave
				, okToStartOwnTransaction: true
			)
		{
		}
		
		/// <summary>
		/// 	Create new instance and start ambient KTM transaction if necessary
		/// </summary>
		/// <param name="repository">
		/// 	Repository manager
		/// </param>
		/// <param name="disposeIfSlave">
		/// 	Dispose transaction created in scope when disposing the scope even if it is part of the ambient managed transaction.
		/// 	Has no effect if <paramref name="joinAmbientManaged"/> is <see langword="false"/>.
		/// </param>
		/// <param name="okToStartOwnTransaction">
		///		Whether to allow staring own storage transaction (slave transaction is not owned).
		/// </param>
		/// <remarks>
		///		DOCO: transaction settings
		///		When wrapping existing transaction a priority is always given to storage transaction scope.
		///		Settings allow prohibiting the use of any external transaction (AlwaysStartNew) or only managed (DisallowJoiningAmbientManaged).
		///		You cannot prohibit usage of external storage transactions but allow that of external managed transactions.
		///		Hence if AlwaysStartNew is OFF and there's an ambient storage transaction when the scope is created it will wrap around that
		///		existing storage transaction.
		/// </remarks>
		protected StorageTransactionScope(IRepository repository, bool disposeIfSlave, bool okToStartOwnTransaction)
		{
			IStorageTransactionScope scopeToWrap;
			IFileSystemProvider fsProvider = repository.ObjectFactory.FileSystemProvider;
			StorageTransactionSettings settings = repository.Settings.StorageTransactionSettings;

			bool noTransactionOption = (settings & StorageTransactionSettings.NoTransactions) == StorageTransactionSettings.NoTransactions;
			bool joinAmbientManaged = (settings & StorageTransactionSettings.DisallowJoiningAmbientManaged) != StorageTransactionSettings.DisallowJoiningAmbientManaged;

			if (!noTransactionOption)
			{

				if ((settings & StorageTransactionSettings.RequireTransactions) == StorageTransactionSettings.RequireTransactions
					&& !fsProvider.SupportsTransactions)
				{
					// transactions required but not supported
					throw new InvalidOperationException(StorageResources.TransactionsNotSupported);
				}

				// done: start new must only apply to top level transaction started in repository because if subsequent
				// nested methods create their scopes they would start their own transactions and would not see parent FS objects
				bool alwaysStartNewOption = IsAlwaysNewTransactionOptionOn(repository: repository);

				// note: "start new" should not create multiple transactions in nested calls INSIDE repository; only one at the top level
				// is all I need.
				bool startNewIfAlreadyActive = alwaysStartNewOption && !IsWithinStorageTransactionScope;

				// startNewIfAlreadyActive == true && joinAmbientManaged == true makes sense only when storage (KTM) transaction is already
				// active AND managed external transaction is also active but the KTM transaction is not part of the current Transaction.Current
				// because if it is then the result will be exactly the same as when just using already active KTM transaction

				if (!startNewIfAlreadyActive && fsProvider.IsStorageAmbientTransactionActive)
				{
					// case for reusage of already existing ambient storage transaction; if there's ambient storage transaction and OK to reuse it
					// (by not being required to start new one) then okToStartOwnTransaction is not relevant as I am not going to start own transaction
					// anyway;
					// "always start new" should prevent usage of external storage transactions; therefore when it is ON and not ok to start own tr-n
					// I have to create null scope
					_log.Debug("Ambient transaction active, returning preservation scope");
					scopeToWrap = fsProvider.CreateTransactionScope(transactionToUse: fsProvider.AmbientTransaction, dispose: false);
					Check.Assert(!scopeToWrap.ToBeDisposed && !scopeToWrap.HasChangedContext && fsProvider.AmbientTransaction == scopeToWrap.UnderlyingTransaction);
				}
				else
				{
					if (joinAmbientManaged && IsManagedAmbientActive)
					{
						// slave transaction is not regarded as "own"; the scope does not control it.
						scopeToWrap = fsProvider.CreateSlaveTransactionScope(dispose: disposeIfSlave);
					}
					else
					{
						if (okToStartOwnTransaction)
						{
							// default
							// Creates new scope, starts new independent storage transaction regardless of current storage and managed context.
							scopeToWrap = fsProvider.CreateStandaloneTransactionScope(dispose: true);
						}
						else
						{
							// NULL scope needed - the scope which will not create any transaction and will clear storage transaction context
							// instead; this may happen when when there's storage ambient transaction but "always start new" option is ON.
							scopeToWrap = fsProvider.CreateTransactionScope(transactionToUse: null, dispose: false);
							Check.Assert(scopeToWrap.UnderlyingTransaction == null);
							Check.Assert(!fsProvider.IsStorageAmbientTransactionActive);
						}
					}
				}
			}
			else
			{
				// creating scope which will clear transactional context
				scopeToWrap = fsProvider.CreateTransactionScope(transactionToUse: null, dispose: false);
			}

			Check.Assert(scopeToWrap != null);
			Initialize(scopeToWrap, repository);
		}

		private static bool IsManagedAmbientActive
		{ get { return null != Transaction.Current; } }

		
		/// <summary>
		/// 	Create new instance wrapping the specified transaction instance.
		/// </summary>
		/// <param name="transactionToUse">
		/// 	Transaction to wrap in the scope. If null, the scope will clear transaction context.
		/// </param>
		/// <param name="dispose">
		/// 	Whether to dispose the <paramref name="transactionToUse"/> when the scope is disposed.
		/// </param>
		protected StorageTransactionScope(IRepository repository, IFileSystemTransaction transactionToUse, bool dispose)
		{
			Initialize(
				repository.ObjectFactory.FileSystemProvider.CreateTransactionScope(transactionToUse, dispose)
				, repository);
		}

		/// <summary>
		///		Create scope by wrapping specified scope
		/// </summary>
		/// <param name="repository">
		///		Repository
		/// </param>
		/// <param name="scopeToWrap">
		///		Accepts <see langword="null"/> to create NULL scope, <see cref="IsNullScope"/>
		/// </param>
		protected StorageTransactionScope(IRepository repository, IStorageTransactionScope scopeToWrap)
		{
			Initialize(scopeToWrap, repository);
		}
		
		/// <summary>
		/// 	Factory method for scenario when you already have active transaction which you want to reuse and transaction to live outside
		/// 	the code block marked by the scope.
		/// </summary>
		/// <param name="repository">
		/// 	The repository.
		/// </param>
		/// <param name="pendingTransaction">
		/// 	Transaction to [re]use or null.
		/// </param>
		/// <returns>
		/// 	<see cref="StorageTransactionScope"/>
		/// </returns>
		/// <remarks>
		/// 	This is a factory method wrapping long living transaction. At the end of the scope transaction will not be
		/// 	committed or rolled back or disposed. The context will be changed only if the <paramref name="pendingTransaction"/>
		/// 	is not already ambient. At the end of the scope the context will be restored if it had been changed at the start.
		/// </remarks>
		public static StorageTransactionScope CreateWrapPending(IRepository repository, IFileSystemTransaction pendingTransaction)
		{
			CheckContextForPendingTransaction(repository: repository, pendingTransaction: pendingTransaction);
			return new StorageTransactionScope(repository, pendingTransaction, false);
		}
		
		/// <summary>
		/// 	Create scope to look through it and use already existing storage transaction context.
		/// </summary>
		/// <param name="repository"></param>
		/// <returns></returns>
		/// <remarks>
		/// 	The scope will not commit, rollback, dispose ambient storage transaction or change the context.
		/// </remarks>
		public static StorageTransactionScope CreateLookThroughScope(IRepository repository)
		{
			Check.DoRequireArgumentNotNull(repository, "repository");
			Check.DoCheckOperationValid(
				repository.ObjectFactory.FileSystemProvider.IsStorageAmbientTransactionActive
				, "Ambient storage transaction must be active");
			return CreateWrapPending(repository, repository.ObjectFactory.FileSystemProvider.AmbientTransaction);
		}
		
		/// <summary>
		/// 	Default factory method
		/// </summary>
		/// <param name="repository">
		/// 	The repository manager.
		/// </param>
		/// <param name="pendingTransaction">
		/// 	Pending transaction to reuse if not <see langword="null"/>.
		/// </param>
		/// <returns>
		/// </returns>
		/// <remarks>
		/// 	The most common factory method creates scope catering for the current environment. If pending transaction is provided
		/// 	the method acts like <see cref="CreateWrapPending(IRepositoryManager, IFileSystemTransaction)"/>. Otherwise it will act
		/// 	as <see cref="StorageTransactionScope(IRepositoryManager, bool)"/>, using repository settings
		/// 	(<see cref="repository.Settings.StorageTransactionSettings"/>) to decide if and what kind of transaction to create.
		/// 	When wrapping existing pending transaction or when creating slave transaction the scope will be configured not to dispose
		/// 	transaction at the end of the scope.
		/// </remarks>
		public static StorageTransactionScope Create(IRepository repository, IFileSystemTransaction pendingTransaction)
		{
			if (pendingTransaction == null)
			{
				// see remarks
				return new StorageTransactionScope(repository, false);
			}
			else
			{
				return CreateWrapPending(repository, pendingTransaction);
			}
		}

		private static IFileSystemTransaction GetAmbientTransaction(IRepository repository)
		{
			return repository.ObjectFactory.FileSystemProvider.AmbientTransaction;
		}

		private static bool IsAlwaysNewTransactionOptionOn(IRepository repository)
		{
			return (repository.Settings.StorageTransactionSettings & StorageTransactionSettings.AlwaysStartNew) == StorageTransactionSettings.AlwaysStartNew;
		}

		/// <summary>
		///		Create scope which will not create its own transaction, but would only pick up existing ambient transaction.
		/// </summary>
		/// <param name="repository">
		///		Repository
		/// </param>
		/// <param name="pendingTransaction">
		/// 	Pending transaction to reuse if not <see langword="null"/>.
		/// </param>
		/// <returns>
		///		New transaction scope not owning a transaction.
		/// </returns>
		/// <remarks>
		///		
		/// </remarks>
		/// <exception cref="InvalidOperationException">	
		///		Current transactional context is inconsistent with the <paramref name="pendingTransaction"/>.
		/// </exception>
		/// <remarks>
		///		When <paramref name="pendingTransaction"/> is not null the ambient transaction must be:
		///			- null
		///			- equal to <paramref name="pendingTransaction"/>
		///			- belong to the same master transaction
		///		Otherwise the "always start new transaction" option must be ON (<see cref="IRepositoryManager.Settings.StorageTransactionSettings"/>).
		///		If top level scope is created lazy and it is not NULL scope then there is external transaction being used in repository.
		/// </remarks>
		public static StorageTransactionScope CreateLazy(IRepository repository, IFileSystemTransaction pendingTransaction)
		{
			StorageTransactionScope retval;
			if (pendingTransaction == null)
			{
				retval = new StorageTransactionScope(repository: repository, disposeIfSlave: false, okToStartOwnTransaction: false);
				// proxy may have to be a new transaction instance, but slave of an existing managed transaction
				Check.Ensure(!retval.IsTransactionOwner, "Lazy scope must not create and own a transaction, it can only be a proxy to an existing transaction.");
			}
			else
			{
				CheckContextForPendingTransaction(repository: repository, pendingTransaction: pendingTransaction);
				retval = Create(repository, pendingTransaction);
				Check.Ensure(pendingTransaction == retval.UnderlyingTransaction);
			}
			Check.Ensure(!retval.ToBeDisposed, "Lazy scope must not own or dispose underlying transaction");
			return retval;
		}


		/// <summary>
		/// 	Create transaction scope.
		/// </summary>
		/// <param name="repository">
		/// 	The repository manager.
		/// </param>
		/// <remarks>
		/// 	The created scope will be configured to dispose underlying transaction if it is not NULL scope, <see cref="IsNullScope"/>.
		/// 	Repository settings (<see cref="repository.Settings.StorageTransactionSettings"/> will be used to decide if and
		/// 	what kind of transaction to create. Same as <code>Create(repository, null)</code>.
		/// </remarks>
		public static StorageTransactionScope Create(IRepository repository)
		{
			return Create(repository, null);
		}

		private static void CheckContextForPendingTransaction(IRepository repository, IFileSystemTransaction pendingTransaction)
		{
			Check.DoCheckOperationValid(
				GetAmbientTransaction(repository) == null
				|| GetAmbientTransaction(repository) == pendingTransaction
				|| IsAlwaysNewTransactionOptionOn(repository)
				|| (
					pendingTransaction.MasterTransaction != null
					&& GetAmbientTransaction(repository).MasterTransaction == pendingTransaction.MasterTransaction
				)
				, StorageResources.ContextInconsistentWithPendingTransaction);
		}
		
		/// <summary>
		/// 	Initialize
		/// </summary>
		/// <param name="scopeToWrap">
		/// 	Accepts null
		/// </param>
		/// <param name="repository">
		/// 	Must not be null.
		/// </param>
		private void Initialize(IStorageTransactionScope scopeToWrap, IRepository repository)
		{
			_wrappedScope = scopeToWrap;
			Repository = repository;
			
			NoTransaction = !repository.ObjectFactory.FileSystemProvider.IsStorageAmbientTransactionActive;
			// must be last statement executed in a constructor
			if (IncrementNestLevel() == 1)
			{
				Check.Require(TopLevelScope == null, "TopLevelScope out of sync with nest level");
				TopLevelScope = this;
			}

			Check.Ensure((NoTransaction && IsNullScope) || (!NoTransaction && !IsNullScope));
		}
		
		/// <summary>
		/// 	Null scope wraps scope which clears transactional context.
		/// </summary>
		/// <remarks>
		///		DOCO:
		/// 	The scope is created when repository is configured not to use transactions (<see cref="repository.Settings.StorageTransactionSettings"/>)
		/// 	or "always start new" is ON and the scope is lazy, i.e. when scope needs to pick up existing external transactions only but not start
		/// 	its own (this is required when a method such as <see cref="IRepositoryWriter.Write(IDataItem)"/> does not represent a complete unit of
		/// 	work by itself as we do not want to flush every single data item to disk immediately)
		/// </remarks>
		public bool IsNullScope
		{ get { return _wrappedScope.UnderlyingTransaction == null; } }
		
		public IRepositoryManager Repository
		{ get; private set; }
		
		public void Complete()
		{
			_wrappedScope.Complete();
		}
		
		/// <summary>
		/// 	Whether ambient Storage transaction was active after initializing this scope
		/// 	(either as a result of creating this scope or created outside the scope).
		/// </summary>
		/// <remarks>
		/// 	After scope is created this is same as <see cref="IsNullScope"/>, but unlike that one, this property does not change
		/// 	after transaction is detached from scope.
		/// </remarks>
		public bool NoTransaction
		{ get; private set; }

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
		{ get { return _wrappedScope.ToBeDisposed; } }
		
		public bool HasChangedContext
		{ get { return _wrappedScope.HasChangedContext; } }

		/// <summary>
		/// 	Whether the scope owns transaction, i.e. it will end it when disposed.
		/// </summary>
		/// <remarks>
		/// 	If the scope owns the transaction it will be disposed (<see cref="ToBeDisposed"/> will be <see langword="true"/>) and committed
		/// 	or rolled back when the scope is disposed.
		/// </remarks>
		public bool IsTransactionOwner
		{ get { return _wrappedScope.IsTransactionOwner; } }
		
		public IFileSystemTransaction UnderlyingTransaction
		{ get { return _wrappedScope.UnderlyingTransaction; } }

		/// <summary>
		/// 	Get file system transaction which was active before the scope started.
		/// </summary>
		public IFileSystemTransaction PreviousTransaction
		{ get { return _wrappedScope.PreviousTransaction; } }
		
		public IFileSystemTransaction DetachTransaction()
		{
			Check.DoCheckOperationValid(!IsNullScope, "Transaction scope is NULL cope - it does not wrap any transaction.");
			return _wrappedScope.DetachTransaction();
		}
		
		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_wrappedScope.Dispose();
				// must be last 
				if (DecrementNestLevel() == 0)
				{
					// top level scope ended
					Check.Ensure(TopLevelScope == this);
					TopLevelScope = null;
				}
			}
		}
		
		/// <summary>
		/// 	This must be last command in the constructor in order for the <see cref="TransactionScopeCreatedInRepository"/>
		/// 	and <see cref="ScopeNestLevel"/> to return correct result in the constructor.
		/// </summary>
		/// <returns>
		///		Updated nest level
		/// </returns>
		private static int IncrementNestLevel()
		{
			if (_scopeNestLevel.IsValueCreated)
			{
				++_scopeNestLevel.Value;
			}
			else
			{
				_scopeNestLevel.Value = 1;
			}
			return _scopeNestLevel.Value;
		}
		
		/// <summary>
		/// 	This must be last command in the constructor in order for the <see cref="ScopeNestLevel"/> to return correct current
		/// 	result in the Dispose() method.
		/// </summary>
		/// <returns>
		///		Updated nest level
		/// </returns>
		private static int DecrementNestLevel()
		{
			Check.DoCheckOperationValid(_scopeNestLevel.IsValueCreated && _scopeNestLevel.Value > 0, "Nest level underflow");
			return --_scopeNestLevel.Value;
		}
		
		/// <summary>
		///		Get StorageTransactionScope nest level.
		/// </summary>
		/// <remarks>
		///		<code>
		///			Assert.AreEqual(0, StorageTransactionScope.ScopeNestLevel);
		///			
		///			using (var scopeLevel1 = StorageTransactionScope.Create(repository))
		///			{
		///				Assert.AreEqual(1, StorageTransactionScope.ScopeNestLevel);
		///				using (var scopeLevel2 = StorageTransactionScope.Create(repository))
		///				{
		///					Assert.AreEqual(2, StorageTransactionScope.ScopeNestLevel);
		///					// ... and so on
		///				}
		///			}
		///		</code>
		/// </remarks>
		public static int ScopeNestLevel
		{
			get
			{
				if (_scopeNestLevel.IsValueCreated)
				{
					return _scopeNestLevel.Value;
				}
				return 0;
			}
		}

		public static StorageTransactionScope TopLevelScope
		{
			get
			{
				return _topLevelScopes.Value;
			}
			private set
			{
				_topLevelScopes.Value = value;
			}
		}

		/// <summary>
		///		Get whether top level scope (which must have been created in repository) owns context storage transaction.
		/// </summary>
		/// <remarks>
		///		If this property is <see langword="true"/> then IO will finish before call exits top level method in repository.
		///		If it is <see langword="false"/> <b>and</b> there is <b>no</b> context storage transaction IO will complete immediately.
		///		This should help dirty state maintenance for writers etc.
		/// </remarks>
		public static bool IsTopLevelScopeTransactionOwner
		{
			get
			{
				return TopLevelScope != null && TopLevelScope.IsTransactionOwner;
			}
		}
		
		/// <summary>
		///		If true then the code is wrapped in scope created somewhere in the repository (because the class is internal).
		/// </summary>
		private static bool IsWithinStorageTransactionScope
		{ get { return ScopeNestLevel > 0; } }
	}
}
