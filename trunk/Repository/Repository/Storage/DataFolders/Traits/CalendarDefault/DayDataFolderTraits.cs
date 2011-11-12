using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;
using System.Text.RegularExpressions;
using bfs.Repository.Util;
using bfs.Repository.Interfaces;
using log4net;

namespace bfs.Repository.Storage.DataFolders.Traits.CalendarDefault
{
	internal class DayDataFolderTraits : ICalendarHistoricalFolderTraits
	{
		private static readonly ILog _log = LogManager.GetLogger("DayDataFolderTraits");

		internal const string dirNamePattern = @"^\d{2}$";
		internal const string dirNameToStringFormat = "D2";
		internal const string dirNameMask = "??";

		private static readonly Regex _folderNameRegex = new Regex(dirNamePattern, RegexOptions.CultureInvariant);

		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(DateTime dateTime)
		{
			return GetDescriptor(dateTime.Year, dateTime.Month, dateTime.Day);
		}

		public string GetFolderName(DateTime dateTime)
		{
			return GetFolderName(dateTime.Day);
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
			Check.RequireArgumentNotNull(parent, "parent");
			Check.Require(parent.Start == new DateTime(parent.Start.Year, parent.Start.Month, 1)
				&& parent.End.AddMonths(-1) == parent.Start, "Parent of day must be month");
			return GetDescriptor(parent.Start.Year, parent.Start.Month, folderName);
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
			Check.RequireArgumentNotNull(parent, "parent");
			Check.Require(parent.Start == new DateTime(parent.Start.Year, parent.Start.Month, 1)
				&& parent.End.AddMonths(-1) == parent.Start, "Parent of day must be month");
			return GetDescriptor(parent.Start.Year, parent.Start.Month, folderName);
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
			return GetRangeStartImpl(dataItemTimestamp);
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
			return GetRangeEndImpl(dataItemTimestamp);
		}

		/// <summary>
		///		Get folder name mask for searching
		/// </summary>
		public string NameMask
		{
			get { return dirNameMask; }
		}

		//---------------------------------------------------------------------------------

		/// <returns>
		///		<see langword="null"/> if folder name is invalid
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		year is less than 1 or greater than 9999.  -or- month is less than 1 or greater
		///		than 12.  -or- day is less than 1 or greater than the number of days in month.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		///		The specified parameters evaluate to less than System.DateTime.MinValue or
		///		more than System.DateTime.MaxValue.
		/// </exception>
		private static CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year, int month, string folderName)
		{
			CalendarHistoricalFolderInternalDescriptor retval = null;
			if (_folderNameRegex.IsMatch(folderName))
			{
				int day = int.Parse(folderName);

				if (day < 32 && day > 0)
				{
					try
					{
						retval = GetDescriptor(year, month, day);
					}
					catch (ArgumentException)
					{
						_log.ErrorFormat("Data folder name invalid: {0}", folderName);
					}
				}
			}
			return retval;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="year">
		///		The year (1 through 9999).
		/// </param>
		/// <param name="month">
		///		The month (1 through 12).
		/// </param>
		/// <param name="day">
		///		The day (1 through 31)
		/// </param>
		/// <returns>
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		year is less than 1 or greater than 9999.  -or- month is less than 1 or greater
		///		than 12.  -or- day is less than 1 or greater than the number of days in month.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		///		The specified parameters evaluate to less than System.DateTime.MinValue or
		///		more than System.DateTime.MaxValue.
		/// </exception>
		private static CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year, int month, int day)
		{
			DateTime start = new DateTime(year, month, day);

			return new CalendarHistoricalFolderInternalDescriptor()
			{
				Start = start,
				End = start.AddDays(1),
				Name = GetFolderName(day)
			};
		}

		public static string GetFolderName(int day)
		{
			Check.Require(day >= 0 && day < 32);
			return day.ToString(dirNameToStringFormat);
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
		public static DateTime GetRangeStartImpl(DateTime dataItemTimestamp)
		{
			return dataItemTimestamp.Date;
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
		public static DateTime GetRangeEndImpl(DateTime dataItemTimestamp)
		{
			return dataItemTimestamp.Date.AddDays(1);
		}

	}
}
