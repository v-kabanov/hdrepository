//-----------------------------------------------------------------------------
// <created>2/16/2010 9:30:20 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using System.IO;
using System.Text.RegularExpressions;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	[Obsolete]
	internal class YearMonthHistoricalFoldersTraits : IHistoricalFoldersTraits
	{
		#region const declarations --------------------------------------------

		/// <summary>
		///		Year level, names like "2009"
		/// </summary>
		public const int FolderLevelYear = 1;
		/// <summary>
		///		Leaf level (names "01" .. "12")
		/// </summary>
		public const int FolderLevelMonth = 0;

		#endregion const declarations -----------------------------------------

		#region fields --------------------------------------------------------

		/// <summary>
		///		Array of native directory level traits;
		///		index corresponds to level (0 - month, 1 - year)
		/// </summary>
		private static readonly YearMonthDirLevelTraits[] _dirLevelTraits = new YearMonthDirLevelTraits[]
		{
			new MonthDirLevelTraits(),
			new YearDirLevelTraits()
		};

		private IDirectoryProvider _directoryProvider;

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		public YearMonthHistoricalFoldersTraits(IDirectoryProvider directoryProvider)
		{
			_directoryProvider = directoryProvider;
		}

		#endregion constructors -----------------------------------------------

		#region private methods -----------------------------------------------

		private bool CanContainItemsFromRange(
			IRepoFileContainerDescriptor folder,
			DateTime rangeStart,
			DateTime rangeEnd)
		{
			return folder.Start < rangeEnd && folder.End > rangeStart;
		}

		private List<IRepoFileContainerDescriptor> EnumerateMonths(
			IRepoFileContainerDescriptor yearDir)
		{
			Check.DoAssertLambda(yearDir.Level == FolderLevelYear, () => new ArgumentException("Year level expected in yearDir"));

			List<IRepoFileContainerDescriptor> retval = new List<IRepoFileContainerDescriptor>();

			string root = Path.Combine(this.RootPath, yearDir.RelativePath);

			if (_directoryProvider.Exists(root))
			{
				IEnumerable<string> monthDirs = _directoryProvider.EnumerateDirectories(
					root
					, _dirLevelTraits[FolderLevelMonth].DirNameMask);
				foreach (string monthDir in monthDirs)
				{
					int month = 0;
					if (_dirLevelTraits[FolderLevelMonth].IsValidDir(_directoryProvider.GetLastPathComponent(monthDir), ref month))
					{
						IRepoFileContainerDescriptor descrMonth =
							_dirLevelTraits[FolderLevelMonth].GetFolderDescriptorByRange(
								yearDir.Start.AddMonths(month - 1));

						retval.Add(descrMonth);
					}
				} // foreach (string monthDir in monthDirs)
			} // if (Directory.Exists(this.RootPath))
			return retval;
		}

		#endregion private methods --------------------------------------------

		#region IHistoricalFoldersTraits Members

		public string RootPath
		{ get; set; }

		/// <summary>
		///		2 levels - year\month:
		///			2009_
		///				 |- 01
		///				 |- 02
		/// </summary>
		public int LevelCount
		{
			get
			{
				return 2;
			}
		}

		/// <summary>
		///		Enumerate descendant data folders in the [sub]tree with root at <see cref="RootPath"/>
		/// </summary>
		/// <param name="level">
		///		Level at which to find data folders
		///			0 - leaf level
		///			1 - months
		///			2 - years
		/// </param>
		/// <returns>
		///		List of data file container descriptors containing data items falling
		///		into the specified range (<paramref name="rangeStart"/> - <paramref name="rangeEnd"/>)
		/// </returns>
		public List<IRepoFileContainerDescriptor> Enumerate(int level)
		{
			if (level < 0 || level >= this.LevelCount)
			{
				throw new ArgumentOutOfRangeException("level");
			}

			List<IRepoFileContainerDescriptor> retval = new List<IRepoFileContainerDescriptor>();

			if (_directoryProvider.Exists(this.RootPath))
			{
				IEnumerable<string> yearDirs = _directoryProvider.EnumerateDirectories(
					this.RootPath
					, _dirLevelTraits[FolderLevelYear].DirNameMask);
				foreach (string yearDir in yearDirs)
				{
					string dirName = _directoryProvider.GetLastPathComponent(yearDir);
					int year = 0;
					if (_dirLevelTraits[FolderLevelYear].IsValidDir(dirName, ref year))
					{
						IRepoFileContainerDescriptor descrYear =
							_dirLevelTraits[FolderLevelYear].GetFolderDescriptorByRange(
								new DateTime(year, 1, 1));

						if (level == YearMonthHistoricalFoldersTraits.FolderLevelYear)
						{
							retval.Add(descrYear);
						}
						else // if (level == YearMonthHistoricalFoldersTraits.FolderLevelYear)
						{
							retval.AddRange(EnumerateMonths(descrYear));
						} // else // if (level == YearMonthHistoricalFoldersTraits.FolderLevelYear)
					} // if (_dirLevelTraits[FolderLevelYear].IsValidDir(yearDir.Name, ref year))
				} // foreach (string yearDir in yearDirs)
			} // if (root.Exists)
			return retval;
		}

		/// <summary>
		///		Enumerate descendant data folders in the [sub]tree with root at <paramref name="root"/>
		/// </summary>
		/// <param name="root">
		///		One of the descendants of <see cref="RootPath"/> among the descendants of which
		///		to search; specify <see langword="null"/> to search in <see cref="RootPath"/>
		/// </param>
		/// <param name="level">
		///		Level at which to find data folders
		///			0 - leaf level
		///			1 - months
		///			2 - years
		/// </param>
		/// <returns>
		///		List of data file container descriptors containing data items falling
		///		into the specified range (<paramref name="rangeStart"/> - <paramref name="rangeEnd"/>)
		/// </returns>
		public List<IRepoFileContainerDescriptor> Enumerate(
			IRepoFileContainerDescriptor root, int level)
		{
			if (null == root)
			{
				return Enumerate(level);
			}

			// naturally this must be dropped/revised when adding more levels
			Check.Require(root.Level == FolderLevelYear && level == FolderLevelMonth
				, "Month level expected. Can only enumerate months in a year root in year-month scheme.");

			return EnumerateMonths(root);
		}

		public IRepoFileContainerDescriptor GetTargetFolder(DateTime timestamp, int level)
		{
			if (level < 0 || level >= this.LevelCount)
			{
				throw new ArgumentOutOfRangeException("level");
			}

			YearMonthDirLevelTraits levelTraits = _dirLevelTraits[level];

			return levelTraits.GetFolderDescriptorByRange(levelTraits.GetRangeStart(timestamp));
		}

		public DateTime GetRangeStart(DateTime dataItemTimestamp, int level)
		{
			if (level < 0 || level >= this.LevelCount)
			{
				throw new ArgumentOutOfRangeException("level");
			}

			YearMonthDirLevelTraits levelTraits = _dirLevelTraits[level];

			return levelTraits.GetRangeStart(dataItemTimestamp);
		}

		public DateTime GetRangeEnd(DateTime dataItemTimestamp, int level)
		{
			if (level < 0 || level >= this.LevelCount)
			{
				throw new ArgumentOutOfRangeException("level");
			}

			YearMonthDirLevelTraits levelTraits = _dirLevelTraits[level];

			return levelTraits.GetRangeEnd(levelTraits.GetRangeStart(dataItemTimestamp));
		}

		#endregion
	}
}
