/*
 * User: vasily
 * Date: 28/02/2011
 * Time: 8:39 PM
 * 
 */
using System;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// 	Completely independent transactions when supported: DisallowJoiningAmbientManaged | AlwaysStartNew
	/// 	Completely independent transactions, require: DisallowJoiningAmbientManaged | AlwaysStartNew | RequireTransactions
	/// 	[Vista+] No DTC but join ambient KTM transaction when available or start own: DisallowJoiningAmbientManaged
	/// </remarks>
	/// <see cref="IRepositoryManager"/>
	[Flags]
	public enum StorageTransactionSettings
	{
		/// <summary>
		/// 	Prohibit the use of transaction even if supported
		/// </summary>
		NoTransactions 					= 0x00000001,
		/// <summary>
		/// 	If no transaction supported refuse to work
		/// </summary>
		RequireTransactions				= 0x00000002,
		/// <summary>
		/// 	Never join ambient managed transaction (means no DTC under Vista+)
		/// </summary>
		DisallowJoiningAmbientManaged 	= 0x00000004,
		/// <summary>
		/// 	Do not use ambient storage transaction, start own (but restore context upon return). Own storage transaction
		/// 	may join ambient managed if DisallowJoiningAmbientManaged is not set
		/// </summary>
		AlwaysStartNew					= 0x00000008
	}
	
	/// <summary>
	///		Repository settings
	/// </summary>
	public interface IRepositorySettings
	{
		/// <summary>
		/// 	Get or set transaction handling settings
		/// </summary>
		StorageTransactionSettings StorageTransactionSettings
		{ get; set; }
	}
}
