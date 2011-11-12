/*
 * User: vasily
 * Date: 2/03/2011
 * Time: 9:40 PM
 * 
 */
using System;
using System.Runtime.ConstrainedExecution;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	/// 	Interface of a subscriber to notifications about storage transaction progress. Used to define what to do
	/// 	before transaction is committed and after transaction is either committed or rolled back.
	/// </summary>
	[CLSCompliant(true)]
	public interface ITransactionNotification
	{
		/// <summary>
		///		Transaction is being prepared and about to be commiitted (first fase).
		/// </summary>
		/// <exception cref="TransactionNotificationException">
		///		An error has occurred.
		///		The exception will result in transaction commit being cancelled.
		/// </exception>
		/// <remarks>
		///		Note that this method may be executed in a concurrent worker thread.
		/// </remarks>
		void Prepare();

		/// <summary>
		/// 	Must not throw exceptions
		/// </summary>
		/// <param name="committed">
		/// 	Whether transaction was committed successfully.
		/// </param>
		/// <remarks>
		///		Note that this method may be executed in a concurrent worker thread.
		/// </remarks>
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		void TransactionCompleted(IFileSystemTransaction transaction, bool committed);
	}
}
