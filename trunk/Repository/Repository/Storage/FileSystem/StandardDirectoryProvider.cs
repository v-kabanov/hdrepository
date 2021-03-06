﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage.FileSystem
{
	/// <summary>
	///		Standard implementation of <see cref="IDirectoryProvider"/> using built-in .NET framework features.
	/// </summary>
	public class StandardDirectoryProvider : IDirectoryProvider
	{
		/// <summary>
		///     Determines whether the given path refers to an existing directory on disk.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path to check.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="path"/> refers to an existing directory; 
		///     otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		///     Note that this method will return false if any error occurs while trying to determine 
		///     if the specified directory exists. This includes situations that would normally result in 
		///     thrown exceptions including (but not limited to); passing in a directory name with invalid 
		///     or too many characters, an I/O error such as a failing or missing disk, or if the caller
		///     does not have Windows or Code Access Security (CAS) permissions to to read the directory.
		/// </remarks>
		public bool Exists(string path)
		{
			return Directory.Exists(path);
		}

		/// <summary>
		///     Returns a enumerable containing the directory names of the specified directory.
		///     Non-recursive, only top level directories.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the directory names within <paramref name="path"/>.
		/// </returns>
		public IEnumerable<string> EnumerateDirectories(string path)
		{
			return Directory.GetDirectories(path);
		}

		/// <summary>
		///     Returns a enumerable containing the directory names of the specified directory that 
		///     match the specified search pattern. Non-recursive.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <param name="searchPattern">
		///     A <see cref="String"/> containing search pattern to match against the names of the 
		///     directories in <paramref name="path"/>, otherwise, <see langword="null"/> or an empty 
		///     string ("") to use the default search pattern, "*".
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the directory names within <paramref name="path"/>
		///     that match <paramref name="searchPattern"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white 
		///     space, or contains one or more invalid characters as defined in 
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based 
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length. 
		///     For example, on Windows-based platforms, paths must not exceed 
		///     255 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public IEnumerable<string> EnumerateDirectories(string directoryPath, string searchPattern)
		{
			return Directory.GetDirectories(directoryPath, searchPattern);
		}

		/// <summary>
		///     Returns a enumerable containing the file names of the specified directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the file names within <paramref name="path"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white 
		///     space, or contains one or more invalid characters as defined in 
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based 
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length. 
		///     For example, on Windows-based platforms, paths must not exceed 
		///     255 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public IEnumerable<string> EnumerateFiles(string path)
		{
			return Directory.GetFiles(path);
		}

		/// <summary>
		///     Returns a enumerable containing the file names of the specified directory that 
		///     match the specified search pattern.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <param name="searchPattern">
		///     A <see cref="String"/> containing search pattern to match against the names of the 
		///     files in <paramref name="path"/>, otherwise, <see langword="null"/> or an empty 
		///     string ("") to use the default search pattern, "*".
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the file names within <paramref name="path"/>
		///     that match <paramref name="searchPattern"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white 
		///     space, or contains one or more invalid characters as defined in 
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based 
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length. 
		///     For example, on Windows-based platforms, paths must not exceed 
		///     255 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
		{
			return Directory.GetFiles(path, searchPattern);
		}

		/// <summary>
		///     Creates the specified directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to create.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white 
		///     space, or contains one or more invalid characters as defined in 
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based 
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length. 
		///     For example, on Windows-based platforms, paths must not exceed 
		///     255 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		/// <remarks>
		///     Note: Unlike <see cref="Directory.CreateDirectory(System.String)"/>, this method only creates 
		///     the last directory in <paramref name="path"/>.
		/// </remarks>
		public void Create(string path)
		{
			Directory.CreateDirectory(path);
		}

		/// <summary>
		///     Deletes the specified empty directory.
		/// </summary>
		/// <param name="path">
		///      A <see cref="String"/> containing the path of the directory to delete.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white 
		///     space, or contains one or more invalid characters as defined in 
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based 
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length. 
		///     For example, on Windows-based platforms, paths must not exceed 
		///     255 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a directory that is read-only.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a directory that is not empty.
		///     <para>
		///         -or-    
		///     </para>
		///     <paramref name="path"/> refers to a directory that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public void Delete(string path)
		{
			Directory.Delete(path);
		}

		/// <summary>
		///     Moves the specified directory to a new location.
		/// </summary>
		/// <param name="sourcePath">
		///     A <see cref="String"/> containing the path of the directory to move.
		/// </param>
		/// <param name="destinationPath">
		///     A <see cref="String"/> containing the new path of the directory.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> is 
		///     <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> is 
		///     an empty string (""), contains only white space, or contains one or more 
		///     invalid characters as defined in <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> 
		///     contains one or more components that exceed the drive-defined maximum length. 
		///     For example, on Windows-based platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> 
		///     exceeds the system-defined maximum length. For example, on Windows-based platforms, 
		///     paths must not exceed 255 characters.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		///     <paramref name="sourcePath"/> could not be found.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     One or more directories in <paramref name="sourcePath"/> and/or 
		///     <paramref name="destinationPath"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="destinationPath"/> refers to a directory or file that already exists.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> is a 
		///     directory.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="sourcePath"/> refers to a file that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> specifies 
		///     a device that is not ready.
		/// </exception>
		public void Move(string oldPath, string newPath)
		{
			Directory.Move(oldPath, newPath);
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
			Directory.Delete(path, recursive);
		}

		/// <summary>
		///		Get max length of directory path as supported by the provider
		///		(requirements of repository itself are not counted)
		/// </summary>
		public int MaxDirectoryPathLengh
		{
			get
			{
				if (Util.SystemInfo.IsUnix)
				{
					// http://serverfault.com/questions/9546/filename-length-limits-on-linux
					// http://stackoverflow.com/questions/833291/is-there-an-equivalent-to-winapis-max-path-under-linux-unix
					return 4096 - 8;
				}
				else if (Util.SystemInfo.IsMacOS)
				{
					return 1024;
				}

				// restriction in .NET framework up to and including 4.0
				return 260 - 8;
			}
		}

		/// <summary>
		///		Get max length of a single component of file system path.
		///		On windows it's 255
		/// </summary>
		public int MaxPathElementLength
		{
			get
			{
				// on unix and mac limit is same as for whole path
				if (Util.SystemInfo.IsUnix)
				{
					// http://serverfault.com/questions/9546/filename-length-limits-on-linux
					// http://stackoverflow.com/questions/833291/is-there-an-equivalent-to-winapis-max-path-under-linux-unix
					return 4096 - 8;
				}
				else if (Util.SystemInfo.IsMacOS)
				{
					return 1024;
				}

				// restriction in .NET framework up to and including 4.0
				return 255;
			}
		}

	}
}
