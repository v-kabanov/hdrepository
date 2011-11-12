using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using System.IO;
using bfs.Repository.Interfaces.Infrastructure;
using log4net;

namespace bfs.Repository.Storage.DataFolders.Traits.CalendarDefault
{
	/// <summary>
	/// 	Levels in data folders hierarchy based on a calendar.
	/// </summary>
	public enum DataFolderLevel
	{
		/// <summary>
		/// 	Hour of the day.
		/// </summary>
		Hour = Constants.DataFolderLevelLeaf,
		/// <summary>
		/// 	Day of the month.
		/// </summary>
		Day,
		/// <summary>
		/// 	Month of the year.
		/// </summary>
		Month,
		/// <summary>
		/// 	Calendar year.
		/// </summary>
		Year
	};

	internal class CalendarHistoricalFoldersTraits : IHistoricalFoldersTraits
	{
		private static readonly ILog _log = LogManager.GetLogger("CalendarHistoricalFoldersTraits");

		public const int absoluteLevelNumberYear = (int)DataFolderLevel.Year;
		public const int absoluteLevelNumberMonth = (int)DataFolderLevel.Month;
		public const int absoluteLevelNumberDay = (int)DataFolderLevel.Day;
		public const int absoluteLevelNumberHour = (int)DataFolderLevel.Hour;

		public const int maxAllowedAbsoluteStartLevel = absoluteLevelNumberMonth;
		public const int absoluteTopLevel = absoluteLevelNumberYear;

		public const DataFolderLevel defaultDepth = DataFolderLevel.Month;

		private const int _maxLevel = absoluteLevelNumberYear;

		/// <summary>
		///		Maximum length of folder name among all levels; year - 4 digits
		/// </summary>
		private const int _estimatedMaxDirNameLength = 4;

		private static ICalendarHistoricalFolderTraits[] _levelTraits;

		private int _startLevel;

		static CalendarHistoricalFoldersTraits()
		{
			_levelTraits = new ICalendarHistoricalFolderTraits[]
			{
				new HourDataFolderTraits()
				, new DayDataFolderTraits()
				, new MonthDataFolderTraits()
				, new YearDataFolderTraits()
			};
		}

		/// <summary>
		///		 Create new instance
		/// </summary>
		/// <param name="depth">
		/// 	Lowest level in the directory tree. Note that highest level is always Year. Must be <see cref="DataFolderLevel.Month"/> or lower.
		/// </param>
		/// <param name="repository">
		/// 	Owning repository.
		/// </param>
		public CalendarHistoricalFoldersTraits(DataFolderLevel depth, IRepository repository)
		{
			CheckDepth(depth);
			_startLevel = (int)depth;
			Repository = repository;
		}

		/// <summary>
		///		Check the validity of the data folders tree depth setting.
		/// </summary>
		/// <param name="depth">
		///		A <see cref="DataFolderLevel"/> representing the lowest level of the data folders tree.
		/// </param>
		/// <exception cref="ArgumentException">
		///		The depth value is invalid.
		/// </exception>
		public static void CheckDepth(DataFolderLevel depth)
		{
			Check.DoAssertLambda((int)depth >= Constants.DataFolderLevelLeaf && (int)depth <= maxAllowedAbsoluteStartLevel
				, () => new ArgumentException(StorageResources.DataFoldersDepthInvalid));
		}

		/// <summary>
		///		Get or set the directory tree root.
		/// </summary>
		public string RootPath
		{ get; set; }

		/// <summary>
		///		Get the actual number of levels in the directory tree according to the current state of the class instance.
		/// </summary>
		/// <remarks>
		///		The directory tree is balanced - there's always the same number of levels from top (root) to leaf folders (containing data files).
		///		Only the lowest, leaf level folders contain data files.
		/// </remarks>
		public int LevelCount
		{
			get { return _levelTraits.Length - _startLevel; }
		}

		/// <summary>
		///		Enumerate child data folders in the [sub]tree with root at <see cref="RootPath"/>.
		/// </summary>
		/// <param name="level">
		///		Level at which to find data folders; must be the top level, i.e. equal to <see cref="RelativeTopLevelIndex"/>,
		///		which represents child nodes.
		/// </param>
		/// <returns>
		///		List of all top level data file containers.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// 	<paramref name="level"/> is not equal to <see cref="RelativeTopLevelIndex"/>.
		/// </exception>
		public List<IRepoFileContainerDescriptor> Enumerate(int level)
		{
			Check.DoCheckArgument(level == RelativeTopLevelIndex, "This implementation enumerates children only");
			return EnumerateChildren(null, RelativeTopLevelIndex);
		}

