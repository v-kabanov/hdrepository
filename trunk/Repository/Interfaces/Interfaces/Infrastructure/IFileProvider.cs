using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces.Infrastructure
{
	public interface IFileProvider
	{
		/// <summary>
		///     Returns a value indicating whether the specified path refers to an existing file.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path to check.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="path"/> refers to an existing file; 
		///     otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		///     Note that this method will return false if any error occurs while trying to determine 
		///     if the specified file exists. This includes situations that would normally result in 
		///     thrown exceptions including (but not limited to); passing in a file name with invalid 
		///     or too many characters, an I/O error such as a failing or missing disk, or if the caller
		///     does not have Windows or Code Access Security (CAS) permissions to read the file.
		/// </remarks>
		bool Exists(string path);

		/// <summary>
		///     Deletes the specified file.
		/// </summary>
		/// <param name="path">
		///      A <see cref="String"/> containing the path of the file to delete.
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
		/// <exception cref="FileNotFoundException">
		///     <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     One or more directories in <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a file that is read-only.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> is a directory.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> refers to a file that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		void Delete(string path);

		/// <summary>
		///     Moves the specified file to a new location.
		/// </summary>
		/// <param name="sourcePath">
		///     A <see cref="String"/> containing the path of the file to move.
		/// </param>
		/// <param name="destinationPath">
		///     A <see cref="String"/> containing the new path of the file.
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
		///     paths must not exceed 32,000 characters.
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
		///     <paramref name="destinationPath"/> refers to a file that already exists.
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
		void Move(string sourcePath, string destinationPath);

		/// <summary>
		///     Copies the specified file to a specified new file, indicating whether to overwrite an existing file.
		/// </summary>
		/// <param name="sourcePath">
		///     A <see cref="String"/> containing the path of the file to copy.
		/// </param>
		/// <param name="destinationPath">
		///     A <see cref="String"/> containing the new path of the file.
		/// </param>
		/// <param name="overwrite">
		///     <see langword="true"/> if <paramref name="destinationPath"/> should be overwritten 
		///     if it refers to an existing file, otherwise, <see langword="false"/>.
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
		///     paths must not exceed 32,000 characters.
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
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="overwrite"/> is true and <paramref name="destinationPath"/> refers to a 
		///     file that is read-only.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="overwrite"/> is false and <paramref name="destinationPath"/> refers to 
		///     a file that already exists.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="sourcePath"/> and/or <paramref name="destinationPath"/> is a 
		///     directory.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="overwrite"/> is true and <paramref name="destinationPath"/> refers to 
		///     a file that already exists and is in use.
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
		void Copy(string sourcePath, string destinationPath, bool overwrite);

		/// <summary>
		///     Opens the specified file.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the file to open.
		/// </param>
		/// <param name="access">
		///     One of the <see cref="FileAccess"/> value that specifies the operations that can be 
		///     performed on the file. 
		/// </param>
		/// <param name="mode">
		///     One of the <see cref="FileMode"/> values that specifies whether a file is created
		///     if one does not exist, and determines whether the contents of existing files are 
		///     retained or overwritten.
		/// </param>
		/// <returns>
		///     A <see cref="FileStream"/> that provides access to the file specified in 
		///     <paramref name="path"/>.
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
		///     One or more directories in <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a file that is read-only and <paramref name="access"/>
		///     is not <see cref="FileAccess.Read"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> is a directory.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> refers to a file that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		FileStream Open(string path, FileMode mode, FileAccess access);

		/// <summary>
		///     Opens the specified file.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the file to open.
		/// </param>
		/// <param name="access">
		///     One of the <see cref="FileAccess"/> value that specifies the operations that can be 
		///     performed on the file. 
		/// </param>
		/// <param name="mode">
		///     One of the <see cref="FileMode"/> values that specifies whether a file is created
		///     if one does not exist, and determines whether the contents of existing files are 
		///     retained or overwritten.
		/// </param>
		/// <param name="share">
		///     One of the <see cref="FileShare"/> values specifying the type of access other threads 
		///     have to the file. 
		/// </param>
		/// <returns>
		///     A <see cref="FileStream"/> that provides access to the file specified in 
		///     <paramref name="path"/>.
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
		///     One or more directories in <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a file that is read-only and <paramref name="access"/>
		///     is not <see cref="FileAccess.Read"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> is a directory.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> refers to a file that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		FileStream Open(string path, FileMode mode, FileAccess access, FileShare share);

		/// <summary>
		///     Opens the specified file.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the file to open.
		/// </param>
		/// <param name="access">
		///     One of the <see cref="FileAccess"/> value that specifies the operations that can be 
		///     performed on the file. 
		/// </param>
		/// <param name="mode">
		///     One of the <see cref="FileMode"/> values that specifies whether a file is created
		///     if one does not exist, and determines whether the contents of existing files are 
		///     retained or overwritten.
		/// </param>
		/// <param name="share">
		///     One of the <see cref="FileShare"/> values specifying the type of access other threads 
		///     have to the file. 
		/// </param>
		/// <param name="bufferSize">
		///     An <see cref="Int32"/> containing the number of bytes to buffer for reads and writes
		///     to the file, or 0 to specified the default buffer size, 1024.
		/// </param>
		/// <param name="options">
		///     One or more of the <see cref="FileOptions"/> values that describes how to create or 
		///     overwrite the file.
		/// </param>
		/// <returns>
		///     A <see cref="FileStream"/> that provides access to the file specified in 
		///     <paramref name="path"/>.
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
		/// <exception cref="ArgumentOutOfRangeException">
		///     <paramref name="bufferSize"/> is less than 0.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length. 
		///     For example, on Windows-based platforms, paths must not exceed 
		///     32,000 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		///     One or more directories in <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a file that is read-only and <paramref name="access"/>
		///     is not <see cref="FileAccess.Read"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> is a directory.
		/// </exception>
		/// <exception cref="IOException">
		///     <paramref name="path"/> refers to a file that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		FileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options);
	}
}
