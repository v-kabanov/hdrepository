using System;
using System.IO;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.ComponentModel;


namespace bfs.Repository.IO.WinNtfs
{
	public static class WindowsNative
	{

		// Enumerations that capture Win32 values.
        [Flags]
        internal enum FileShare : uint
        {
            FILE_SHARE_NONE = 0x00,
            FILE_SHARE_READ = 0x01,
            FILE_SHARE_WRITE = 0x02,
            FILE_SHARE_DELETE = 0x04
        }

        internal enum FileMode
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        internal enum FileAccess : uint
        {
            GENERIC_READ = unchecked((uint)0x80000000),
            GENERIC_WRITE = 0x40000000
        }
        
		[Flags]
		internal enum MoveFileFlags : uint
		{
		    NONE           						= 0x00000000,
		    MOVEFILE_REPLACE_EXISTING           = 0x00000001,
		    MOVEFILE_COPY_ALLOWED               = 0x00000002,
		    MOVEFILE_DELAY_UNTIL_REBOOT         = 0x00000004,
		    MOVEFILE_WRITE_THROUGH              = 0x00000008,
		    MOVEFILE_CREATE_HARDLINK            = 0x00000010,
		    MOVEFILE_FAIL_IF_NOT_TRACKABLE      = 0x00000020
		}
        // Win32 Error codes.
        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_RECOVERY_NOT_NEEDED = 6821;

		
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_FILE_NAME_TOO_LONG = 0xCE;
        internal const int ERROR_DIRECTORY = 0x10B;
        internal const FileAttributes INVALID_FILE_ATTRIBUTES = (FileAttributes)(-1);

        internal const int MAX_PATH = 260;
        // actual maxium is 32767 characters
        internal const int MAX_LONG_PATH = 32500;
        internal const int MAX_ALTERNATE = 14;

        internal const uint FIND_FIRST_EX_CASE_SENSITIVE = 1;

        internal const string LongPathPrefix = @"\\?\";

        [Flags]
        internal enum EFileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }        

        //http://msdn.microsoft.com/en-us/library/bb736257(v=vs.85).aspx
        internal enum GET_FILEEX_INFO_LEVELS : uint
        {
           GetFileExInfoStandard = 0
        }

        /// <summary>
        /// 	Used in CopyFileTransacted
        /// </summary>
        /// <remarks>
        ///		http://msdn.microsoft.com/en-us/library/aa363853(v=VS.85).aspx
        /// </remarks>
        [Flags]
		internal enum CopyFileFlags : uint
		{
		    COPY_FILE_FAIL_IF_EXISTS          		= 0x00000001,
		    // Progress of the copy is tracked in the target file in case the copy fails. The failed copy can be restarted at a later
		    // time by specifying the same values for lpExistingFileName and lpNewFileName as those used in the call that failed.
		    COPY_FILE_RESTARTABLE         			= 0x00000002,
		    // The file is copied and the original file is opened for write access.
		    COPY_FILE_OPEN_SOURCE_FOR_WRITE       	= 0x00000004,
		    COPY_FILE_ALLOW_DECRYPTED_DESTINATION 	= 0x00000008,
		    // If the source file is a symbolic link, the destination file is also a symbolic link pointing to the same
		    // file that the source symbolic link is pointing to.
		    COPY_FILE_COPY_SYMLINK        			= 0x00000800 //NT 6.0+
		}

		/// <summary>
		/// 	Used in CreateFileTransacted (... dwFlagsAndAttributes)
		/// </summary>
		/// <remarks>
		///		http://www.pinvoke.net/default.aspx/kernel32/CreateFile.html
		///		http://msdn.microsoft.com/en-us/library/gg258117(v=VS.85).aspx
		/// </remarks>
		[Flags]
		internal enum EFileAttributes : uint
		{
			Readonly         = 0x00000001,
			Hidden           = 0x00000002,
			System           = 0x00000004,
			Directory        = 0x00000010,
			Archive          = 0x00000020,
			Device           = 0x00000040,
			Normal           = 0x00000080,
			Temporary        = 0x00000100,
			SparseFile       = 0x00000200,
			ReparsePoint     = 0x00000400,
			Compressed       = 0x00000800,
			Offline          = 0x00001000,
			NotContentIndexed= 0x00002000,
			Encrypted        = 0x00004000,
			Write_Through    = 0x80000000,
			Overlapped       = 0x40000000,
			NoBuffering      = 0x20000000,
			RandomAccess     = 0x10000000,
			SequentialScan   = 0x08000000,
			DeleteOnClose    = 0x04000000,
			BackupSemantics  = 0x02000000,
			PosixSemantics   = 0x01000000,
			OpenReparsePoint = 0x00200000,
			OpenNoRecall     = 0x00100000,
			FirstPipeInstance= 0x00080000
		}

