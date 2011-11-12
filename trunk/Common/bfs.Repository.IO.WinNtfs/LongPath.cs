using System;

using bfs.Repository.Util;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace bfs.Repository.IO.WinNtfs
{
	public class LongPath
	{
    	private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Path).Name);
		
		private const string _currentDirectoryAlias = ".";
		private const string _searchPatternAll = "*";
		
		public LongPath(string path)
		{
			Check.DoRequireArgumentNotNull(path, "path");
			Check.DoCheckArgument(!string.IsNullOrEmpty(path), string.Empty, "path");

			StringBuilder bld = new StringBuilder(path.Length + 5);
			uint size = WindowsNative.GetFullPathName(path, (uint)bld.Capacity, bld, IntPtr.Zero);

			if (size > bld.Capacity)
			{
				bld.Capacity = (int)size + 5;
				size = WindowsNative.GetFullPathName(path, size, bld, IntPtr.Zero);
			}

			Check.DoAssertLambda(size > 0, () => WindowsNative.GetLastErrorException("path"));
			Check.DoAssertLambda(size <= WindowsNative.MAX_LONG_PATH, () => new PathTooLongException());
			
			bld.Insert(0, WindowsNative.LongPathPrefix);
			
			PathString = bld.ToString();
		}
		
		/// <remarks>
		/// 	Will return WindowsNative.INVALID_FILE_ATTRIBUTES if failed and throwException is false.
		/// </remarks>
		/// <exception cref='Win32Exception'>
		/// 	Call to native windows function failed.
		/// </exception>
		public FileAttributes GetFileAttributes(bool throwException)
		{
			FileAttributes retval = WindowsNative.INVALID_FILE_ATTRIBUTES;
			
			if (!KtmTransaction.IsInTransaction)
			{
				retval = WindowsNative.GetFileAttributes(PathString);
			}
			else
			{
				WindowsNative.WIN32_FILE_ATTRIBUTE_DATA attrData = new WindowsNative.WIN32_FILE_ATTRIBUTE_DATA();
				if (WindowsNative.GetFileAttributesTransacted(
            		PathString
            		, WindowsNative.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard
            		, out attrData
            		, KtmTransaction.Current.Hanlde))
				{
					retval = attrData.dwFileAttributes;
				}
			}
            
			Check.DoAssertLambda(!throwException || retval != WindowsNative.INVALID_FILE_ATTRIBUTES, () => new Win32Exception());

			return retval;
		}
		
		public bool Exists()
		{
			bool isDirectory;
			return Exists(out isDirectory);
		}

		public bool Exists(out bool isDirectory)
		{
			bool retval = false;
			isDirectory = false;
			
			FileAttributes attributes = GetFileAttributes(false);
			if (attributes != WindowsNative.INVALID_FILE_ATTRIBUTES)
			{
				isDirectory = IsDirectory;
				retval = true;
			}
			return retval;
		}
		
		/// <summary>
		/// 	Gets a value indicating whether the path refers to an existing directory.
		/// </summary>
		public bool IsDirectory
		{
			get
			{
				FileAttributes attr = GetFileAttributes(false);
				return attr != WindowsNative.INVALID_FILE_ATTRIBUTES && ((attr & FileAttributes.Directory) == FileAttributes.Directory);
			}
		}
		
		/// <summary>
		/// 	Gets a value indicating whether the path refers to an existing file.
		/// </summary>
		public bool IsFile
		{
			get
			{
				FileAttributes attr = GetFileAttributes(false);
				return attr != WindowsNative.INVALID_FILE_ATTRIBUTES && ((attr & FileAttributes.Directory) != FileAttributes.Directory);
			}
		}
		
		/// <summary>
		/// 	Gets the path as normalized path string prefixed with "\\?\".
		/// </summary>
		public string PathString
		{ get; private set; }
		
		/// <summary>
		/// 	Gets the path as normalized path string without "\\?\" prefix.
		/// </summary>
		public string UnPrefixedPathString
		{
			get
			{
				return PathString.Substring(WindowsNative.LongPathPrefix.Length);
			}
		}
	}
}

