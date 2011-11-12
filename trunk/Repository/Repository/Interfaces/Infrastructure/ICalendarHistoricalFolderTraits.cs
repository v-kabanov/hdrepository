using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Storage;
using bfs.Repository.Storage.DataFolders.Traits.CalendarDefault;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		The interface describing built-in simple calendar-based data folders tree.
	/// </summary>
	/// <remarks>
	///		The tree will guarantee unambiguity by always including the year level.
	///		No attempt is made to enforce a particular order of tree levels inside the traits classes / through the interface. This is because
	///		the interface and implementing classes are internal and used by the default folders explorer implementation which does enforce
	///		the valid collection and order of levels. Development of universal and reusable self-sufficient classes of folder traits seems
	///		problematic and of limited value at this point, bearing in mind the objective of enforcing unambiguous history tree. Even if the data stream
	///		is so strong/fast as to warrant extending the tree down to minutes level, having the year level at the top (and month, day, hour in
	///		between) seems to present no major problem, but more likely be important anyway. Therefeore the default implementation will
	///		allow to vary only the depth of the tree with minimum lowest level of month and maximum, say, minute.
	/// </remarks>
	internal interface ICalendarHistoricalFolderTraits
	{
		/// <summary>
		///		Get historical data folder descriptor by data timestamp.
		/// </summary>
		/// <param name="dateTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		The descriptor (<see cref="CalendarHistoricalFolderInternalDescriptor"/>) of the data folder to contain
		///		the <paramref name="dateTime"/>.
		/// </returns>
		CalendarHistoricalFolderInternalDescriptor GetDescriptor(DateTime dateTime);

		/// <summary>
		///		Get historical data foledr name by data timestamp.
		/// </summary>
		/// <param name="dateTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		The name of the data folder to contain the <paramref name="dateTime"/> (no path).
		/// </returns>
		string GetFolderName(DateTime dateTime);

		/// <summary>
		///		Get descriptor of an [existing] folder by its name and parent folder.
		/// </summary>
		/// <param name="parent">
		///		Descriptor of a parent folder
		/// </param>
		/// <param name="folderName">
		///		The folder name
		/// </param>
		/// <returns>
		///		<see langword="null"/> if the folder is not a valid historical data folder.
		///		<see cref="CalendarHistoricalFolderInternalDescriptor"/> instance otherwise
		/// </returns>
		CalendarHistoricalFolderInternalDescriptor GetDescriptor(
			CalendarHistoricalFolderInternalDescriptor parent
			, string folderName);

		/// <summary>
		///		Get descriptor of an [existing] folder by its name and parent folder.
		/// </summary>
		/// <param name="parent">
		///		Descriptor of a parent folder
		/// </param>
		/// <param name="folderName">
		///		The folder name
		/// </param>
		/// <returns>
		///		<see langword="null"/> if the folder is not a valid historical data folder.
		///		<see cref="CalendarHistoricalFolderInternalDescriptor"/> instance otherwise
		/// </returns>
		CalendarHistoricalFolderInternalDescriptor GetDescriptor(
			IRepoFileContainerDescriptor parent
			, string folderName);


		/// <summary>
		///		Get the start of the date-time range covered by data folder owning <paramref name="dataItemTimestamp"/>
		///		(inclusive)
		/// </summary>
		/// <param name="dataItemTimestamp">
		///		Timestamp of a data item belonging to the data folder to find the start of date-time range of
		/// </param>
		/// <returns>
		///		<see cref="DateTime"/>, inclusive
		/// </returns>
		DateTime GetRangeStart(DateTime dataItemTimestamp);

		/// <summary>
		///		Get the end of the date-time range covered by data folder owning <paramref name="dataItemTimestamp"/>
		///		(exclusive)
		/// </summary>
		/// <param name="dataItemTimestamp">
		///		Timestamp of a data item belonging to the data folder to find the end of date-time range of
		/// </param>
		/// <returns>
		///		<see cref="DateTime"/>, exclusive
		/// </returns>
		DateTime GetRangeEnd(DateTime dataItemTimestamp);

		/// <summary>
		///		Get folder name mask for searching
		/// </summary>
		string NameMask
		{ get; }
	}
}
