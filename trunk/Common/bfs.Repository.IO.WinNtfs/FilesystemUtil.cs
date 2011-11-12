// 
//  FilesystemUtil.cs
//  
//  Author:
//       Vasily <vkabanov@ymail.com>
//  
//  Copyright (c) 2011 Vasily
// 
using System;
using System.Collections.Generic;
using bfs.Repository.Util;
using System.IO;
using System.Runtime.InteropServices;

namespace bfs.Repository.IO.WinNtfs
{
	public static class FilesystemUtil
	{
    	private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(FilesystemUtil));

		internal static IEnumerable<string> EnumerateFileSystemEntries(LongPath path, string searchPattern, bool includeDirectories, bool includeFiles)
		{
			Check.DoRequireArgumentNotNull(path, "path");
			Check.Require(path.IsDirectory, "path");
			
        	_log.DebugFormat("EnumerateFileSystemEntries({0}, {1}, {2}, {3})", path.PathString, searchPattern, includeDirectories, includeFiles);


            return EnumerateFileSystemIterator(path, searchPattern, includeDirectories, includeFiles);
        }
		
        private static IEnumerable<string> EnumerateFileSystemIterator(
        	LongPath path, string mask, bool enumerateDirectories, bool enumerateFiles)
        {
			if (string.IsNullOrEmpty(mask) || mask == ".")
			{
				mask = "*";
			}
			string unprefixedPath = path.UnPrefixedPathString;
			
            WindowsNative.WIN32_FIND_DATA findData;
            using (FindFileHandle handle = BeginFind(Path.Combine(path.PathString, mask), out findData))
			{
                if (handle == null)
				{
                    yield break;
				}

                do
				{
                    string fileName = findData.cFileName;

                    if (IsDirectory(findData.dwFileAttributes))
					{
                        if (enumerateDirectories && IsRealFolder(fileName))
						{
                            yield return Path.Combine(unprefixedPath, fileName);
                        }
                    }
                    else {
                        if (enumerateFiles)
						{
                            yield return Path.Combine(unprefixedPath, fileName);
                        }
                    }
                }
				while (WindowsNative.FindNextFile(handle, out findData));

                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != WindowsNative.ERROR_NO_MORE_FILES)
				{
                    WindowsNative.HandleWindowsError(errorCode);
				}
            }
        }

		internal static bool IsDirectory(FileAttributes attributes)
		{
            return (attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        private static bool IsRealFolder(string directoryName)
		{
            return !directoryName.Equals(".", StringComparison.OrdinalIgnoreCase) && !directoryName.Equals("..", StringComparison.OrdinalIgnoreCase);
        }

        private static FindFileHandle BeginFind(string searchPattern, out WindowsNative.WIN32_FIND_DATA findData)
        {
        	FindFileHandle handle = null;
            if (KtmTransaction.IsInTransaction)
            {
            	handle = WindowsNative.FindFirstFileTransacted(
            		searchPattern
            		, WindowsNative.FINDEX_INFO_LEVELS.FindExInfoBasic
            		, out findData
            		, WindowsNative.FINDEX_SEARCH_OPS.FindExSearchNameMatch
            		, IntPtr.Zero
            		, 0				// 1 - case sensitive
            		, KtmTransaction.Current.Hanlde);
            }
        	else
        	{
            	handle = WindowsNative.FindFirstFile(searchPattern, out findData);
        	}
        	
            if (handle.IsInvalid) {

                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != WindowsNative.ERROR_FILE_NOT_FOUND)
				{
                    WindowsNative.HandleWindowsError(errorCode);
				}

                return null;
            }

            return handle;  
        }

		internal static void Move(string sourcePath, string destinationPath)
        {
            var longPathSource = new LongPath(sourcePath);
            var longPathDestination = new LongPath(destinationPath);
			
            bool result;
            if (KtmTransaction.IsInTransaction)
            {
            	result = WindowsNative.MoveFileTransacted(
            		longPathSource.PathString
            		, longPathDestination.PathString
            		, IntPtr.Zero
            		, IntPtr.Zero
            		, WindowsNative.MoveFileFlags.NONE
            		, KtmTransaction.Current.Hanlde);
            }
            else
            {
            	result = WindowsNative.MoveFile(longPathSource.PathString, longPathDestination.PathString);
            }
            if (!result)
            {
                WindowsNative.HandleWindowsError();
            }
        }
	}
}

