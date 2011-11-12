//-----------------------------------------------------------------------------
// <created>3/5/2010 2:34:59 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		Interface of a object which wants to listen to changes in repository data file container[s].
	/// </summary>
	/// <remarks>
	///		Files in a container must not overlap. Therefore they can be identified
	///		by the first item timestamp which is specified in the file name
	///		(<see cref="IRepositoryFileName"/>).
	/// </remarks>
	public interface IRepoFileChangeListener
	{
		/// <summary>
		///		Perform necessary operations after a new file has been added to the container.
		/// </summary>
		/// <param name="newRepoFile">
		///		New file added to the container
		/// </param>
		/// <exception cref="OverlappingFileInContainer">
		///		The <paramref name="newRepoFile"/> overlaps with an existing file; possible concurrency issue or internal error.
		/// </exception>
		void FileAdded(IRepositoryFileName newRepoFile);

		/// <summary>
		///		Notify container that a file was changed
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		The timestamp of the file as it has been known to the container
		///		(the value before the change happened)
		/// </param>
		/// <param name="newFileName">
		///		New file name
		/// </param>
		/// <exception cref="FileContainerNotificationException">
		///		The file cannot be found in the container; possible concurrency issue
		/// </exception>
		/// <exception cref="OverlappingFileInContainer">
		///		The <paramref name="newRepoFile"/> overlaps with an existing file; possible concurrency issue or internal error.
		/// </exception>
		void FileChanged(DateTime firstItemTimestamp, IRepositoryFileName newFileName);

		/// <summary>
		///		Notify container that a file was deleted from the container
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		The timestamp of the first item in the file as it has been known to the container (the value before deletion)
		/// </param>
		/// <exception cref="FileContainerNotificationException">
		///		The file cannot be found in the container; possible concurrency issue
		/// </exception>
		void FileDeleted(DateTime firstItemTimestamp);
	}
}
