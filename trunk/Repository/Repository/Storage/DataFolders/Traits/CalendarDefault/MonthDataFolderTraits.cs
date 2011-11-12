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
	internal class MonthDataFolderTraits : ICalendarHistoricalFolderTraits
	{
		internal const string dirNamePattern = @"^\d{2}$";
		internal const string dirNameToStringFormat = "D2";
		internal const string dirNameMask = "??";

		private static readonly Regex _folderNameRegex = new Regex(dirNamePattern, RegexOptions.CultureInvariant);

		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(DateTime dateTime)
		{
			return GetDescriptor(dateTime.Year, dateTime.Month);
		}

		public string GetFolderName(DateTime dateTime)
		{
			return GetFolderName(dateTime.Month);
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
			Check.Require(parent.Start == new DateTime(parent.Start.Year, 1, 1) && parent.End.AddYears(-1) == parent.Start, "Parent of month must be year");
			return GetDescriptor(parent.Start.Year, folderName);
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
			Check.Require(parent.Start == new DateTime(parent.Start.Year, 1, 1) && parent.End.AddYears(-1) == parent.Start, "Parent of month must be year");
			return GetDescriptor(parent.Start.Year, folderName);
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
			return new DateTime(dataItemTimestamp.Year, dataItemTimestamp.Month, 1);
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
			return GetRangeStart(dataItemTimestamp).AddMonths(1);
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
		/// 
		/// </summary>
		/// <param name="year"></param>
		/// <param name="folderName">
		///		2-digit 1 - based month number
		/// </param>
		/// <returns>
		///		<see langword="null"/> if folder name is invalid
		/// </returns>
		private CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year, string folderName)
		{
			CalendarHistoricalFolderInternalDescriptor retval = null;
			if (_folderNameRegex.IsMatch(folderName))
			{
				int month = int.Parse(folderName);
				if (month > 0 && month <= 12)
				{
					retval = GetDescriptor(year, month);
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
		/// <returns>
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		year is less than 1 or greater than 9999.  -or- month is less than 1 or greater
		///		than 12.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		///		The specified parameters evaluate to less than System.DateTime.MinValue or
		///		more than System.DateTime.MaxValue.
		/// </exception>
		private CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year, int month)
		{
			DateTime start = new DateTime(year, month, 1);

			return new CalendarHistoricalFolderInternalDescriptor()
			{
				Start = start,
				End = start.AddMonths(1),
				Name = GetFolderName(month)
			};
		}

		private string GetFolderName(int month)
		{
			return month.ToString(dirNameToStringFormat);
		}
	}
}
