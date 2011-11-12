/*
 * User: vasily
 * Date: 26/02/2011
 * Time: 7:15 PM
 * 
 */
using System;
using System.Transactions;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;
using bfs.Repository.IO.WinNtfs;

namespace bfs.Repository.IO.WinNtfs
{
	/// <summary>
	/// 	Provider for windows NT and NTFS.
	/// </summary>
	public class WinLongFileSystemProvider : IFileSystemProvider
	{
		private static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(WinLongFileSystemProvider).Name);

		public WinLongFileSystemProvider()
		{
			FileProvider = new WinLongFileFrovider();
			DirectoryProvider = new WinLongDirectoryProvider();
			SupportsTransactions = Util.SystemInfo.IsAnyWindows && Environment.OSVersion.Version.Major > 5;
		}
		
		/// <summary>
		/// 	Get file IO provider.
		/// </summary>
		public IFileProvider FileProvider
		{ get; private set; }
		
		/// <summary>
		/// 	Get directory IO provider
		/// </summary>
		public IDirectoryProvider DirectoryProvider
		{ get; private set; }
		
		/// <summary>
		/// 	Whether the provider supports storage (i.e. file system) transactions.
		/// </summary>
		public bool SupportsTransactions
		{ get; private set; }
		
		/// <summary>
		/// 	Whether ambient storage transaction is active.
		/// </summary>
		public bool IsStorageAmbientTransactionActive
		{
			get
			{
				CheckTransactionsSupported();
				return KtmTransaction.Current != null && KtmTransaction.Current.IsHandleValid; // .IsInvalid && !KtmTransactionHandle.Current.IsClosed;
			}
		}
		
		/// <summary>
		/// 	Dispose active ambient storage transaction.
		/// </summary>
		/// <exception cref="NotSupportedException">
		/// 	Thansactions are not supported by this provider (<see cref="SupportsTransactions"/>).
		/// </exception>
		/// <remarks>
		/// 	If current ambient storage transaction is part of a managed transaction, disposing
		/// 	the storage transaction will not roll it back. Use this method with care; normally,
		/// 	transaction scope should handle these matters.
		/// </remarks>
		public void DisposeAmbientStorageTransaction()
		{
			CheckTransactionsSupported();
			KtmTransaction.Current.Dispose();
		}
		
		/// <summary>
		///		Creates new scope, starts new independent storage transaction regardless of current storage and managed context.
		/// </summary>
		/// <param name="dispose">
		///		Dispose underlying storage transaction at the end of the scope.
		/// </param>
		/// <returns>
		///		New scope.
		/// </returns>
		/// <remarks>
		/// </remarks>
		public IStorageTransactionScope CreateStandaloneTransactionScope(bool dispose)
		{
			return KtmTransactionScope.CreateLocal(dispose);
		}

		/// <summary>
		///		Create transaction scope for storage IO.
		/// </summary>
		/// <param name="dispose">
		///		Dispose underlying storage transaction at the end of the scope.
		/// </param>
		/// <returns>
		///		New scope.
		/// </returns>
		/// <remarks>
		///		Creates new storage transaction participating in the ambient managed transaction (<see cref="System.Transactions.Transaction.Current"/>)
		///		regardless of current storage and managed context.
		/// </remarks>
		public IStorageTransactionScope CreateSlaveTransactionScope(bool dispose)
		{
			return KtmTransactionScope.CreateJoinAmbientManaged(dispose);
		}

		/// <summary>
		///		Create transaction scope for storage IO.
		/// </summary>
		/// <param name="masterTransaction">
		///		Transaction to be made master of the underlying storage transaction.
		/// </param>
		/// <param name="dispose">
		///		Dispose underlying storage transaction at the end of the scope.
		/// </param>
		/// <returns>
		///		New scope.
		/// </returns>
		/// <remarks>
		///		Creates new storage transaction participating in the specified managed <paramref name="masterTransaction"/>
		///		regardless of current storage and managed context. The underlying storage transaction will be subscribed to master transaction's completion event
		///		if <paramref name="dispose"/> is <see langword="false"/>.
		/// </remarks>
		public IStorageTransactionScope CreateSlaveTransactionScope(Transaction masterTransaction, bool dispose)
		{
			return KtmTransactionScope.Create(masterTransaction, dispose);
		}

		/// <summary>
		///		Create transaction scope wrapping existing storage transaction.
		/// </summary>
		/// <param name="transactionToUse">
		///		Transaction to wrap; may be null.
		/// </param>
		/// <param name="dispose">
		///		Dispose underlying storage transaction at the end of the scope.
		/// </param>
		/// <returns>
		///		New scope.
		/// </returns>
		public IStorageTransactionScope CreateTransactionScope(IFileSystemTransaction transactionToUse, bool dispose)
		{
			CheckTransactionsSupported();
			Check.DoAssertLambda(transactionToUse == null || transactionToUse is KtmTransaction, () => new ArgumentException("Unknown transaction type"));
			Check.DoAssertLambda(transactionToUse == null || transactionToUse.IsActive, () => new ArgumentException("The transaction is not active"));
			_log.Debug("Creating storage transaction scope from provided transaction");

			return KtmTransactionScope.Create((KtmTransaction)transactionToUse, dispose);
		}
		
		/// <summary>
		/// 	Subscribe for notification about the completion of the ambient Storage transaction.
		/// </summary>
		/// <param name="subscriber">
		/// 	The subscriber to notify.
		/// </param>
		public void SubscribeForAmbientTransactionCompletion(ITransactionNotification subscriber)
		{
			KtmTransaction.Current.Subscribe(subscriber);
		}
		
		/// <summary>
		/// 	Get ambient thread-local (context) transaction. Returns <see langword="null"/> if none.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public IFileSystemTransaction AmbientTransaction
		{ get { return KtmTransaction.Current; } }
		
		private void CheckTransactionsSupported()
		{
			Check.DoAssertLambda(SupportsTransactions, () => new NotSupportedException("Windows Vista or later required."));
		}
	}
}
