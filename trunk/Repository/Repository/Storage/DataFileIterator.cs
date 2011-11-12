using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		The class implements iteration over data files in a repository folder.
	/// </summary>
	/// <example>
	///		Iterating over all data files in the repository folder with relative path "/relativePath/relativePath" containing
	///		data with timestamp within the last 24 hours inclusive.
	///		<code>
	///		IRepository repository;
	///		//...
	///		var iterator = new DataFileIterator(repository.RootFolder.GetDescendant("relativePath/relativePath", false), false);
	///		iterator.Seek(DateTime.Now.AddDays(-1));
	///		while (iterator.Current != null)
	///		{
	///			Console.WriteLine(iterator.Current.Path);
	///			iterator.MoveNext();
	///		}
	///		</code>
	/// </example>
	/// <remarks>
	///		The default implementation of <see cref="IObjectFactory.GetDataFileIterator(IRepositoryFolder, bool)"/> creates instances
	///		of this class. Note that you must call one of the Seek methods to start iteration after the iterator instance is created.
	/// </remarks>
	public class DataFileIterator : IDataFileIterator
	{
		private IFolder _targetFolder;
		private bool _backwards;

		/// <summary>
		///		Create new instance.
		/// </summary>
		/// <param name="targetFolder">
		///		Target repository folder.
		/// </param>
		/// <param name="backwards">
		///		Whether to iterate backwards.
		/// </param>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached.
		/// </exception>
		public DataFileIterator(IRepositoryFolder targetFolder, bool backwards)
		{
			RepositoryFolder.CheckNotDetached(targetFolder);
			_targetFolder = RepositoryFolder.CastFolder(targetFolder);
			_backwards = backwards;
			Current = NextForward = NextBackwards = Next = Previous = null;
		}

		/// <summary>
		///		Get target repository folder.
		/// </summary>
		public IFolder Folder
		{ get { return _targetFolder; } }

		/// <summary>
		///		Get target repository instance.
		/// </summary>
		public IRepository Repository
		{ get { return Folder.Repository; } }

		/// <summary>
		///		Initialise iteration position.
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp; files containig data items with the timestamp will be iterated.
		/// </param>
		/// <param name="backwards">
		///		Direction in which to search for data files if the <paramref name="seekTime"/> is not covered by any
		///		existing data file
		/// </param>
		/// <returns>
		///		First found data file; null if none found.
		/// </returns>
		/// <remarks>
		///		The data in a data file does not have to be entirely in the specified datetime range for the file to be iterated. If any data in the file
		///		falls in the sought range it will be returned by the iterator.
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached.
		/// </exception>
		public IRepositoryFile Seek(DateTime seekTime, bool backwards)
		{
			RepositoryFolder.CheckNotDetached(Folder);

			DateTime seekTimeCorrected;
			IRepositoryFile currentFile = _targetFolder.RootDataFolder.Seek(seekTime, backwards);
			if (currentFile != null)
			{
				seekTimeCorrected = currentFile.Name.FirstItemTimestamp;
			}
			else
			{
				seekTimeCorrected = seekTime;
			}

			SeekExact(seekTimeCorrected);

			Check.Ensure(Current == null || Current.Name == currentFile.Name, "Seek results differ");
			Check.Ensure(Current == null ||
				GetComparer(backwards).Compare(
					backwards ? Current.Name.FirstItemTimestamp : Current.Name.LastItemTimestamp
					, seekTime
				) >= 0
			);
			Check.Ensure(NextBackwards == null || NextBackwards.Name.End <= seekTime);
			Check.Ensure(NextForward == null || NextForward.Name.FirstItemTimestamp > seekTime);
			return Current;
		}

		/// <summary>
		///		Initialise iteration position using default direction (<see cref="Backwards"/>)
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		First found data file; null if none found.
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached.
		/// </exception>
		public IRepositoryFile Seek(DateTime seekTime)
		{
			return Seek(seekTime, Backwards);
		}

		/// <summary>
		///		Only set current if owns the <paramref name="seekTime"/>. Set next and previous as per <paramref name="seekTime"/>
		/// </summary>
		/// <param name="seekTime">
		///		Data timestamp
		/// </param>
		/// <returns>
		///		Existing <see cref="IRepositoryFile"/> if it covers the <paramref name="seekTime"/>.
		///		<see langword="null"/> otherwise
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached.
		/// </exception>
		public IRepositoryFile SeekExact(DateTime seekTime)
		{
			RepositoryFolder.CheckNotDetached(Folder);

			IRepositoryFile owner;
			IRepositoryFile predecessor;
			IRepositoryFile successor;

			_targetFolder.RootDataFolder.CutDataFiles(seekTime, out predecessor, out owner, out successor);

			Current = owner;
			NextBackwards = predecessor;
			NextForward = successor;

			if (Backwards)
			{
				Next = NextBackwards;
				Previous = NextForward;
			}
			else
			{
				Next = NextForward;
				Previous = NextBackwards;
			}

			return Current;
		}

		/// <summary>
		///		Get or set the default direction. When using <see cref="MoveNext(System.Boolean)"/> the setting is ignored.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed when setting value.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached when setting value.
		/// </exception>
		public bool Backwards
		{
			get { return _backwards; }
			set
			{
				RepositoryFolder.CheckNotDetached(Folder);

				if (value != _backwards)
				{
					IRepositoryFile file = Next;
					Next = Previous;
					Previous = file;

					_backwards = value;
				}
			}
		}

		/// <summary>
		///		Get the current file.
		/// </summary>
		public IRepositoryFile Current { get; private set; }

		/// <summary>
		///		Get next file according to <see cref="Backwards"/> setting
		/// </summary>
		public IRepositoryFile Next { get; private set; }

		/// <summary>
		///		Get previous file according to <see cref="Backwards"/> setting
		/// </summary>
		public IRepositoryFile Previous { get; private set; }

		/// <summary>
		///		Get the next newest data file
		/// </summary>
		public IRepositoryFile NextForward { get; private set; }

		/// <summary>
		///		Get the next oldest data file
		/// </summary>
		public IRepositoryFile NextBackwards { get; private set; }

		/// <summary>
		///		Move current file to the next file.
		/// </summary>
		/// <param name="backwards">
		///		The direction in which to move
		/// </param>
		/// <returns>
		///		New current file (either <see cref="NextForward"/> or <see cref="NextBackwards"/> prior to the call to this method)
		/// </returns>
		/// <remarks>
		///		Does nothing if <see cref="Current"/> and <see cref="Next"/> are already <see langword="null"/>.
		///		Otherwise the iterator will not be able to go back.
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached.
		/// </exception>
		public IRepositoryFile MoveNext(bool backwards)
		{
			RepositoryFolder.CheckNotDetached(Folder);

			if (Current != null || Next != null)
			{
				Previous = Current;
				if (backwards)
				{
					NextForward = Current;
					Current = NextBackwards;
					Next = NextBackwards = GetNextNullSafe(Current, backwards);
				}
				else
				{
					NextBackwards = Current;
					Current = NextForward;
					Next = NextForward = GetNextNullSafe(Current, backwards);
				}
			}
			return Current;
		}

		/// <summary>
		///		Move current file to the next file in the default direction (<see cref="Backwards"/>).
		/// </summary>
		/// <returns>
		///		New current file (either <see cref="NextForward"/> or <see cref="NextBackwards"/> prior to the call to this method)
		/// </returns>
		/// <remarks>
		///		Does nothing if <see cref="Current"/> is already <see langword="null"/>. Otherwise the iterator will not be able to go back.
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The target repository instance is disposed.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="targetFolder"/> is detached.
		/// </exception>
		public IRepositoryFile MoveNext()
		{
			return MoveNext(this.Backwards);
		}

		private IRepositoryFile GetNextNullSafe(IRepositoryFile file, bool backwards)
		{
			if (file == null)
			{
				return null;
			}
			return file.GetNext(backwards);
		}

		private IDirectedTimeComparison GetComparer(bool backwards)
		{
			return TimeComparer.GetComparer(Backwards);
		}
	}
}
