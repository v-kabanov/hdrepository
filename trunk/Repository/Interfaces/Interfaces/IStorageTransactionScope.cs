/*
 * User: vasily
 * Date: 26/02/2011
 * Time: 4:16 PM
 * 
 */
using System;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces
{
	
	/// <summary>
	/// 	Storage transaction scope.
	/// </summary>
	public interface IStorageTransactionScope : IDisposable
	{
		/// <summary>
		/// 	Whether the underlying transaction is going to be disposed when the scope is being disposed. Note that scope can be disposed
		/// 	without ending transaction even if it it is not transaction owner (<see cref="IsTransactionOwner"/>).
		/// </summary>
		/// <remarks>
		///		Usage example: need scope joining managed ambient transaction and do not want to reuse the storage (e.g. KTM) transaction
		///		instance. The KTM transaction instance will be installed into the context and removed/disposed at the end of the scope, but
		///		the actual transaction will continue beyond the end of the scope.
		/// </remarks>
		bool ToBeDisposed
		{ get; }

		/// <summary>
		/// 	Whether the context (ambient KTM transaction) has been changed by the scope.
		/// </summary>
		bool HasChangedContext
		{ get; }

		/// <summary>
		/// 	Whether the scope owns transaction, i.e. it will end it when disposed.
		/// </summary>
		/// <remarks>
		/// 	If the scope owns the transaction it will be disposed (<see cref="ToBeDisposed"/> will be <see langword="true"/>) and committed
		/// 	or rolled back when the scope is disposed.
		/// </remarks>
		bool IsTransactionOwner
		{ get; }

		/// <summary>
		/// 	Get underlying file system transaction.
		/// </summary>
		IFileSystemTransaction UnderlyingTransaction
		{ get; }

		/// <summary>
		/// 	Get file system transaction which was active before the scope started.
		/// </summary>
		IFileSystemTransaction PreviousTransaction
		{ get; }
		
		/// <summary>
		/// 	Detach the underlying transaction from the context. This will ensure that the transaction will not be disposed
		/// 	but context handling will not change.
		/// </summary>
		/// <returns>
		/// 	<see cref="UnderlyingTransaction"/>
		/// </returns>
		IFileSystemTransaction DetachTransaction();
		
		/// <summary>
		/// 	Mark scope for completion.
		/// </summary>
		/// <remarks>
		/// 	If the KTM transaction is standalone it will be committed at the end of the scope -
		/// 	when disposing transaction scope.
		/// </remarks>
		void Complete();
	}
}
