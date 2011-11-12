using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	//TODO: not implemented, future task
	class FolderDataAccessor : IFolderDataAccessor
	{
		IFolder _targetFolder;
		RepositoryFileAccessor _currentFileAccessor;
		IDataFileIterator _iterator;
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetFolder"></param>
		/// <remarks>
		/// 	Constructor is internal because all readers and writers are created in the <see cref="IRepositoryManager.Objectactory />
		/// 	to be able to handle concurrency and provide own/different implementation.
		/// </remarks>
		internal FolderDataAccessor(IRepositoryFolder targetFolder)
		{
			Check.DoRequireArgumentNotNull(targetFolder, "targetFolder");
			Check.DoAssertLambda(!targetFolder.IsDetached, () => new ArgumentException(StorageResources.FolderIsNotPartOfARepository));

			_targetFolder = RepositoryFolder.CastFolder(targetFolder);
			
			_iterator = Repository.ObjectFactory.GetDataFileIterator(targetFolder, false);

			Check.Ensure(Repository.IsDataBeingAccessed(Folder, false));
		}
		
		public IRepositoryFolder Folder
		{
			get { return _targetFolder; }
		}

		public DateTime CurrentFileRangeLimitLow
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public DateTime CurrentFileRangeLimitHigh
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		//note: issue - coder to use when writing may not work when reading existing file; 2 separate coders or always automatic
		// coder for reading? same with encryptors
		public ICoder Coder
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public ICoder Encryptor
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public EnumerationDirection DefaultDirection
		{ get; set; }

		public bool SortBeforeSave
		{ get; set; }

		public IRepositoryFile CurrentFile
		{ get { return _iterator.Current; } }

		public IRepositoryFile NextNewestFile
		{ get { return _iterator.NextForward; } }

		public IRepositoryFile NextOldestFile
		{ get { return _iterator.NextBackwards; } }

		public IList<IDataItem> DataItems
		{
			get { return _currentFileAccessor.GetAllItems(); }
		}

		public void SortItemsInCurrentFile()
		{
			_currentFileAccessor.Sort();
		}

		public void Add(IDataItem dataItem)
		{
			throw new NotImplementedException();
		}

		public void AddToCurrentFile(IDataItem dataItem)
		{
			if (!_currentFileAccessor.Add(dataItem))
			{
				throw new InvalidOperationException("Data item cannot be stored in the current file");
			}
		}

		public void AddToNewFile(IDataItem dataItem)
		{
			CloseCurrentFile();
			// implement seek exact
			_iterator.Seek(dataItem.DateTime, IsBackwards(this.DefaultDirection));
			// check existing owning file - error if exists
			throw new NotImplementedException();
		}

		public void RemoveDataItem(int index)
		{
			throw new NotImplementedException();
		}

		public void RemoveDatItems(int startIndex, int count)
		{
			throw new NotImplementedException();
		}

		public void Seek(DateTime seekTimestamp)
		{
			throw new NotImplementedException();
		}

		public void Seek(DateTime seekTimestamp, EnumerationDirection direction)
		{
			_iterator.Seek(seekTimestamp, IsBackwards(direction));
		}

		public void OpenNextFile()
		{
			throw new NotImplementedException();
		}

		public void OpenPreviousFile()
		{
			throw new NotImplementedException();
		}

		public void ClearCurrentFile()
		{
			throw new NotImplementedException();
		}

		public void DeleteFile(IRepositoryFileName file)
		{
			throw new NotImplementedException();
		}

		public void Flush()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///		Check whether the accessor is accessing data in the <paramref name="folder"/> or any of its descendants.
		///		Descandants include <paramref name="folder" /> itself.
		/// </summary>
		/// <param name="folder">
		///		<see cref="IRepositoryFolder"/> instance representing the folder or the whole subtree 
		///		(the folder and all its descendants), depending on <paramref name="subtree"/>
		/// </param>
		/// <param name="subtree">
		///		<code>bool</code> indicating whether to check access to any of the descendants of <paramref name="folder"/> (<see langword="true"/>)
		///		or just <paramref name="folder"/> itself.
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the accessor is accessing data in the specified folder or folders
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="folder"/> is <see langword="null"/>
		/// </exception>
		/// <exception cref="DifferentRepositoriesException">
		///		<paramref name="folder"/> is not attached to the same <see cref="IRepositoryManager"/> instance
		///		(<see cref="IRepositoryReader.Repository"/>)
		/// </exception>
		public bool IsAccessing(IRepositoryFolder folder, bool subtree)
		{
			Check.DoRequireArgumentNotNull(folder, "folder");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, Repository);
			return folder == Folder;
		}

		/// <summary>
		///		Get target repository
		/// </summary>
		IRepositoryManager IRepositoryDataAccessor.Repository
		{ get { return _targetFolder.Repository; } }

		/// <summary>
		///		Get target repository
		/// </summary>
		public IRepository Repository
		{ get { return _targetFolder.Repository; } }

		private bool IsBackwards(EnumerationDirection direction)
		{
			return direction == EnumerationDirection.Backwards;
		}

		private void CloseCurrentFile()
		{
		}

		public void Dispose()
		{
			Close();
		}


		public void Close()
		{
			if (_currentFileAccessor != null)
			{
				_currentFileAccessor.Close();
				_currentFileAccessor = null;
			}
		}
	}
}
