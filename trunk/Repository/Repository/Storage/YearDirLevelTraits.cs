//-----------------------------------------------------------------------------
// <created>2/17/2010 11:20:51 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	[Obsolete]
	internal class YearDirLevelTraits : YearMonthDirLevelTraits
	{
		internal YearDirLevelTraits()
		{
			DirNameMask = "????";
			DirNameToStringFormat = dirNameToStringFormatYear;
			DirNameRegex = new Regex(@"^\d{4}$", RegexOptions.CultureInvariant);
			MinValue = 1;
			MaxValue = 9999;
			Level = 1;	// by default levels are [year]\[month]
		}

		internal override DateTime GetRangeStart(DateTime dataItemTimestamp)
		{
			return new DateTime(dataItemTimestamp.Year, 1, 1);
		}

		internal override DateTime GetRangeEnd(DateTime rangeStart)
		{
			return rangeStart.AddYears(1);
		}
		internal override string GetRelativePath(DateTime rangeStart)
		{
			return rangeStart.Year.ToString(this.DirNameToStringFormat);
		}

		protected override IRepoFileContainerDescriptor GetFolderDescriptor(IRepoFileContainerDescriptor parent, string folderName)
		{
			Check.Require(null == parent);
			int monthNumber = 0;
			if (IsValidDir(folderName, ref monthNumber))
			{
				return GetFolderDescriptorByRange(parent.Start.AddMonths(monthNumber - 1));
			}
			return null;
		}
	}
}
