using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using bfs.Repository.Interfaces;
using bfs.Repository.Events;

namespace RepositoryTests.Mock
{
	class RepositoryReaderMock : bfs.Repository.Interfaces.IRepositoryReader
	{
		public RepositoryReaderMock()
		{
			Reset();
		}

		public IRepositoryFolder LastFolderArgument
		{
			get
			{
				return this.FolderArguments.Count == 0 ? null : this.FolderArguments.Last();
			}
		}

		public int AddFolderCalled
		{ get; private set; }

		public int RemoveFolderCalled
		{ get; private set; }

		public List<IRepositoryFolder> FolderArguments
		{ get; set; }

		/// <summary>
		///		Value to return from AddFolder
		/// </summary>
		public bool AddFolderToReturn
		{ get; set; }

		#region IRepositoryReader members

		public bool AddFolder(bfs.Repository.Interfaces.IRepositoryFolder folder)
		{
			++this.AddFolderCalled;
			this.FolderArguments.Add(folder);
			return this.AddFolderToReturn;
		}

		public void RemoveFolder(bfs.Repository.Interfaces.IRepositoryFolder folder)
		{
			++this.RemoveFolderCalled;
			this.FolderArguments.Add(folder);
		}

		public void Seek(DateTime seekTime)
		{
			throw new NotImplementedException();
		}

		public ICollection<bfs.Repository.Interfaces.IRepositoryFolder> Folders
		{
			get { throw new NotImplementedException(); }
		}

		public DateTime NextItemTimestamp
		{
			get { throw new NotImplementedException(); }
		}

		public bool HasData
		{
			get { throw new NotImplementedException(); }
		}

		public bfs.Repository.Interfaces.IDataItemRead Read()
		{
			throw new NotImplementedException();
		}

		public DateTime GetLastItemTimestamp()
		{
			throw new NotImplementedException();
		}

		public IComparer<IDataItem> DataItemComparer
		{
			get
			{
				return null;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion IRepositoryReader members

		public void Reset()
		{
			this.AddFolderCalled = 0;
			this.AddFolderToReturn = false;
			this.FolderArguments = new List<IRepositoryFolder>();
		}

		public bfs.Repository.Util.EnumerationDirection Direction
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

		public bool CanChangeDirection
		{
			get { throw new NotImplementedException(); }
		}


		public bool AddFolder(IRepositoryFolder folder, DateTime seekTime)
		{
			throw new NotImplementedException();
		}


		public IReadingPosition Position
		{
			get { throw new NotImplementedException(); }
		}

		public void Seek(IReadingPosition position)
		{
			throw new NotImplementedException();
		}


		public bfs.Repository.Util.IDirectedTimeComparison TimeComparer
		{
			get { throw new NotImplementedException(); }
		}

		public void Close()
		{
			throw new NotImplementedException();
		}


		public void SeekStatusCallback(bfs.Repository.Storage.FolderSeekStatus status)
		{
			throw new NotImplementedException();
		}

		public event EventHandler<PositionRestoreStatusEventArgs> SeekStatus;

		public bool IsAccessing(IRepositoryFolder folder, bool subtree)
		{
			throw new NotImplementedException();
		}


		public bool AddFolder(IFolderReadingPosition folderPosition)
		{
			throw new NotImplementedException();
		}


		public IRepositoryManager Repository
		{
			get { throw new NotImplementedException(); }
		}

		public void Dispose()
		{
		}
	}
}
