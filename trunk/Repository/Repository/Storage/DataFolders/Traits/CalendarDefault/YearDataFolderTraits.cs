using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;
using System.Text.RegularExpressions;
using bfs.Repository.Util;
using bfs.Repository.Interfaces;

namespace bfs.Repository.Storage.DataFolders.Traits.CalendarDefault
{
	internal class YearDataFolderTraits : ICalendarHistoricalFolderTraits
	{
		internal const string dirNamePattern = @"^\d{4}$";
		internal const string dirNameToStringFormatYear = "D4";
		internal const string dirNameMask = "????";


		private static readonly Regex _folderNameRegex = new Regex(dirNamePattern, RegexOptions.CultureInvariant);

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
		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(DateTime dateTime)
		{
			int year = dateTime.Year;
			return new CalendarHistoricalFolderInternalDescriptor()
			{
				Start = new DateTime(year, 1, 1),
				End = new DateTime(year + 1, 1, 1),
				Name = GetFolderName(year)
			};
		}

		/// <summary>
		///		Get historical data foledr name by data timestamp.
		/// </summary>
		/// <param name="dateTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		The name of the data folder to contain the <paramref name="dateTime"/> (no path).
		/// </returns>
		public string GetFolderName(DateTime dateTime)
		{
			return GetFolderName(dateTime.Year);
		}

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
		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(CalendarHistoricalFolderInternalDescriptor parent, string folderName)
		{
			Check.Require(parent == null, "Year must be top level folder");
			return GetDescriptor(folderName);
		}

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
		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(IRepoFileContainerDescriptor parent, string folderName)
		{
			Check.Require(parent == null, "Year must be top level folder");
			return GetDescriptor(folderName);
		}

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
		public DateTime GetRangeStart(DateTime dataItemTimestamp)
		{
			return new DateTime(dataItemTimestamp.Year, 1, 1);
		}

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
		public DateTime GetRangeEnd(DateTime dataItemTimestamp)
		{
			return new DateTime(dataItemTimestamp.Year + 1, 1, 1);
		}

		/// <summary>
		///		Get folder name mask for searching
		/// </summary>
		public string NameMask
		{
			get { return dirNameMask; }
		}

		//---------------------------------------------------------------------------------

		/// <summary>
		///		Returns <see langword="null"/> if folder name is invalid
		/// </summary>
		/// <param name="folderName">
		///		4-digit year string representing a positive integer
		/// </param>
		/// <returns>
		/// </returns>
		private CalendarHistoricalFolderInternalDescriptor GetDescriptor(string folderName)
		{
			CalendarHistoricalFolderInternalDescriptor retval = null;
			if (_folderNameRegex.IsMatch(folderName))
			{
				int year = int.Parse(folderName);

				if (year > 0)
				{
					retval = GetDescriptor(year);
				}
			}
			return retval;
		}

		private CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year)
		{
			return new CalendarHistoricalFolderInternalDescriptor()
			{
				Start = new DateTime(year, 1, 1),
				End = new DateTime(year + 1, 1, 1),
				Name = GetFolderName(year)
			};
		}

		private string GetFolderName(int year)
		{
			return year.ToString(dirNameToStringFormatYear);
		}
	}
}
