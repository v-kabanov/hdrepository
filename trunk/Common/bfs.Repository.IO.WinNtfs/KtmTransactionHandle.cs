/*
 * User: vasily
 * Date: 22/02/2011
 * Time: 9:51 PM
 * 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;
using System.Transactions;

using bfs.Repository.Interfaces.Infrastructure;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace bfs.Repository.IO.WinNtfs
{
	/// <summary>
	/// 	Safe handle of a Kernel Transaction
	/// </summary>
	public class KtmTransactionHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		/// <summary>
		/// 	The invalid KTM transaction handle
		/// </summary>
		public static readonly KtmTransactionHandle InvalidHandle = new KtmTransactionHandle(IntPtr.Zero);

		/// <summary>
		/// 	http://msdn.microsoft.com/en-us/library/aa344210(VS.85).aspx
		/// </summary>
		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("79427A2B-F895-40e0-BE79-B57DC82ED231")]
		private interface IKernelTransaction
		{
			void GetHandle([Out] out IntPtr handle);
		}

		/// <summary>
		/// 	Create new instance and initialize it with the specified raw handle.
		/// </summary>
		/// <param name="handle">
		/// 	Raw handle to wrap.
		/// </param>
		public KtmTransactionHandle(IntPtr handle): base(true)
		{
			this.handle = handle;
		}
		
		/// <summary>
		/// 	Commit the transaction
		/// </summary>
		/// <exception cref="Win32Exception">
		/// 	Commit failed.
		/// </exception>
		public void Commit()
		{
			bool committed = WindowsNative.CommitTransaction(this);
			//OnTransactionEnded(committed);
			if (!committed)
			{
				WindowsNative.HandleWindowsError();
			}
		}
		
		/// <summary>
		/// 	Rollback the transaction
		/// </summary>
		/// <exception cref="Win32Exception">
		/// 	Rollback failed.
		/// </exception>
		public void Rollback()
		{
			if (WindowsNative.RollbackTransaction(this))
			{
				WindowsNative.HandleWindowsError();
			}
		}
		
		/// <summary>
		/// 	Start local KTM transaction
		/// </summary>
		/// <returns>
		/// 	New transaction handle
		/// </returns>
		public static KtmTransactionHandle CreateLocalTransaction()
		{
			return new KtmTransactionHandle(WindowsNative.CreateTransaction(IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, null));
		}

		/// <summary>
		/// 	Get KTM transaction from ambient managed transaction (<see cref="Transaction.Current" />)
		/// </summary>
		/// <remarks>
		/// 	Currently this will require MS DTC service running. The created transaction should not be committed
		/// 	or rolled back itself explicitly. Use the owning managed transaction to control it.
		/// 	http://msdn.microsoft.com/en-us/library/cc303707.aspx
		/// </remarks>
		public static KtmTransactionHandle GetFromAmbientTransaction()
		{
			if (Transaction.Current == null)
				throw new InvalidOperationException("Cannot create a KTM handle without Transaction.Current");
			
			return KtmTransactionHandle.GetFromManaged(Transaction.Current);
		}

		/// <summary>
		/// 	Get KTM transaction from the specified managed <paramref name="managedTransaction" />
		/// </summary>
		/// <param name="managedTransaction">
		/// 	Owning managed transaction
		/// </param>
		/// <remarks>
		/// 	Currently this will require MS DTC service running. The created transaction should not be committed
		/// 	or rolled back itself explicitly. Use the owning managed transaction to control it.
		/// 	http://msdn.microsoft.com/en-us/library/cc303707.aspx
		/// </remarks>
		public static KtmTransactionHandle GetFromManaged(Transaction managedTransaction)
		{
			IKernelTransaction tx = (IKernelTransaction)TransactionInterop.GetDtcTransaction(Transaction.Current);
			IntPtr txHandle;
			tx.GetHandle(out txHandle);
			
			if (txHandle == IntPtr.Zero)
				throw new Win32Exception("Could not get KTM transaction handle.");
			
			return new KtmTransactionHandle(txHandle);
		}
		
		/// <summary>
		/// 	Release handle
		/// </summary>
		/// <returns>
		/// 	bool
		/// </returns>
		protected override bool ReleaseHandle()
		{
			return WindowsNative.CloseHandle(handle);
		}
	}
}