		/// <summary>
		/// 	Used in FindFirstFileTransacted
		/// </summary>
		/// <remarks>
		///		http://www.pinvoke.net/default.aspx/Enums/FINDEX_INFO_LEVELS.html
		///		http://msdn.microsoft.com/en-us/library/aa364415(v=vs.85).aspx
		/// </remarks>
		internal enum FINDEX_INFO_LEVELS : uint
		{
		    FindExInfoStandard=0,
		    FindExInfoBasic=1
		}
		
		/// <summary>
		/// 	Used in FindFirstFileTransacted.
		/// </summary>
		/// <remarks>
		/// 	http://www.pinvoke.net/default.aspx/Enums/FINDEX_SEARCH_OPS.html
		/// 	http://msdn.microsoft.com/en-us/library/aa364416(v=vs.85).aspx
		/// </remarks>
		internal enum FINDEX_SEARCH_OPS : uint
		{
		     FindExSearchNameMatch = 0,
		     FindExSearchLimitToDirectories = 1,
		     FindExSearchLimitToDevices = 2
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle([In] IntPtr handle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WIN32_FIND_DATA {
            internal FileAttributes dwFileAttributes;
            internal FILETIME ftCreationTime;
            internal FILETIME ftLastAccessTime;
            internal FILETIME ftLastWriteTime;
            internal int nFileSizeHigh;
            internal int nFileSizeLow;
            internal int dwReserved0;
            internal int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
            internal string cAlternate;
        }
        
//		typedef struct _WIN32_FILE_ATTRIBUTE_DATA {
//		  DWORD    dwFileAttributes;
//		  FILETIME ftCreationTime;
//		  FILETIME ftLastAccessTime;
//		  FILETIME ftLastWriteTime;
//		  DWORD    nFileSizeHigh;
//		  DWORD    nFileSizeLow;
//		} WIN32_FILE_ATTRIBUTE_DATA, *LPWIN32_FILE_ATTRIBUTE_DATA;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WIN32_FILE_ATTRIBUTE_DATA
		{
			internal FileAttributes dwFileAttributes;
			internal FILETIME ftCreationTime;
			internal FILETIME ftLastAccessTime;
			internal FILETIME ftLastWriteTime;
			internal int nFileSizeHigh;
			internal int nFileSizeLow;
		}

        public static int ErrorCodeToHResult(int errorCode)
		{
            return unchecked((int)0x80070000 | errorCode);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CopyFile(string src, string dst, [MarshalAs(UnmanagedType.Bool)]bool failIfExists);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern FindFileHandle FindFirstFile(string fileName, out WIN32_FIND_DATA findFileData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindNextFile(FindFileHandle findFileHandle, out WIN32_FIND_DATA findFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose(IntPtr findFileHandle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetFullPathName(string fileName, uint bufferSize, StringBuilder buffer, IntPtr nullPtr);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string fileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveDirectory(string directoryPath);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateDirectory(string path, IntPtr securityAttributes);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MoveFile(string existingFilePath, string newFilePath);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            EFileAccess desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern FileAttributes GetFileAttributes(string fileName);
        
        [DllImport("KERNEL32.dll", EntryPoint = "CreateFileTransactedW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFileTransacted(
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [In] WindowsNative.FileAccess dwDesiredAccess,
            [In] WindowsNative.FileShare dwShareMode,
            [In] IntPtr lpSecurityAttributes,
            [In] WindowsNative.FileMode dwCreationDisposition,
            [In] EFileAttributes dwFlagsAndAttributes,
            [In] IntPtr hTemplateFile,
            [In] KtmTransactionHandle hTransaction,
            [In] IntPtr pusMiniVersion,
            [In] IntPtr pExtendedParameter);
        
//		BOOL WINAPI CreateDirectoryTransacted(
//		  __in_opt  LPCTSTR lpTemplateDirectory,
//		  __in      LPCTSTR lpNewDirectory,
//		  __in_opt  LPSECURITY_ATTRIBUTES lpSecurityAttributes,
//		  __in      HANDLE hTransaction
//		);
        // http://msdn.microsoft.com/en-us/library/aa363857(v=VS.85).aspx
        [DllImport("KERNEL32.dll", EntryPoint = "CreateDirectoryTransactedW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateDirectoryTransacted(
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpTemplateDirectory,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpNewDirectory,
            [In] IntPtr lpSecurityAttributes,
            [In] KtmTransactionHandle hTransaction);
        
        /// <summary> 
		/// http://msdn.microsoft.com/en-us/library/aa363916(VS.85).aspx 
		/// </summary> 
		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError=true)] 
        [return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool DeleteFileTransacted(
			[In, MarshalAs(UnmanagedType.LPWStr)] string file
			, [In] KtmTransactionHandle transaction);

//		BOOL WINAPI RemoveDirectoryTransacted(
//		  __in  LPCTSTR lpPathName,
//		  __in  HANDLE hTransaction
//		);
		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError=true)] 
        [return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool RemoveDirectoryTransacted(
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpPathName
			, [In] KtmTransactionHandle transaction);
		
		
		// http://msdn.microsoft.com/en-us/library/aa363853(v=VS.85).aspx
		[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CopyFileTransacted(
			[In] string lpExistingFileName
			, [In] string lpNewFileName
			, [In] IntPtr lpProgressRoutine
			, [In] IntPtr lpData
			, [In, MarshalAs(UnmanagedType.Bool)] ref bool pbCancel
			, [In] CopyFileFlags dwCopyFlags
			, [In] KtmTransactionHandle hTransaction);

//		BOOL WINAPI MoveFileTransacted(
//		  __in      LPCTSTR lpExistingFileName,
//		  __in_opt  LPCTSTR lpNewFileName,
//		  __in_opt  LPPROGRESS_ROUTINE lpProgressRoutine,
//		  __in_opt  LPVOID lpData,
//		  __in      DWORD dwFlags,
//		  __in      HANDLE hTransaction
//		);
		// http://msdn.microsoft.com/en-us/library/aa365241(v=VS.85).aspx
		[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool MoveFileTransacted(
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpExistingFileName
			, [In, MarshalAs(UnmanagedType.LPWStr)] string lpNewFileName
			, [In] IntPtr lpProgressRoutine
			, [In] IntPtr lpData
			, [In] MoveFileFlags dwFlags
			, [In] KtmTransactionHandle hTransaction);
		
// http://msdn.microsoft.com/en-us/library/aa364422(v=vs.85).aspx
//		HANDLE WINAPI FindFirstFileTransacted(
//		  __in        LPCTSTR lpFileName,
//		  __in        FINDEX_INFO_LEVELS fInfoLevelId,
//		  __out       LPVOID lpFindFileData,
//		  __in        FINDEX_SEARCH_OPS fSearchOp,
//		  __reserved  LPVOID lpSearchFilter,
//		  __in        DWORD dwAdditionalFlags,
//		  __in        HANDLE hTransaction
//		);
// see also Wow64DisableWow64FsRedirection
		[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern FindFileHandle FindFirstFileTransacted(
			[In] string lpDirSpec
			, [In] WindowsNative.FINDEX_INFO_LEVELS fInfoLevelId
			, [Out] out WIN32_FIND_DATA lpFindFileData
			, [In] WindowsNative.FINDEX_SEARCH_OPS fSearchOp
			, [In] IntPtr lpSearchFilter
			, [In] int dwAdditionalFlags
			, [In] KtmTransactionHandle hTransaction);

//		http://msdn.microsoft.com/en-us/library/aa364949(v=vs.85).aspx
//		BOOL WINAPI GetFileAttributesTransacted(
//		  __in   LPCTSTR lpFileName,
//		  __in   GET_FILEEX_INFO_LEVELS fInfoLevelId,
//		  __out  LPVOID lpFileInformation,
//		  __in   HANDLE hTransaction
//		);
		[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetFileAttributesTransacted (
			[In] string lpFileName
			, [In] GET_FILEEX_INFO_LEVELS fInfoLevelId
			, [Out] out WIN32_FILE_ATTRIBUTE_DATA attributes
			, [In] KtmTransactionHandle hTransaction);
		
        public static void HandleWindowsError(int error)
        {
            throw new Win32Exception(error);
        }
        
        public static void HandleWindowsError()
        {
        	HandleWindowsError(Marshal.GetLastWin32Error());
        }

		[DllImport("Ktmw32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CommitTransaction(KtmTransactionHandle transaction);
		
		[DllImport("Ktmw32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool RollbackTransaction(KtmTransactionHandle transaction);
		
		[DllImport("Ktmw32.dll")]
		internal static extern IntPtr CreateTransaction(
			IntPtr securityAttributes, IntPtr guid, int options, int isolationLevel, int isolationFlags, int milliSeconds, string description);

	
		internal static Exception GetLastErrorException (string parameterName)
		{
			return GetWindowsErrorException(Marshal.GetLastWin32Error(), parameterName);
		}

		internal static Exception GetWindowsErrorException (int errorCode, string parameterName)
		{
			string message = new Win32Exception(errorCode).Message;

			Exception retval;
			
			if (ERROR_FILE_NOT_FOUND == errorCode)
			{
				retval = new FileNotFoundException (message);
			}
			else if (ERROR_INVALID_DRIVE == errorCode)
			{
				retval = new DriveNotFoundException (message);
			}
			else if (ERROR_ACCESS_DENIED == errorCode)
			{
				retval = new UnauthorizedAccessException (message);
			}
			else if (ERROR_PATH_NOT_FOUND == errorCode)
			{
				retval = new DirectoryNotFoundException (message);
			}
			else if (ERROR_FILE_NAME_TOO_LONG == errorCode)
			{
				retval = new PathTooLongException (message);
			}
			else if (ERROR_INVALID_NAME == errorCode)
			{
				retval = new ArgumentException (message, parameterName);
			}
			else
			{
				retval = new IOException (message, ErrorCodeToHResult(errorCode));
			}

			return retval;
		}

		// Translate managed FileMode member to unmanaged file mode flag.
		internal static WindowsNative.FileMode TranslateFileMode(System.IO.FileMode mode)
		{
			if (mode != System.IO.FileMode.Append)
			{
				return (WindowsNative.FileMode)(int)mode;
			}
			else
			{
				return (WindowsNative.FileMode)(int)System.IO.FileMode.OpenOrCreate;
			}
		}

		// Translate managed FileAcess member to unmanaged file access flag.
		internal static WindowsNative.FileAccess TranslateFileAccess(System.IO.FileAccess access)
		{
			return access == System.IO.FileAccess.Read ?
                WindowsNative.FileAccess.GENERIC_READ
                : WindowsNative.FileAccess.GENERIC_WRITE;
		}

		internal static WindowsNative.EFileAccess GetUnderlyingAccess(System.IO.FileAccess access)
		{
			WindowsNative.EFileAccess retval;
			
            if (access == System.IO.FileAccess.Read)
			{
            	retval = EFileAccess.GenericRead;
			}
			else if (access == System.IO.FileAccess.Write)
			{
             	retval = EFileAccess.GenericWrite;
			}
			else if (access == System.IO.FileAccess.ReadWrite)
			{
				retval = EFileAccess.GenericRead | EFileAccess.GenericWrite;
			}
			else
			{
               	throw new ArgumentOutOfRangeException("access");
            }
			
			return retval;
        }

		// FileShare members map directly to their unmanaged counterparts.
		internal static FileShare TranslateFileShare (System.IO.FileShare share)
		{
			return (FileShare)(int)share;
		}
	}
}

