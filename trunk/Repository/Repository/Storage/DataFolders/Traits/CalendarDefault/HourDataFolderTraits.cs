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
	internal class HourDataFolderTraits : ICalendarHistoricalFolderTraits
	{
		internal const string dirNamePattern = @"^\d{2}$";
		internal const string dirNameToStringFormat = "D2";
		internal const string dirNameMask = "??";

		private static readonly Regex _folderNameRegex = new Regex(dirNamePattern, RegexOptions.CultureInvariant);

		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(DateTime dateTime)
		{
			return GetDescriptor(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour);
		}

		public string GetFolderName(DateTime dateTime)
		{
			return GetFolderName(dateTime.Hour);
		}

		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(CalendarHistoricalFolderInternalDescriptor parent, string folderName)
		{
			Check.Require(parent.Start == DayDataFolderTraits.GetRangeStartImpl(parent.Start)
				&& parent.End.AddHours(-1) == parent.Start, "Parent of hour must be day");
			return GetDescriptor(parent.Start.Year, parent.Start.Month, parent.Start.Day, folderName);
		}

		public CalendarHistoricalFolderInternalDescriptor GetDescriptor(IRepoFileContainerDescriptor parent, string folderName)
		{
			Check.Require(parent.Start == DayDataFolderTraits.GetRangeStartImpl(parent.Start)
				&& parent.End.AddHours(-1) == parent.Start, "Parent of hour must be day");
			return GetDescriptor(parent.Start.Year, parent.Start.Month, parent.Start.Day, folderName);
		}

		public DateTime GetRangeStart(DateTime dataItemTimestamp)
		{
			return dataItemTimestamp.Date.AddHours(dataItemTimestamp.Hour);
		}

		public DateTime GetRangeEnd(DateTime dataItemTimestamp)
		{
			return dataItemTimestamp.Date.AddHours(dataItemTimestamp.Hour + 1);
		}

		public string NameMask
		{
			get { return dirNameMask; }
		}

		//---------------------------------------------------------------------------------

		private CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year, int month, int day, string folderName)
		{
			CalendarHistoricalFolderInternalDescriptor retval = null;
			if (_folderNameRegex.IsMatch(folderName))
			{
				int hour = int.Parse(folderName);

				if (hour >= 0 && hour < 24)
				{
					retval = GetDescriptor(year, month, day, hour);
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
		/// 	Day of the month.
		/// </param>
		/// <param name="hour">
		/// 	Hour of the day (0..23)
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
		private CalendarHistoricalFolderInternalDescriptor GetDescriptor(int year, int month, int day, int hour)
		{
			DateTime start = new DateTime(year, month, day, hour, 0, 0);

			return new CalendarHistoricalFolderInternalDescriptor()
			{
				Start = start,
				End = start.AddHours(1),
				Name = GetFolderName(hour)
			};
		}

		private string GetFolderName(int hour)
		{
			return hour.ToString(dirNameToStringFormat);
		}
	}
}
