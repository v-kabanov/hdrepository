//-----------------------------------------------------------------------------
// <created>2/17/2010 10:53:26 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		The interface provides access to data folders for readers and writers.
	/// </summary>
	public interface IHistoricalFoldersExplorer
	{
		/// <summary>
		///		Get the directory tree root
		/// </summary>
		string RootPath
		{ get; }

		/// <summary>
		///		Get the number of levels in the directory tree.
		/// </summary>
		/// <remarks>
		///		The directory tree is balanced - there's always the same number of levels from top (root) to leaf folders (containing data files).
		///		Only the lowest, leaf level folders contain data files.
		/// </remarks>
		int LevelCount
		{ get; }

		/// <summary>
		///		Enumerate descendant data folders in the [sub]tree with root at <see cref="RootPath"/>
		/// </summary>
		/// <param name="level">
		///		Level at which to find data folders
		///		(0 - leaf level
		///		1, 2, ... - higher levels)
		/// </param>
		/// <returns>
		///		List of data file container descriptors containing data items falling
		///		into the specified range (<paramref name="rangeStart"/> - <paramref name="rangeEnd"/>)
		/// </returns>
		List<IRepoFileContainerDescriptor> Enumerate(int level);

		/// <summary>
		///		Enumerate descendant data folders in the [sub]tree with root at <paramref name="root"/>
		/// </summary>
		/// <param name="root">
		///		One of the descendants of <see cref="RootPath"/> among the descendants of which
		///		to search; specify <see langword="null"/> to search in <see cref="RootPath"/>
		/// </param>
		/// <param name="level">
		///		Level at which to find data folders
		///		(0 - leaf level
		///		1, 2, ... - higher levels)
		/// </param>
		/// <returns>
		///		List of data file container descriptors containing data items falling
		///		into the specified range (<paramref name="rangeStart"/> - <paramref name="rangeEnd"/>)
		/// </returns>
		List<IRepoFileContainerDescriptor> Enumerate(IRepoFileContainerDescriptor root, int level);

		/// <summary>
		///		Get folder for the data item with the specified timestamp
		/// </summary>
		/// <param name="timestamp">
		///		data item timestamp, UTC
		/// </param>
		/// <param name="level">
		///		folder level:
		///			0 - leaf level (contains files)
		///			1.. - upper levels
		/// </param>
		/// <returns>
		/// </returns>
		IRepoFileContainerDescriptor GetTargetFolder(DateTime timestamp, int level);

		/// <summary>
		///		Get the start of the date-time range covered by data folder owning <paramref name="dataItemTimestamp"/>
		///		(inclusive)
		/// </summary>
		/// <param name="dataItemTimestamp">
		///		Timestamp of a data item belonging to the data folder to find the start of date-time range of
		/// </param>
		/// <param name="level">
		///		The level of the data folder
		///			0 - leaf level (contains files)
		///			1.. - upper levels
		/// </param>
		/// <returns>
		///		<see cref="DateTime"/>, inclusive
		/// </returns>
		DateTime GetRangeStart(DateTime dataItemTimestamp, int level);

		/// <summary>
		///		Get the end of the date-time range covered by data folder owning <paramref name="dataItemTimestamp"/>
		///		(exclusive)
		/// </summary>
		/// <param name="dataItemTimestamp">
		///		Timestamp of a data item belonging to the data folder to find the end of date-time range of
		/// </param>
		/// <param name="level">
		///		The level of the data folder
		///			0 - leaf level (contains files)
		///			1.. - upper levels
		/// </param>
		/// <returns>
		///		<see cref="DateTime"/>, exclusive
		/// </returns>
		DateTime GetRangeEnd(DateTime dataItemTimestamp, int level);
	}
}
