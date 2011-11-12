using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	public interface IDirectoryProvider
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
		bool Exists(string path);

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
		IEnumerable<string> EnumerateDirectories(string path);

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
		///     32,000 characters.
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
		IEnumerable<string> EnumerateDirectories(string directoryPath, string searchPattern);

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
		///     32,000 characters.
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
		IEnumerable<string> EnumerateFiles(string path);

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
		///     32,000 characters.
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
		IEnumerable<string> EnumerateFiles(string path, string searchPattern);

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
		///     32,000 characters.
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
		void Create(string path);

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
		///     32,000 characters.
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
		void Delete(string path);

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
		///     paths must not exceed 32,000 or 255 characters (depending on API used).
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
		void Move(string oldPath, string newPath);

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
		string GetLastPathComponent(string path);

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
		void Delete(string path, bool recursive);

		/// <summary>
		///		Get max length of directory path as supported by the provider
		///		(requirements of repository itself are not counted)
		/// </summary>
		int MaxDirectoryPathLengh
		{ get; }

		/// <summary>
		///		Get max length of a single component of file system path.
		///		On windows it's 255
		/// </summary>
		int MaxPathElementLength
		{ get; }
	}
}
