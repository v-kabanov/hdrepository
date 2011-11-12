//-----------------------------------------------------------------------------
// <created>2/17/2010 11:18:55 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

using bfs.Repository.Util;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Base class defining common historical data folder traits for month and year levels
	/// </summary>
	[Obsolete]
	internal abstract class YearMonthDirLevelTraits
	{
		internal const string dirNameToStringFormatYear = "D4";
		internal const string dirNameToStringFormatMonth = "D2";
		internal const string dirNameToStringFormatDay = "D2";

		protected SizeCappedCache<DateTime, IRepoFileContainerDescriptor> _folderDescriptorsCache =
			new SizeCappedCache<DateTime, IRepoFileContainerDescriptor>() { SizeCap = 100, TrimPercent = 15 };

		public string DirNameMask
		{ get; protected set; }

		/// <summary>
		///		Format to use with Int32.ToString(format) to get valid dir name
		///		out of parsed integer value
		/// </summary>
		public string DirNameToStringFormat
		{ get; protected set; }

		public Regex DirNameRegex
		{ get; protected set; }

		public int MinValue
		{ get; protected set; }

		public int MaxValue
		{ get; protected set; }

		internal int Level
		{ get; set; }

		internal virtual bool IsValidDir(string dirName, ref int parsedValue)
		{
			bool retval = this.DirNameRegex.IsMatch(dirName);
			if (retval)
			{
				int nval = int.Parse(dirName);
				retval = this.MinValue <= nval && this.MaxValue >= nval;
				if (retval)
				{
					parsedValue = nval;
				}
			}
			return retval;
		}

		internal abstract DateTime GetRangeStart(DateTime dataItemTimestamp);
		internal abstract DateTime GetRangeEnd(DateTime rangeStart);
		internal abstract string GetRelativePath(DateTime rangeStart);

		/// <summary>
		///		No caching
		/// </summary>
		/// <param name="rangeStart">
		///		target folder date-time range start
		///		<see cref="GetRangeStart"/>
		/// </param>
		/// <param name="basePath">parent folder [relative] path</param>
		/// <returns></returns>
		protected IRepoFileContainerDescriptor DoGetFolderDescriptorByRange(DateTime rangeStart)
		{
			RepoFileContainerDescriptor retval = new RepoFileContainerDescriptor()
			{
				Start = rangeStart,
				End = GetRangeEnd(rangeStart),
				Level = this.Level,
				RelativePath = GetRelativePath(rangeStart)
			};
			return retval;
		}

		internal virtual IRepoFileContainerDescriptor GetFolderDescriptorByRange(DateTime rangeStart)
		{
			IRepoFileContainerDescriptor retval;
			if (_folderDescriptorsCache.TryGetItem(rangeStart, out retval))
			{
				return retval;
			}
			retval = DoGetFolderDescriptorByRange(rangeStart);
			_folderDescriptorsCache.PutItem(rangeStart, retval);
			return retval;
		}

		protected virtual IRepoFileContainerDescriptor GetFolderDescriptor(IRepoFileContainerDescriptor parent, string folderName)
		{
			int monthNumber = 0;
			if (IsValidDir(folderName, ref monthNumber))
			{
				return GetFolderDescriptorByRange(parent.Start.AddMonths(monthNumber - 1));
			}
			return null;
		}

		internal List<IRepoFileContainerDescriptor> Enumerate(
			IDirectoryProvider dirProvider, string rootPath, IRepoFileContainerDescriptor parent)
		{
			IEnumerable<string> allDirPaths = dirProvider.EnumerateDirectories(rootPath, DirNameMask);
			List<IRepoFileContainerDescriptor> retval = new List<IRepoFileContainerDescriptor>();

			foreach (string name in allDirPaths.Select((p) => dirProvider.GetLastPathComponent(p)))
			{
				IRepoFileContainerDescriptor descr = GetFolderDescriptor(parent, name);
				if (null != descr)
				{
					retval.Add(descr);
				}
			}

			return retval;
		}
	}
}
