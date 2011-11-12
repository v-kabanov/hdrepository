//-----------------------------------------------------------------------------
// <created>2/17/2010 11:26:07 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace bfs.Repository.Storage
{
	[Obsolete]
	internal class MonthDirLevelTraits : YearMonthDirLevelTraits
	{
		internal MonthDirLevelTraits()
		{
			DirNameMask = "??";
			DirNameToStringFormat = dirNameToStringFormatMonth;
			DirNameRegex = new Regex(@"^\d{2}$", RegexOptions.CultureInvariant);
			MinValue = 1;
			MaxValue = 12;
			Level = 0;	// by default levels are [year]\[month]
		}
		internal override DateTime GetRangeStart(DateTime dataItemTimestamp)
		{
			return new DateTime(dataItemTimestamp.Year, dataItemTimestamp.Month, 1);
		}

		internal override DateTime GetRangeEnd(DateTime rangeStart)
		{
			return rangeStart.AddMonths(1);
		}
		/// <summary>
		///		
		/// </summary>
		/// <param name="rangeStart"></param>
		/// <remarks>
		///		by default levels are [year]\[month]
		/// </remarks>
		/// <returns></returns>
		internal override string GetRelativePath(DateTime rangeStart)
		{
			return Path.Combine(
				rangeStart.Year.ToString(dirNameToStringFormatYear),
				rangeStart.Month.ToString(dirNameToStringFormatMonth));
		}
	}
}