		/// <summary>
		///		Enumerate child data folders in the [sub]tree with root at <see cref="RootPath"/>.
		/// </summary>
		/// <param name="root">
		///		Descriptor of the data folder whose children to enumerate
		/// </param>
		/// <param name="level">
		///		Level at which to find data folders; must be the child of the <paramref name="root"/>, i.e. equal to <code>root.Level - 1</code>.
		/// </param>
		/// <returns>
		///		List of children of <paramref name="root"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// 	<paramref name="level"/> does not represent <paramref name="root"/>'s children.
		/// </exception>
		public List<IRepoFileContainerDescriptor> Enumerate(IRepoFileContainerDescriptor root, int level)
		{
			if (root == null)
			{
				return Enumerate(level);
			}

			Check.DoCheckArgument(level >= Constants.DataFolderLevelLeaf, "Leaf level folders do not have child folders");

			Check.DoCheckArgument(level == root.Level - 1, "This implementation enumerates children only");
			return EnumerateChildren(root, root.Level - 1);
		}

		private StringBuilder GetRelativePath(DateTime timestamp, int level)
		{
			Check.Require(level >= Constants.DataFolderLevelLeaf && level <= RelativeTopLevelIndex);

			int absoluteTargetLevel = GetAbsoluteLevelIndex(level);
			StringBuilder bld = new StringBuilder((absoluteTopLevel - absoluteTargetLevel) * _estimatedMaxDirNameLength);
			for (int n = absoluteTopLevel; n >= absoluteTargetLevel; --n)
			{
				bld.Append(_levelTraits[n].GetFolderName(timestamp)).Append(Path.DirectorySeparatorChar);
			}
			return bld;
		}
		
		/// <summary>
		/// 	Get descriptor of a data folder which would contain data item with the specified timestamp at the specified level in
		/// 	the data folders tree.
		/// </summary>
		/// <returns>
		/// 	Descriptor instance.
		/// </returns>
		/// <param name='timestamp'>
		/// 	Data item timestamp.
		/// </param>
		/// <param name='level'>
		/// 	Target level.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// 	The tree does not contain <paramref name="level"/>.
		/// </exception>
		public IRepoFileContainerDescriptor GetTargetFolder(DateTime timestamp, int level)
		{
			Check.DoAssertLambda(level >= Constants.DataFolderLevelLeaf && level <= RelativeTopLevelIndex, () => new ArgumentOutOfRangeException("level"));
			CalendarHistoricalFolderInternalDescriptor descr = GetLevelTraits(level).GetDescriptor(timestamp);

			string relativePath;
			if (level < RelativeTopLevelIndex)
			{
				// not top level - construct path
				StringBuilder bld = GetRelativePath(timestamp, level + 1);
				bld.Append(descr.Name);
				relativePath = bld.ToString();
			}
			else
			{
				relativePath = descr.Name;
			}

			return new RepoFileContainerDescriptor()
			{
				Start = descr.Start,
				End = descr.End,
				Level = level,
				RelativePath = relativePath
			};
		}

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
		public DateTime GetRangeStart(DateTime dataItemTimestamp, int level)
		{
			return GetLevelTraits(level).GetRangeStart(dataItemTimestamp);
		}

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
		public DateTime GetRangeEnd(DateTime dataItemTimestamp, int level)
		{
			return GetLevelTraits(level).GetRangeEnd(dataItemTimestamp);
		}

		//--------------------------------------------------------------------------------

		/// <summary>
		///		Get owning repository
		/// </summary>
		public IRepository Repository
		{ get; private set; }

		internal IDirectoryProvider DirectoryProvider
		{ get { return Repository.ObjectFactory.FileSystemProvider.DirectoryProvider; } }

		/// <summary>
		///		Get level index in <see cref="_levelTraits"/> by actual level number (according to the state of the class instance)
		/// </summary>
		/// <param name="levelIndex">
		///		Actual level number (according to the state of the class instance)
		/// </param>
		/// <returns></returns>
		private int GetAbsoluteLevelIndex(int levelIndex)
		{
			return _startLevel + levelIndex;
		}

