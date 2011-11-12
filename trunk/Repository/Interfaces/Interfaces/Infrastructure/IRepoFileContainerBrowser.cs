//-----------------------------------------------------------------------------
// <created>2/18/2010 9:37:54 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		The interface for browsing leaf data folders.
	/// </summary>
	public interface IRepoFileContainerBrowser : IRepoFileChangeListener
	{
		/// <summary>
		///		Get number of contained data files.
		/// </summary>
		int FileCount
		{ get; }

		/// <summary>
		///		Get chronologically first data file.
		/// </summary>
		/// <remarks>
		///		Returns null when the container is empty.
		/// </remarks>
		IRepositoryFileName FirstFile
		{ get; }

		/// <summary>
		///		Get chronologically last data file.
		/// </summary>
		/// <remarks>
		///		Returns null when the container is empty.
		/// </remarks>
		IRepositoryFileName LastFile
		{ get; }

		/// <summary>
		///		Get read-only collection of contained files.
		/// </summary>
		Util.IReadOnlyCollection<IRepositoryFileName> Files
		{ get; }

		/// <summary>
		///		Get full path to the target directory.
		/// </summary>
		string FullPath
		{ get; }

		/// <summary>
		///		Re-read the contents of the data folder.
		/// </summary>
		void Refresh();

		/// <summary>
		///		Get first file with data timestamped starting from the specified date (inclusive)
		/// </summary>
		/// <param name="dataTimestampFrom">
		///		Start of date-time range to find
		/// </param>
		/// <param name="backwards">
		///		To which side of <paramref name="dataTimestampFrom"/> to look for data if there's no
		///		file containing exactly that timestamp
		/// </param>
		/// <returns>
		///		<see langword="null"/> if no file exists in the container with data in the specified date-time range
		///		otherwise first file (from <paramref name="dataTimestampFrom"/>) with the required data.
		/// </returns>
		IRepositoryFileName GetFile(DateTime dataTimestampFrom, bool backwards);

		/// <summary>
		///		Get contained data file by its key - first timestamp
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		First data item timestamp
		/// </param>
		/// <returns>
		///		<see langword="null"/> file with the specified key not found
		///		<code>IRepositoryFileName</code> so that <code>IRepositoryFileName.Start</code> equals <paramref name="firstItemTimestamp"/>
		/// </returns>
		IRepositoryFileName GetFile(DateTime firstItemTimestamp);

		/// <summary>
		///		Get the sequence of files containing data items in the specified datetime range.
		/// </summary>
		/// <param name="rangeStart">
		///		Selection range start  (inclusive if less than <paramref name="rangeEnd"/> and exclusive otherwise)
		/// </param>
		/// <param name="rangeEnd">
		///		Selection range end (inclusive if less than <paramref name="rangeStart"/> and exclusive otherwise)
		/// </param>
		/// <returns>
		///		Enumerable containing all the files which contain data items in the specified datetime range
		/// </returns>
		Util.IDirectedEnumerable<IRepositoryFileName> SelectSequence(DateTime rangeStart, DateTime rangeEnd);

		/// <summary>
		///		Get the sequence of files containing data in the specified datetime range.
		/// </summary>
		/// <param name="startFrom">
		///		Selection range start  (inclusive if <paramref name="backwards"/> is <see langword="false"/> and exclusive otherwise)
		/// </param>
		/// <param name="backwards">
		///		Direction of the sequence: ascending if <see langword="false"/> and descending otherwise
		/// </param>
		/// <returns>
		///		Enumerable containing all the files which contain data items in the specified datetime range
		/// </returns>
		Util.IDirectedEnumerable<IRepositoryFileName> SelectSequence(DateTime startFrom, bool backwards);

		/// <summary>
		///		Get data files around the specified item timestamp
		/// </summary>
		/// <param name="itemTimestamp">
		///		The data item timestamp
		/// </param>
		/// <param name="predecessor">
		///		The file if any which ends before the specified timestamp
		/// </param>
		/// <param name="ownerFile">
		///		The file covering the specified item timestamp
		/// </param>
		/// <param name="successor">
		///		The file which starts after the specified timestamp
		/// </param>
		void GetDataFiles(DateTime itemTimestamp
			, out IRepositoryFileName predecessor
			, out IRepositoryFileName ownerFile
			, out IRepositoryFileName successor);
	}
}
