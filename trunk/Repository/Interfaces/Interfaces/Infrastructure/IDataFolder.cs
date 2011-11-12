//-----------------------------------------------------------------------------
// <created>2/26/2010 10:43:30 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		Interface for an object representing data folder.
	/// </summary>
	/// <remarks>
	/// DOCO:
	///		Data is stored in compressed and (optionally) encrypted files. The files are stored in a balanced tree of data folders
	///		where only leaf level data folders contain data files. Non-leaf data folders contain child data folders. The tree structure
	///		works like an index on data item timestamp (<see cref="IDataItem.DateTime"/>). The structure is managed by
	///		<see cref="IHistoricalFoldersExplorer"/> and data files in leaf folders are managed by <see cref="IRepoFileContainerBrowser"/>.
	///		Data folder levels are numbered from 0 - leaf and up to the root level.
	///		Every <see cref="IRepositoryFolder"/> thus contains its own data folders tree. The tree is placed into a subfolder named
	///		<see cref="IFolder.DataFolderName"/>.
	/// </remarks>
	public interface IDataFolder : IRepoFileContainerDescriptor
	{
		/// <summary>
		///		Whether this is a leaf folder (one containing data files).
		/// </summary>
		bool IsLeafFolder
		{ get; }

		/// <summary>
		///		Get full filesystem path to the data folder.
		/// </summary>
		string FullPath
		{ get; }

		/// <summary>
		///		Whether this is a leaf folder and data files are loaded.
		/// </summary>
		bool DataFilesLoaded
		{ get; }

		/// <summary>
		///		Whether this folder is not a leaf folder and child folders are loaded.
		/// </summary>
		bool SubfoldersLoaded
		{ get; }

		/// <summary>
		///		Whether the data folder is root
		/// </summary>
		bool IsVirtualRoot
		{ get; }

		/// <summary>
		///		Get owning repository folder.
		/// </summary>
		IFolder RepoFolder
		{ get; }

		/// <summary>
		///		Get data file browser for this leaf folder.
		///		Files are loaded if needed (when DataFilesLoaded == false).
		///		If this is not a leaf folder an InvalidOperationException is thrown.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		This is not a leaf folder
		/// </exception>
		IRepoFileContainerBrowser DataFileBrowser
		{ get; }

		/// <summary>
		///		Get parent data folder.
		///		This will return <see langword="null" /> if <see cref="IsVirtualRoot"/> returns <see langword="true" />
		/// </summary>
		IDataFolder ParentDataFolder
		{ get; }

		/// <summary>
		///		Whether the directory exist on disk
		/// </summary>
		bool Exists
		{ get; }

		/// <summary>
		///		Get last (the newest) child data folder; returns <see langword="null"/> if none
		/// </summary>
		IDataFolder LastChild
		{ get; }

		/// <summary>
		///		Get first (the oldest) child data folder; returns <see langword="null"/> if none
		/// </summary>
		IDataFolder FirstChild
		{ get; }

		/// <summary>
		///		Get path relative to the repository root
		/// </summary>
		string PathInRepository
		{ get; }

		/// <summary>
		///		Read the list of immediately contained data files
		///		from disk.
		///		Must not be called for non-leaf folders.
		/// </summary>
		/// <param name="reloadIfLoaded">
		///		if <see langword="false"/> and files were loaded before
		///		the previous result is kept
		///		otherwise file list is read from disk
		/// </param>
		void LoadFiles(bool reloadIfLoaded);

		/// <summary>
		///		Read the list of immediately contained subfolders
		///		from disk.
		///		For leaf folders does nothing.
		/// </summary>
		/// <param name="reloadIfLoaded">
		/// </param>
		void LoadSubFolders(bool reloadIfLoaded);

		/// <summary>
		///		Free memory
		/// </summary>
		void UnloadFiles();

		/// <summary>
		///		Find first (chronologically) data file in the subtree.
		/// </summary>
		/// <param name="fromEnd">
		///		<see langword="true"/> - search for last (chronologically) data file
		///		<see langword="false"/> - search for first (chronologically) data file
		/// </param>
		/// <remarks>
		///		To reload use <see cref="Refresh()"/>
		/// </remarks>
		IRepositoryFile FindFirstDataFile(bool fromEnd);

		/// <summary>
		///		Find first leaf data subfolder and data file in it containing items dated at, earlier or later than the
		///		<paramref name="seekTime"/>
		/// </summary>
		/// <param name="seekTime">
		///		Specifies the start of date-time range (inclusive) to find data in
		/// </param>
		/// <param name="backwards">
		///		To which side from <paramref name="seekTime"/> to look for data
		/// </param>
		/// <returns>
		///		First (according to <paramref name="seekTime"/>) data file containing the required data;
		///		<see langword="null"/> if none found
		/// </returns>
		IRepositoryFile Seek(DateTime seekTime, bool backwards);

		/// <summary>
		///		Find data file owning/covering the <paramref name="seekTime"/>
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		Existing <see cref="IRepositoryFile"/> covering by its date-time range the <paramref name="seekTime"/>.
		///		<see langword="null"/> if such file does not exist.
		/// </returns>
		IRepositoryFile SeekOwner(DateTime seekTime);

		/// <summary>
		///		Get child data file next to the specified data file.
		/// </summary>
		/// <param name="file">
		///		The data file which neighbour to find
		/// </param>
		/// <param name="backwards">
		///		The direction in which to go from <paramref name="file"/>:
		///		<see langword="true"/>: to the past
		///		<see langword="false"/>: to the future
		/// </param>
		/// <returns>
		///		Existing data file which is contained in this data folder or <see langword="null"/> if the sought data file
		///		does not exist.
		/// </returns>
		IRepositoryFileName GetNextDataFile(IRepositoryFileName file, bool backwards);

		/// <summary>
		///		Causes delayed load if accessed afterwards
		/// </summary>
		void Refresh();

		/// <summary>
		///		Find existing leaf data folder and [optionally] create if it is missing. Primarily intended for use by writers.
		/// </summary>
		/// <param name="itemTimestamp">
		///		The timestamp of an item for which to find the existing or create new leaf folder
		/// </param>
		/// <returns>
		///		Existing leaf folder or
		///		<see langword="null"/> if the folder is missing and <paramref name="createIfMissing"/> is <see langword="false"/>
		/// </returns>
		IDataFolder GetLeafFolder(DateTime itemTimestamp, bool createIfMissing);

		/// <summary>
		///		Ensure the underlying file system directory exists
		/// </summary>
		void EnsureDirectoryExists();

		/// <summary>
		///		Delete data folder from disk, always recursive
		/// </summary>
		/// <param name="deleteUnknown">
		///		Delete any extra, externally created files and folders found
		/// </param>
		void Delete(bool deleteUnknown);

		/// <summary>
		///		Get child data folder next to the specified child folder.
		/// </summary>
		/// <param name="childFolder">
		///		The child folder which neighbour to find
		/// </param>
		/// <param name="backwards">
		///		The direction in which to go from <paramref name="childFolder"/>:
		///		<see langword="true"/>: to the past
		///		<see langword="false"/>: to the future
		/// </param>
		/// <returns>
		///		Existing data folder which is a child of this data folder or <see langword="null"/> if the sought data folder
		///		does not exist.
		/// </returns>
		IDataFolder GetNextChild(IDataFolder childFolder, bool backwards);

		/// <summary>
		///		Get first or last child data folder
		/// </summary>
		/// <param name="fromEnd">
		///		if <see langword="true"/> returns chronologically last, otherwise first
		/// </param>
		/// <returns>
		///		<see langword="null"/> if there are no child data folders
		/// </returns>
		IDataFolder GetFirstChildFolder(bool fromEnd);

		/// <summary>
		///		Get first child data folder covering data in the period starting from the specified timestamp.
		/// </summary>
		/// <param name="dataTimestamp">
		///		The start of the time period (inclusive) for which to find covering child data folder.
		/// </param>
		/// <param name="backwards">
		///		The direction in which the time period goes from <paramref name="dataTimestamp"/>
		/// </param>
		/// <returns>
		///		<see langword="null"/> if there are no child data folders covering data in the specified period
		/// </returns>
		IDataFolder GetFirstChildFolder(DateTime dataTimestamp, bool backwards);

		/// <summary>
		///		Get successor or predecessor of this folder at the same level of the folders hierarchy
		/// </summary>
		/// <param name="backwards">
		///		<see langword="true"/> for predecessor
		///		<see langword="false"/> for successor
		/// </param>
		/// <returns>
		///		<see langword="null"/> if none exist
		/// </returns>
		IDataFolder GetNextSiblingInTree(bool backwards);

		/// <summary>
		///		Get timestamp of the first (chronologically) data item in the subtree with this data folder at its root.
		/// </summary>
		/// <param name="fromEnd">
		///		The direction of search.
		///		<see langword="true"/> - search for last (chronologically) data item
		///		<see langword="false"/> - search for first (chronologically) data item
		/// </param>
		/// <returns>
		///		<see cref="DateTime.MinValue"/> - there's no data and <paramref name="fromEnd"/> is <see langword="true"/>
		///		<see cref="DateTime.MaxValue"/> - there's no data and <paramref name="fromEnd"/> is <see langword="false"/>
		///		Otherwise timestamp of first existing data item.
		/// </returns>
		DateTime GetFirstItemTimestamp(bool fromEnd);

		/// <summary>
		///		Cut data files in the subtree
		/// </summary>
		/// <param name="timestamp">
		///		Cutting data timesatamp
		/// </param>
		/// <param name="predecessor">
		///		Output, the immediate predecessor of the data file (if any) covering <paramref name="owner"/>.
		/// </param>
		/// <param name="owner">
		///		Output, the data file covering the <paramref name="owner"/>.
		/// </param>
		/// <param name="successor">
		///		Output, the immediate successor of the data file (if any) covering <paramref name="owner"/>.
		/// </param>
		void CutDataFiles(DateTime timestamp, out IRepositoryFile predecessor, out IRepositoryFile owner, out IRepositoryFile successor);

		/// <summary>
		///		Find first (chronologically) data file starting from the start or the end of this subtree until the end of the whole
		///		data folders tree.
		/// </summary>
		/// <param name="fromEnd">
		///		<see langword="true"/> - search from the last (the newest) leaf folder in the subtree towards the start of the whole data folders tree
		///		<see langword="false"/> - search from the first (the oldest) leaf folder in the subtree towards the end of the whole data folders tree
		/// </param>
		/// <returns>
		///		First existing file found in the segment or <see langword="null"/> if none exists
		/// </returns>
		IRepositoryFile FindFirstDataFileInSegment(bool fromEnd);

		/// <summary>
		///		Cut descendant folders at the specified level.
		/// </summary>
		/// <param name="timestamp">
		///		Cutting data timesatamp
		/// </param>
		/// <param name="level">
		///		Level at which to cut. Must point to a descendant (i.e. be less than <code>this.Level</code> and greater or equal to zero
		///		(which is leaf level).
		/// </param>
		/// <param name="predecessor">
		///		Output, the existing predecessor at <paramref name="level"/> of a folder, covering <paramref name="timestamp"/>;
		///		<see langword="null"/> if none exists.
		/// </param>
		/// <param name="owner">
		///		Output, the data folder at <paramref name="level"/>, covering <paramref name="timestamp"/>.
		///		<see langword="null"/> if none exists.
		/// </param>
		/// <param name="successor">
		///		Output, the existing successor at <paramref name="level"/> of a folder, covering <paramref name="timestamp"/>;
		///		<see langword="null"/> if none exists.
		/// </param>
		void CutDescendantFolders(DateTime timestamp, int level, out IDataFolder predecessor, out IDataFolder owner, out IDataFolder successor);

		/// <summary>
		///		Get first (chronologically) descendant at the specified level
		/// </summary>
		/// <param name="level">
		///		Level at which to find descendant. Must point to a descendant (i.e. be less than <code>this.Level</code> and greater or
		///		equal to zero (which is leaf level).
		/// </param>
		/// <param name="backwards">
		///		Whether to find oldest (<see langword="false"/>) or newest (<see langword="true"/>) descendant.
		/// </param>
		/// <returns>
		///		First descendant at <paramref name="level"/> or <see langword="null"/> if none exists.
		/// </returns>
		IDataFolder GetFirstDescendant(int level, bool backwards);

		/// <summary>
		///		Get first (chronologically) descendant at the specified level
		/// </summary>
		/// <param name="dataTimestamp">
		///		The start of the time period (inclusive) for which to find covering data folder
		/// </param>
		/// <param name="level">
		///		Level at which to find descendant. Must point to a descendant (i.e. be less than <code>this.Level</code> and greater or
		///		equal to zero (which is leaf level).
		/// </param>
		/// <param name="backwards">
		///		The direction in which the time period goes from <paramref name="dataTimestamp"/>
		/// </param>
		/// <returns>
		///		First descendant at <paramref name="level"/> or <see langword="null"/> if none exists.
		/// </returns>
		/// <remarks>
		///		For example GetFirstDescendant(DateTime.Now, 1, true) will find first existing descendant at level 1
		///		which covers time starting from now and going to the past.
		/// </remarks>
		IDataFolder GetFirstDescendant(DateTime dataTimestamp, int level, bool backwards);

		/// <summary>
		///		Get sequence of child folders which are covering, after or before the specified <paramref name="seekTime"/>,
		///		depending on direction (<paramref name="backwards"/>), inclusive of <paramref name="seekTime"/>.
		/// </summary>
		/// <param name="seekTime">
		///		Seek timestamp, inclusive.
		/// </param>
		/// <param name="backwards">
		///		Whether to go backwards (<see langword="true"/>) or forward (<see langword="false"/>) from the <paramref name="seekTime"/>.
		/// </param>
		/// <returns>
		///		<see cref="IEnumerable&lt;IDataFolder&gt;"/>
		/// </returns>
		IEnumerable<IDataFolder> GetSubfolders(DateTime seekTime, bool backwards);
	}
}
