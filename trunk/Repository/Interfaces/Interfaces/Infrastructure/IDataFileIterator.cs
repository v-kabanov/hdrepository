using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		The interface describes iterating over data files in a repository folder;
	/// </summary>
	/// <remarks>
	///		Objects implementing this interface are created in <see cref="IObjectFactory.GetDataFileIterator(IRepositoryFolder, bool)"/>
	/// </remarks>
	/// <example>
	///		Iterating over all data files in the repository folder with relative path "/relativePath/relativePath" containing
	///		data with timestamp within the last 24 hours inclusive.
	///		<code>
	///		IRepository repository;
	///		//...
	///		var iterator = repository.ObjectFactory.GetDataFileIterator(repository.RootFolder.GetDescendant("relativePath/relativePath", false), false);
	///		iterator.Seek(DateTime.Now.AddDays(-1));
	///		while (iterator.Current != null)
	///		{
	///			Console.WriteLine(iterator.Current.Path);
	///			iterator.MoveNext();
	///		}
	///		</code>
	/// </example>
	public interface IDataFileIterator
	{
		/// <summary>
		///		Get target repository folder.
		/// </summary>
		IFolder Folder
		{ get; }

		/// <summary>
		///		Get target repository instance.
		/// </summary>
		IRepository Repository
		{ get; }

		/// <summary>
		///		Initialise iteration position
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp
		/// </param>
		/// <param name="backwards">
		///		Direction in which to search for data files if the <paramref name="seekTime"/> is not covered by any
		///		existing data file
		/// </param>
		/// <returns>
		///		First found data file; null if none found.
		/// </returns>
		IRepositoryFile Seek(DateTime seekTime, bool backwards);

		/// <summary>
		///		Initialise iteration position using default direction (<see cref="Backwards"/>)
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		First found data file; null if none found.
		/// </returns>
		IRepositoryFile Seek(DateTime seekTime);

		/// <summary>
		///		Get or set the default direction. When using <see cref="MoveNext(System.Boolean)"/> the setting is ignored.
		/// </summary>
		bool Backwards { get; set; }

		/// <summary>
		///		Get the current file.
		/// </summary>
		IRepositoryFile Current { get; }

		/// <summary>
		///		Get next file according to <see cref="Backwards"/> setting
		/// </summary>
		IRepositoryFile Next { get; }

		/// <summary>
		///		Get previous file according to <see cref="Backwards"/> setting
		/// </summary>
		IRepositoryFile Previous { get; }

		/// <summary>
		///		Get the next newest data file
		/// </summary>
		IRepositoryFile NextForward { get; }

		/// <summary>
		///		Get the next oldest data file
		/// </summary>
		IRepositoryFile NextBackwards { get; }

		/// <summary>
		///		Move current file to the next file.
		/// </summary>
		/// <param name="backwards">
		///		The direction in which to move
		/// </param>
		/// <returns>
		///		New current file (either <see cref="NextForward"/> or <see cref="NextBackwards"/> prior to the call to this method)
		/// </returns>
		/// <remarks>
		///		Does nothing if <see cref="Current"/> is already <see langword="null"/>. Otherwise the iterator will not be able to go back.
		/// </remarks>
		IRepositoryFile MoveNext(bool backwards);

		/// <summary>
		///		Move current file to the next file in the default direction (<see cref="Backwards"/>).
		/// </summary>
		/// <returns>
		///		New current file (either <see cref="NextForward"/> or <see cref="NextBackwards"/> prior to the call to this method)
		/// </returns>
		/// <remarks>
		///		Does nothing if <see cref="Current"/> is already <see langword="null"/>. Otherwise the iterator will not be able to go back.
		/// </remarks>
		IRepositoryFile MoveNext();
	}
}
