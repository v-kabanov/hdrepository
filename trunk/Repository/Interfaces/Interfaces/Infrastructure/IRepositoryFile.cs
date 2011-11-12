using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		Interface of an object representing a data file in a repository.
	/// </summary>
	/// <remarks>
	///		This interface adds location awareness to the <see cref="IRepositoryFileName"/>.
	/// </remarks>
	public interface IRepositoryFile
	{
		/// <summary>
		///		Get or set file name object.
		/// </summary>
		IRepositoryFileName Name { get; }

		/// <summary>
		///		Get containing data folder object.
		/// </summary>
		IDataFolder ContainingFolder { get; }

		/// <summary>
		///		Get full path to the file.
		/// </summary>
		string Path { get; }

		/// <summary>
		///		Whether the file exists on disk.
		/// </summary>
		bool Exists { get; }

		/// <summary>
		///		Get next data file in the same repo folder.
		/// </summary>
		/// <param name="backwards">
		///		The direction in which to look for data file relative to this file: to the past or to the future
		/// </param>
		/// <returns>
		///		Next data file or <see langword="null"/> if none exists
		/// </returns>
		IRepositoryFile GetNext(bool backwards);

		/// <summary>
		///		Delete the file from repository and disk.
		/// </summary>
		/// <remarks>
		///		Deletes from disk and then notifies the containing folder.
		/// </remarks>
		void Delete();

		/// <summary>
		///		Copy file to the specified location.
		/// </summary>
		/// <param name="newPath">
		///		New file path.
		/// </param>
		/// <param name="overwrite">
		///		Whether to overwrite existing file if any.
		/// </param>
		void Copy(string newPath, bool overwrite);
	}
}
