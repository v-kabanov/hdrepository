using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.IO.WinNtfs;
using Microsoft.Win32.SafeHandles;

namespace bfs.Repository.IO.WinNtfs
{
	/// <summary>
	///		The class implements <see cref="IFileProvider"/> using kernel32 long file name
	///		support through interop.
	/// </summary>
	public class WinLongFileFrovider : IFileProvider
	{
		/// <summary>
		///		<see cref="IFileProvider.Exists"/>
		/// </summary>
		public bool Exists(string path)
		{
			return new LongPath(path).IsFile;
		}

		/// <summary>
		///		<see cref="IFileProvider.Delete"/>
		/// </summary>
		public void Delete(string path)
		{
            var longPath = new LongPath(path);
            
            bool result;
            if (KtmTransaction.IsInTransaction)
            {
            	result = WindowsNative.DeleteFileTransacted(longPath.PathString, KtmTransaction.Current.Hanlde);
            }
            else
            {
	            result = WindowsNative.DeleteFile(longPath.PathString);
            }
            if (!result)
            {
                WindowsNative.HandleWindowsError();
            }
		}

		/// <summary>
		///		<see cref="IFileProvider.Move"/>
		/// </summary>
		public void Move(string sourcePath, string destinationPath)
		{
			FilesystemUtil.Move(sourcePath, destinationPath);
		}

		/// <summary>
		///		<see cref="IFileProvider.Copy"/>
		/// </summary>
		public void Copy(string sourcePath, string destinationPath, bool overwrite)
		{
            var longPathSource = new LongPath(sourcePath);
            var longPathDestination = new LongPath(destinationPath);

            bool result;
            if (KtmTransaction.IsInTransaction)
            {
            	bool cancel = false;
            	result = WindowsNative.CopyFileTransacted(
            		longPathSource.PathString
            		, longPathDestination.PathString
            		, IntPtr.Zero
            		, IntPtr.Zero
            		, ref cancel
            		, WindowsNative.CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS
            		, KtmTransaction.Current.Hanlde);
            }
            else
            {
            	result = WindowsNative.CopyFile(longPathSource.PathString, longPathDestination.PathString, !overwrite);
            }
            if (!result)
            {
                WindowsNative.HandleWindowsError();
            }
		}

		/// <summary>
		///		<see cref="IFileProvider.Open"/>
		/// </summary>
		public System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access)
		{
            return Open(path, mode, access, FileShare.None, 0, FileOptions.None);
		}

		/// <summary>
		///		<see cref="IFileProvider.Open"/>
		/// </summary>
		public System.IO.FileStream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share)
		{
            return Open(path, mode, access, share, 0, FileOptions.None);
		}

		/// <summary>
		///		<see cref="IFileProvider.Open"/>
		/// </summary>
		public System.IO.FileStream Open(
			string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share, int bufferSize, System.IO.FileOptions options)
		{
            if (bufferSize == 0)
			{
                bufferSize = 1024;
			}

			var longPath = new LongPath(path);

            SafeFileHandle handle = GetFileHandle(longPath, mode, access, share, options);

            return new FileStream(handle, access, bufferSize, (options & FileOptions.Asynchronous) == FileOptions.Asynchronous);
		}
		
		#region private methods
		
        private static SafeFileHandle GetFileHandle(LongPath path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            SafeFileHandle handle;
            
            if (KtmTransaction.IsInTransaction)
            {
	            WindowsNative.FileMode internalMode = WindowsNative.TranslateFileMode(mode);
	            WindowsNative.FileShare internalShare = WindowsNative.TranslateFileShare(share);
	            WindowsNative.FileAccess internalAccess = WindowsNative.TranslateFileAccess(access);
	            
	            KtmTransactionHandle ktmTx = KtmTransaction.Current.Hanlde;
	
	            // Create the transacted file using P/Invoke.
	            handle = WindowsNative.CreateFileTransacted(
	                path.PathString,
	                internalAccess,
	                internalShare,
	                IntPtr.Zero,
	                internalMode,
	                WindowsNative.EFileAttributes.Normal,
	                IntPtr.Zero,
	                KtmTransaction.Current.Hanlde,
	                IntPtr.Zero,
	                IntPtr.Zero);
	        }
            else
            {
	            WindowsNative.EFileAccess underlyingAccess = WindowsNative.GetUnderlyingAccess(access);
            	handle = WindowsNative.CreateFile(path.PathString, underlyingAccess, (uint)share, IntPtr.Zero, (uint)mode, (uint)options, IntPtr.Zero);
            }

            if (handle.IsInvalid)
            {
                WindowsNative.HandleWindowsError();
            }

            return handle;
        }
		
		#endregion private methods
	}
}