		/// <summary>
		///		Get level traits by actual level number (according to the state of the class instance)
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		private ICalendarHistoricalFolderTraits GetLevelTraits(int level)
		{
			return _levelTraits[GetAbsoluteLevelIndex(level)];
		}

		/// <summary>
		///		Get relative top level index (always representing years)
		/// </summary>
		/// <remarks>
		///		Number can change because the number of levels can change and numbering starts from the lowest, leaf level (0)
		/// </remarks>
		private int RelativeTopLevelIndex
		{ get { return LevelCount - 1; } }

		/// <summary>
		///		Get leaf level index in <see cref="_levelTraits"/>.
		/// </summary>
		private int AbsoluteLeafLevelIndex
		{ get { return _startLevel; } }

		/// <summary>
		///		Get full path of a data folder by its descriptor
		/// </summary>
		/// <param name="dataFolder"></param>
		/// <returns></returns>
		private string GetFullPath(IRepoFileContainerDescriptor dataFolder)
		{
			return Path.Combine(RootPath, dataFolder.RelativePath);
		}

		// implementation
		/// <summary>
		///		Enumerate child data folders in a parent data folder
		/// </summary>
		/// <param name="parentFolder">
		///		A folder in a tree whose children to enumerate.
		/// </param>
		/// <param name="level">
		///		The level number of folders being enumerated, according to the current depth (not an index in <see cref="_levelTraits"/>)
		/// </param>
		/// <returns></returns>
		private List<IRepoFileContainerDescriptor> EnumerateChildren(IRepoFileContainerDescriptor parentFolder, int level)
		{
			Check.Require(level >= Constants.DataFolderLevelLeaf && level < LevelCount);

			ICalendarHistoricalFolderTraits traits = GetLevelTraits(level);

			string parentAbsolutePath = GetAbsolutePath(parentFolder);

			IEnumerable<string> allDirPaths = DirectoryProvider.EnumerateDirectories(parentAbsolutePath, traits.NameMask);
			List<IRepoFileContainerDescriptor> retval = new List<IRepoFileContainerDescriptor>();

			foreach (string name in allDirPaths.Select((p) => DirectoryProvider.GetLastPathComponent(p)))
			{
				CalendarHistoricalFolderInternalDescriptor internalDesc = traits.GetDescriptor(parentFolder, name);
				if (null != internalDesc)
				{
					IRepoFileContainerDescriptor descr = GetDescriptor(parentFolder, internalDesc);
					retval.Add(descr);
				}
				else
				{
					_log.WarnFormat("Data folder name {0}/{1} is malformed, ignoring", parentFolder == null ? "" : parentFolder.RelativePath, name);
				}
			}

			return retval;
		}

		/// <summary>
		///		Get absolute path of a data folder by its relative path
		/// </summary>
		/// <param name="relativePath">
		///		Path relative in this historical folders tree
		/// </param>
		/// <returns></returns>
		private string GetAbsolutePath(string relativePath)
		{
			if (string.IsNullOrEmpty(relativePath))
			{
				return RootPath;
			}
			else
			{
				return Path.Combine(RootPath, relativePath);
			}
		}

		/// <summary>
		///		Get absolute path of a data folder
		/// </summary>
		/// <param name="folder">
		///		Descriptor of the data folder; <see langword="null"/> considered data folders root.
		/// </param>
		/// <returns></returns>
		private string GetAbsolutePath(IRepoFileContainerDescriptor folder)
		{
			return GetAbsolutePath(folder == null ? string.Empty : folder.RelativePath);
		}

		private static string GetRelativePath(IRepoFileContainerDescriptor parentFolder, string folderName)
		{
			if (parentFolder == null)
			{
				return folderName;
			}
			else
			{
				return Path.Combine(parentFolder.RelativePath, folderName);
			}
		}

		private IRepoFileContainerDescriptor GetDescriptor(IRepoFileContainerDescriptor parentFolder, CalendarHistoricalFolderInternalDescriptor folder)
		{
			int level;
			if (parentFolder == null)
			{
				level = RelativeTopLevelIndex;
			}
			else
			{
				level = parentFolder.Level - 1;
			}
			return new RepoFileContainerDescriptor()
			{
				Start = folder.Start,
				End = folder.End,
				Level = level,
				RelativePath = GetRelativePath(parentFolder, folder.Name)
			};
		}
	}
}
