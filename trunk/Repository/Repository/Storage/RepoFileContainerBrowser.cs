//-----------------------------------------------------------------------------
// <created>2/17/2010 4:06:02 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;

using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Exceptions;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		The class implements leaf data folder view; allows to browse
	///		existing data files without reading file content. Provides quick seek by time
	///		rather than file index in the sorted array
	/// </summary>
	internal class RepoFileContainerBrowser : IRepoFileContainerBrowser
	{

		#region fields --------------------------------------------------------

		private IRepoFileContainerDescriptor _container;
		private IFolder _folder;
		private IIndexedRangeCollection<DateTime, IRepositoryFileName> _files;

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		internal RepoFileContainerBrowser(
			IFolder folder
			, IRepoFileContainerDescriptor dataFileContainer)
		{
			_folder = folder;
			_container = dataFileContainer;
		}

		#endregion constructors -----------------------------------------------

		#region IRepoFileContainerBrowser implementation ----------------------

		public int FileCount
		{
			get
			{
				return null == _files ? 0 : _files.Count;
			}
		}

		public IRepositoryFileName FirstFile
		{
			get
			{
				if (this.FileCount > 0)
				{
					return _files.GetMin();
				}
				else
				{
					return null;
				}
			}
		}

		public IRepositoryFileName LastFile
		{
			get
			{
				if (this.FileCount > 0)
				{
					return _files.GetMax();
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		///		Get read-only collection of contained files.
		/// </summary>
		public Util.IReadOnlyCollection<IRepositoryFileName> Files
		{
			get
			{
				Check.RequireLambda(null != _files, () => new InvalidOperationException("Must refresh first"));
				return _files.Values;
			}
		}

		public string FullPath
		{
			get
			{
				return Path.Combine(_folder.DataFolderFullPath, _container.RelativePath);
			}
		}

		public void Refresh()
		{
			string dirPath = this.FullPath;
			IEnumerable<string> files =
				_folder.Repository.ObjectFactory.FileSystemProvider.DirectoryProvider.EnumerateFiles(dirPath, "*");

			_files = new IndexedRangeTreeDictionary<DateTime,IRepositoryFileName>(f => f.FirstItemTimestamp, f => f.End);

			foreach (string filePath in files)
			{
				IRepositoryFileName repoFile =
					_folder.Repository.ObjectFactory.GetFileDescriptor(System.IO.Path.GetFileName(filePath));
				if (null != repoFile)
				{
					AddFile(repoFile);
				}
			}
		}

		/// <summary>
		///		Get first file with data timestamped starting from the specified date (inclusive)
		/// </summary>
		/// <param name="dataTimestampFrom">
		///		Start of date-time range to find, inclusive
		/// </param>
		/// <param name="backwards">
		///		To which side of <paramref name="dataTimestampFrom"/> to look for data if there's no
		///		file containing exactly that timestamp
		/// </param>
		/// <returns>
		///		<see langword="null"/> if no file exists in the container with data in the specified date-time range
		///		otherwise first file (from <paramref name="dataTimestampFrom"/>) with the required data.
		/// </returns>
		public IRepositoryFileName GetFile(DateTime dataTimestampFrom, bool backwards)
		{
			IRepositoryFileName lowFile;
			IRepositoryFileName ownerFile;
			IRepositoryFileName highFile;
			_files.GetItems(dataTimestampFrom, out lowFile, out ownerFile, out highFile);
			if (ownerFile != null)
			{
				// regadless of direction, owner works because start of the range is inclusive same as dataTimestampFrom
				return ownerFile;
			}
			return backwards ? lowFile : highFile;
		}

		/// <summary>
		///		Get contained data file by its key - first timestamp
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		First data item timestamp
		/// </param>
		/// <returns>
		///		<see langword="null"/> file with the specified key not found
		///		<code>IRepositoryFileName</code> so that <code>IRepositoryFileName.Start</code> equals <paramref name="firstItemTimestamp"/>
		/// </returns>
		public IRepositoryFileName GetFile(DateTime firstItemTimestamp)
		{
			return _files.GetExact(firstItemTimestamp);
		}

		/// <summary>
		///		Get the sequence of files containing data items in the specified datetime range.
		/// </summary>
		/// <param name="rangeStart">
		///		Selection range start  (inclusive if less than <paramref name="rangeEnd"/> and exclusive otherwise)
		/// </param>
		/// <param name="rangeEnd">
		///		Selection range end (inclusive if less than <paramref name="rangeStart"/> and exclusive otherwise)
		/// </param>
		/// <returns>
		///		Enumerable containing all the files which contain data items in the specified datetime range
		/// </returns>
		public Util.IDirectedEnumerable<IRepositoryFileName> SelectSequence(DateTime rangeStart, DateTime rangeEnd)
		{
			Util.Check.Require(null != _files, "File collection is not initialized");

			return _files.SelectCovering(rangeStart, rangeEnd);
		}

		/// <summary>
		///		Get the sequence of files containing data in the specified datetime range.
		/// </summary>
		/// <param name="startFrom">
		///		Selection range start  (inclusive if <paramref name="backwards"/> is <see langword="false"/> and exclusive otherwise)
		/// </param>
		/// <param name="backwards">
		///		Direction of the sequence: ascending if <see langword="false"/> and descending otherwise
		/// </param>
		/// <returns>
		///		Enumerable containing all the files which contain data items in the specified datetime range
		/// </returns>
		public Util.IDirectedEnumerable<IRepositoryFileName> SelectSequence(DateTime startFrom, bool backwards)
		{
			return _files.SelectCovering(startFrom, backwards);
		}

		/// <summary>
		///		Get data files around the specified item timestamp
		/// </summary>
		/// <param name="itemTimestamp">
		///		The data item timestamp
		/// </param>
		/// <param name="predecessor">
		///		The file if any which ends before the specified timestamp
		/// </param>
		/// <param name="owner">
		///		The file covering the specified item timestamp
		/// </param>
		/// <param name="successor">
		///		The file which starts after the specified timestamp
		/// </param>
		public void GetDataFiles(DateTime itemTimestamp
			, out IRepositoryFileName predecessor
			, out IRepositoryFileName owner
			, out IRepositoryFileName successor)
		{
			Check.Require(_container.Start <= itemTimestamp && _container.End > itemTimestamp);

			_files.GetItems(itemTimestamp, out predecessor, out owner, out successor);
		}

		#region IRepoFileChangeListener implementation ------------------------

		/// <summary>
		///		Perform necessary operations after a new file has been added to the container.
		/// </summary>
		/// <param name="newRepoFile">
		///		New file added to the container
		/// </param>
		/// <exception cref="OverlappingFileInContainer">
		///		The <paramref name="newRepoFile"/> overlaps with an existing file; possible concurrency issue or internal error.
		/// </exception>
		public void FileAdded(IRepositoryFileName newRepoFile)
		{
			AddFile(newRepoFile);
		}

		/// <summary>
		///		Notify container that a file was changed
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		The timestamp of the file as it has been known to the container
		///		(the value before the change happened)
		/// </param>
		/// <param name="newFileName">
		///		New file name
		/// </param>
		/// <exception cref="FileContainerNotificationException">
		///		The file cannot be found in the container; possible concurrency issue
		/// </exception>
		/// <exception cref="OverlappingFileInContainer">
		///		The <paramref name="newRepoFile"/> overlaps with an existing file; possible concurrency issue or internal error.
		/// </exception>
		public void FileChanged(DateTime firstItemTimestamp, IRepositoryFileName newFileName)
		{
			FileDeleted(firstItemTimestamp);
			FileAdded(newFileName);
		}

		/// <summary>
		///		Notify container that a file was deleted from the container
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		The timestamp of the first item in the file as it has been known to the container (the value before deletion)
		/// </param>
		/// <exception cref="FileContainerNotificationException">
		///		The file cannot be found in the container; possible concurrency issue
		/// </exception>
		public void FileDeleted(DateTime firstItemTimestamp)
		{
			bool removed = DeleteFile(firstItemTimestamp);
			Check.DoAssertLambda(removed, () => FileContainerNotificationExceptionHelper.GetDeletionOfUnknownFile(firstItemTimestamp));
		}

		#endregion IRepoFileChangeListener implementation ---------------------

		#endregion IRepoFileContainerBrowser implementation -------------------

		#region private methods -----------------------------------------------

		/// <summary>
		///		Handle creation, renaming, or deletion of any file belonging to this container
		/// </summary>
		/// <param name="firstItemTimestamp">
		///		First item timestamp before the change; use <see cref="DateTime.MinValue"/> to handle creation
		/// </param>
		/// <param name="newFileName">
		///		File name after the change; use <see langword="null"/> to handle deletion
		/// </param>
		/// <remarks>
		///		
		/// </remarks>
		private void HandleFileChanged(DateTime firstItemTimestamp, IRepositoryFileName newFileName)
		{
			if (firstItemTimestamp != DateTime.MinValue)
			{
				bool removed = DeleteFile(firstItemTimestamp);
			}
			if (newFileName != null)
			{
				AddFile(newFileName);
			}
		}

		private void RequireFileValid(IRepositoryFileName file)
		{
			Util.Check.RequireArgumentNotNull(file, "file");
			Util.Check.RequireLambda(file.FirstItemTimestamp <= file.LastItemTimestamp, () => StorageResources.InvalidFileNamingFirstLast);
		}

		/// <summary>
		///		Not thread-safe
		/// </summary>
		private void AddFile(IRepositoryFileName file)
		{
			RequireFileValid(file);

			try
			{
				// overlapping and duplicates handled inside Add
				_files.Add(file);
			}
			catch (OverlappingRangesException e)
			{
				throw new OverlappingFileInContainer(e, _container.RelativePath);
			}
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="firstItemTimestamp">
		/// </param>
		/// <returns>
		///		<see langword="true"/> - successfully deleted
		///		<see langword="false"/> - file does not exist in the container
		/// </returns>
		private bool DeleteFile(DateTime firstItemTimestamp)
		{
			return _files.RemoveByKey(firstItemTimestamp);
		}

		private static int CompareFiles(IRepositoryFileName file1, IRepositoryFileName file2)
		{
			return DateTime.Compare(file1.FirstItemTimestamp, file2.FirstItemTimestamp);
		}

		#endregion private methods --------------------------------------------
	}
}
