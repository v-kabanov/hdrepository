/*
 * User: vasily
 * Date: 25/02/2011
 * Time: 7:59 PM
 * 
 */
using System;
using bfs.Repository.Interfaces.Infrastructure;
using System.Transactions;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	/// 	Provides access to filesystem IO.
	/// </summary>
	/// <remarks>
	/// 	
	/// </remarks>
	public interface IFileSystemProvider
	{
		/// <summary>
		/// 	Get file IO provider.
		/// </summary>
		IFileProvider FileProvider
		{ get; }
		
		/// <summary>
		/// 	Get directory IO provider
		/// </summary>
		IDirectoryProvider DirectoryProvider
		{ get; }
		
		/// <summary>
		/// 	Whether the provider supports storage (i.e. file system) transactions.
		/// </summary>
		bool SupportsTransactions
		{ get; }
		
		/// <summary>
		/// 	Whether ambient storage transaction is active.
		/// </summary>
		/// <exception cref="NotSupportedException">
		/// 	Thansactions are not supported by this provider (<see cref="SupportsTransactions"/>).
		/// </exception>
		bool IsStorageAmbientTransactionActive
		{ get; }

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
		void DisposeAmbientStorageTransaction();
		
		/// <summary>
		/// 	Subscribe for notification about the completion of the ambient Storage transaction.
		/// </summary>
		/// <param name="subscriber">
		/// 	The subscriber to notify
		/// </param>
		void SubscribeForAmbientTransactionCompletion(ITransactionNotification subscriber);
		
		/// <summary>
		/// 	Get ambient thread-local (context) transaction. Returns <see langword="null"/> if none.
		/// </summary>
		/// <remarks>
		/// </remarks>
		IFileSystemTransaction AmbientTransaction
		{ get; }
		
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
		///		Creates new scope, starts new independent storage transaction regardless of current storage and managed context.
		/// </remarks>
		IStorageTransactionScope CreateStandaloneTransactionScope(bool dispose);

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
		IStorageTransactionScope CreateSlaveTransactionScope(bool dispose);

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
		///		regardless of current storage and managed context.
		/// </remarks>
		IStorageTransactionScope CreateSlaveTransactionScope(Transaction masterTransaction, bool dispose);
		
		/// <summary>
		///		Create transaction scope wrapping existing storage transaction.
		/// </summary>
		/// <param name="transactionToUse">
		///		Transaction to wrap. May be null - will clear transaction context.
		/// </param>
		/// <param name="dispose">
		///		Dispose underlying storage transaction at the end of the scope.
		/// </param>
		/// <returns>
		///		New scope.
		/// </returns>
		IStorageTransactionScope CreateTransactionScope(IFileSystemTransaction transactionToUse, bool dispose);


	}
}
