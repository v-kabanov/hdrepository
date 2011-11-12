/*
 * User: vasily
 * Date: 25/02/2011
 * Time: 7:44 PM
 * 
 */
using System;
using System.Transactions;


namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	/// Description of IStorageTransaction.
	/// </summary>
	public interface IFileSystemTransaction : IDisposable
	{
		/// <summary>
		/// 	Whether the transaction is under way.
		/// </summary>
		bool IsActive
		{ get; }

		/// <summary>
		/// 	Whether the transaction is part of the <see>System.Transactions.Transaction.Current</see>.
		/// </summary>
		bool IsPartOfManagedAmbient
		{ get; }

		/// <summary>
		/// 	Whether the transaction belongs to another transaction (typically managed, distributed) and cannot be committed or rolled back
		/// 	independently.
		/// </summary>
		/// <remarks>
		/// 	If this returns <see langword="true"/> <see cref="MasterTransaction"/> should return owning managed transaction.
		/// </remarks>
		bool IsSlave
		{ get; }

		/// <summary>
		/// 	Get master transaction this KTM transaction belongs to.
		/// </summary>
		/// <remarks>
		/// 	This will return <see langword="null"/> if <see cref="IsSlave"/> returns <see langword="false"/>.
		/// 	Slave transaction cannot be committed or rolled back independently. Use master transaction instead.
		/// </remarks>
		Transaction MasterTransaction
		{ get; }

		/// <summary>
		/// 	Get transaction status.
		/// </summary>
		/// <remarks>
		/// 	Immediately after creating transaction its status is "Active".
		/// 	Afterwards if transaction if this transaction has been created from the ambient managed one, its status will be
		/// 	the same as status of the <see cref="MasterTransaction"/>. Otherwise (for standalone, local transactions),
		/// 	the status will be set when calling <see cref="Commit"/> or <see cref="Rollback"/>
		/// </remarks>
		TransactionStatus TransactionStatus
		{ get; }

		/// <summary>
		///		Commit transaction.
		/// </summary>
		void Commit();
		
		/// <summary>
		///		Rollback transaction.
		/// </summary>
		void Rollback();
		
		/// <summary>
		/// 	Subscribe to receive notification when the transaction ends.
		/// </summary>
		/// <param name="subscriber">
		/// 	The subscriber to be notified when the transaction ends.
		/// </param>
		/// <remarks>
		/// 	If the subscriber is already subscribed the call has no effect.
		/// </remarks>
		void Subscribe(ITransactionNotification subscriber);
		
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
		bool Unsubscribe(ITransactionNotification subscriber);
	}
}
