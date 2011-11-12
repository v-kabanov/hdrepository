using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.IO.WinNtfs;
using bfs.Repository.Util;

namespace bfs.Repository.IO.WinNtfs
{
	/// <summary>
	///		The class implements <see cref="IDirectoryProvider"/> using kernel32 long directory/file
	///		name support through interop.
	/// </summary>
	public class WinLongDirectoryProvider : IDirectoryProvider
	{
    	private static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(WinLongDirectoryProvider));

		public bool Exists(string path)
		{
			LongPath longPath = new LongPath(path);
			return longPath.IsDirectory;
		}

		public IEnumerable<string> EnumerateDirectories(string path)
		{
			return EnumerateDirectories(path, (string)null);
		}

		public IEnumerable<string> EnumerateDirectories(string directoryPath, string searchPattern)
		{
            return EnumerateFileSystemEntries(directoryPath, searchPattern, includeDirectories: true, includeFiles: false);
		}

		public IEnumerable<string> EnumerateFiles(string path)
		{
			return EnumerateFiles(path, (string)null);
		}

		public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
		{
            return EnumerateFileSystemEntries(path, searchPattern, includeDirectories: false, includeFiles: true);
		}

		public void Create(string path)
		{
			LongPath longPath = new LongPath(path);

            bool result;
            if (KtmTransaction.IsInTransaction)
            {
            	result = WindowsNative.CreateDirectoryTransacted(null, longPath.PathString, IntPtr.Zero, KtmTransaction.Current.Hanlde);
            }
            else
            {
            	result = WindowsNative.CreateDirectory(longPath.PathString, IntPtr.Zero);
            }
            if (!result)
			{
                int errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (errorCode != WindowsNative.ERROR_ALREADY_EXISTS || !longPath.Exists())
				{
                    WindowsNative.HandleWindowsError(errorCode);
                }
			}
		}

		public void Delete(string path)
		{
			LongPath longPath = new LongPath(path);

            bool result;
            if (KtmTransaction.IsInTransaction)
            {
            	result = WindowsNative.RemoveDirectoryTransacted(longPath.PathString, KtmTransaction.Current.Hanlde);
            }
            else
            {
            	result = WindowsNative.RemoveDirectory(longPath.PathString);
            }
            if (!result)
			{
                WindowsNative.HandleWindowsError();
            }
		}

		public void Move(string oldPath, string newPath)
		{
			FilesystemUtil.Move(oldPath, newPath);
		}

		/// <summary>
		///		Given a path to a directory or file return its last component
		///		which would be directory or file name.
		/// </summary>
		/// <param name="directoryPath">
		///		Any valid path such as "c:\foo\poo\".
		/// </param>
		/// <returns>
		///		Last path component, such as "poo" (for "c:\foo\poo\")
		/// </returns>
		public string GetLastPathComponent(string path)
		{
			return Util.FileSystem.GetLastPathComponent(path);
		}

		/// <summary>
		///		Delete a directory.
		///		<seealso cref="Delete(System.String)"/>
		/// </summary>
		/// <param name="path">
		///		Path tto the directory to delete.
		/// </param>
		/// <param name="recursive">
		///		Whether to remove all contained files and directories.
		///		If the parameter is <see langword="false"/> the method is the same
		///		as <see cref="Delete(System.String)"/>.
		/// </param>
		public void Delete(string path, bool recursive)
		{
			WinLongFileFrovider fileProvider = new WinLongFileFrovider();
			if (recursive)
			{
				IEnumerable<string> filePaths = EnumerateFiles(path);
				foreach (string filePath in filePaths)
				{
					fileProvider.Delete(filePath);
				}

				IEnumerable<string> subFolderPaths = EnumerateDirectories(path);

				foreach (string subFolderPath in subFolderPaths)
				{
					Delete(subFolderPath, recursive);
				}
			}
			Delete(path);
		}

		/// <summary>
		///		Get max length of directory path as supported by the provider
		///		(requirements of repository itself are not counted).
		///		On Windows it's 32500 (10 reserved here)
		/// </summary>
		public int MaxDirectoryPathLengh
		{
			get
			{
				return 32490;
			}
		}

		/// <summary>
		///		Get max length of a single component of file system path.
		///		On windows it's 255.
		/// </summary>
		public int MaxPathElementLength
		{
			get
			{
				return 255;
			}
		}
		
		#region private methods


        private static IEnumerable<string> EnumerateFileSystemEntries(string path)
		{
            return EnumerateFileSystemEntries(path, (string)null);
        }

        private static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
		{
            return EnumerateFileSystemEntries(path, searchPattern, includeDirectories: true, includeFiles: true);
        }

        private static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, bool includeDirectories, bool includeFiles)
		{
        	_log.DebugFormat("EnumerateFileSystemEntries({0}, {1}, {2}, {3})", path, searchPattern, includeDirectories, includeFiles);
			
			LongPath longPath = new LongPath(path);
			Check.Require(longPath.IsDirectory, "path");

            return FilesystemUtil.EnumerateFileSystemEntries(longPath, searchPattern, includeDirectories, includeFiles);
        }
		
		
		#endregion private methods

	}
}
