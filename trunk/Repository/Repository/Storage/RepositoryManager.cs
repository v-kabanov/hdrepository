//-----------------------------------------------------------------------------
// <copyright file="RepositoryManager.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/12/2010 9:34:02 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;


namespace bfs.Repository.Storage
{
	public class RepositoryManager : IRepository
	{
		#region fields --------------------------------------------------------

		private static log4net.ILog _logger = log4net.LogManager.GetLogger("RepositoryManager");

		private string _repositoryRoot;
		private IFolder _rootFolder;

		private LinkedList<Util.WeakReferenceT<IRepositoryDataAccessor>> _readers;

		private LinkedList<Util.WeakReferenceT<IRepositoryDataAccessor>> _writers;

		private ReaderWriterLockSlim _dataAccessorRegistryLock;

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		/// <summary>
		///		Create new instance using default object factory
		/// </summary>
		/// <param name="repositoryRoot">
		///		Repository root folder; will be created if does not exist.
		/// </param>
		public RepositoryManager(string repositoryRoot)
		{
			Init(new DefaultObjectFactory(), repositoryRoot);
		}

		/// <summary>
		///		Create new instance
		/// </summary>
		/// <param name="factory">
		///		Object factory to use.
		/// </param>
		/// <param name="repositoryRoot">
		///		Repository root folder; will be created if does not exist.
		/// </param>
		/// <remarks>
		///		<paramref name="factory"/>'s <see cref="IObjectFactory.Repository"/> is set as part of the initialisation.
		/// </remarks>
		/// <example>
		///		To customise the depth of data folders tree:
		///		<code>
		///			DefaultObjectFactory factory = new DefaultObjectFactory() { DataFoldersDepth = DataFolderLevel.Day };
		///			RepositoryManager manager = new RepositoryManager(factory, @"c:\repo");
		///		</code>
		/// </example>
		public RepositoryManager(IObjectFactory factory, string repositoryRoot)
		{
			Init(factory, repositoryRoot);
		}

		#endregion constructors -----------------------------------------------

		#region IRepositoryManager implementation

		/// <summary>
		///		Get root repository folder.
		/// </summary>
		/// <remarks>
		///		The root folder cannot be deleted or renamed (its name is empty string), it has no parent.
		/// </remarks>
		IRepositoryFolder IRepositoryManager.RootFolder
		{ get { return this.RootFolder; } }

		/// <summary>
		///		Get root repository folder.
		/// </summary>
		/// <remarks>
		///		The root folder cannot be deleted or renamed (its name is empty string), it has no parent.
		/// </remarks>
		public IFolder RootFolder
		{ get { return _rootFolder; } }

		/// <summary>
		///		Get repository root path.
		/// </summary>
		/// <remarks>
		///		The path points to folder which contains entire repository. Thus to back up the repository one needs to xcopy recursively the repository root.
		/// </remarks>
		public string RepositoryRoot
		{ get { return _repositoryRoot; } }

		/// <summary>
		///		Get or set object factory implementation.
		/// </summary>
		/// <remarks>
		///		Object factory works as a dependency resolver. It allows you to plug your implementation of one or more repository components.
		/// </remarks>
		public IObjectFactory ObjectFactory
		{ get; set; }

		/// <summary>
		/// 	Register new reader instance
		/// </summary>
		/// <param name="reader">
		/// 	New reader instance
		/// </param>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		/// 	The reader does not need to be de-registered, only weak reference is stored and it will be purged after being garbage
		/// 	collected.
		/// </remarks>
		public void RegisterReader(IRepositoryDataAccessor reader)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(reader, "reader");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, reader.Repository);

			_dataAccessorRegistryLock.EnterWriteLock();
			try
			{
				RemoveDeadReferences(_readers);
				_readers.AddLast(new Util.WeakReferenceT<IRepositoryDataAccessor>(reader));
			}
			finally
			{
				_dataAccessorRegistryLock.ExitWriteLock();
			}
		}

		/// <summary>
		/// 	Register new writer instance
		/// </summary>
		/// <param name="writer">
		/// 	New writer instance
		/// </param>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		/// 	The writer does not need to be de-registered, only weak reference is stored and it will be purged after being garbage
		/// 	collected.
		/// </remarks>
		public void RegisterWriter(IRepositoryDataAccessor writer)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(writer, "writer");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, writer.Repository);

			_dataAccessorRegistryLock.EnterWriteLock();
			try
			{
				RemoveDeadReferences(_writers);
				_writers.AddLast(new Util.WeakReferenceT<IRepositoryDataAccessor>(writer));
			}
			finally
			{
				_dataAccessorRegistryLock.ExitWriteLock();
			}
		}

		/// <summary>
		/// 	De-register reader which is being disposed.
		/// </summary>
		/// <param name="reader">
		/// 	The reader being disposed (must not work afterwards).
		/// </param>
		/// <returns>
		///		Whether the reader has been unregistered (false if it was not registered).
		/// </returns>
		public bool UnRegisterReader(IRepositoryDataAccessor reader)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(reader, "reader");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, reader.Repository);
			bool retval;
			_dataAccessorRegistryLock.EnterWriteLock();
			try
			{
				retval = RemoveReferences(_readers, reader);
			}
			finally
			{
				_dataAccessorRegistryLock.ExitWriteLock();
			}
			return retval;
		}

		/// <summary>
		/// 	De-register writer which is being disposed.
		/// </summary>
		/// <param name="writer">
		/// 	The writer being disposed (must not work afterwards).
		/// </param>
		/// <returns>
		///		Whether the writer has been unregistered (false if it was not registered).
		/// </returns>
		public bool UnRegisterWriter(IRepositoryDataAccessor writer)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(writer, "writer");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, writer.Repository);
			bool retval;
			_dataAccessorRegistryLock.EnterWriteLock();
			try
			{
				retval = RemoveReferences(_writers, writer);
			}
			finally
			{
				_dataAccessorRegistryLock.ExitWriteLock();
			}
			return retval;
		}

		/// <summary>
		///		Check if data is being accessed in the <paramref name="folder"/> or its subtree.
		/// </summary>
		/// <param name="folder">
		///		Folder to check current access to.
		/// </param>
		/// <param name="subtree">
		///		Whether to check access to all folders in the subtree or only to the <paramref name="folder"/> itself
		/// </param>
		/// <returns>
		///		<see langword="true"/> if there are active readers or writers to the specified folder[s]
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		///		Note that result is valid within this instance of <code>RepositoryManager</code> only.
		/// </remarks>
		public bool IsDataBeingAccessed(IRepositoryFolder folder, bool subtree)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(folder, "folder");
			return IsDataBeingAccessed(folder, subtree, true, true);
		}

		/// <summary>
		///		Check whether data is being written to <paramref name="folder"/>.
		/// </summary>
		/// <param name="folder">
		///		Folder to check current access to.
		/// </param>
		/// <param name="subtree">
		///		Whether to check access to all folders in the subtree or only to the <paramref name="folder"/> itself
		/// </param>
		/// <returns>
		///		<see langword="true"/> if there are active writers to the specified folder[s]
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		///		Note that result is valid within this instance of <code>RepositoryManager</code> only.
		/// </remarks>
		public bool IsDataBeingWrittenTo(IRepositoryFolder folder, bool subtree)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(folder, "folder");
			return IsDataBeingAccessed(folder, subtree, true, false);
		}

		/// <summary>
		///		Check whether data is being read from <paramref name="folder"/>.
		/// </summary>
		/// <param name="folder">
		///		Folder to check current access to.
		/// </param>
		/// <param name="subtree">
		///		Whether to check access to all folders in the subtree or only to the <paramref name="folder"/> itself.
		/// </param>
		/// <returns>
		///		<see langword="true"/> if there are active readers from the specified folder[s]
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		///		Note that result is valid within this instance of <code>RepositoryManager</code> only.
		/// </remarks>
		public bool IsDataBeingReadFrom(IRepositoryFolder folder, bool subtree)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(folder, "folder");
			return IsDataBeingAccessed(folder, subtree, false, true);
		}

		/// <summary>
		///		Get existing writers into either the specified folder or any of its descendants, depending on the parameter
		/// </summary>
		/// <param name="folder">
		///		<see cref="IRepositoryFolder"/> instance representing the folder or the whole subtree 
		///		(the folder and all its descendants), depending on <paramref name="subtree"/>
		/// </param>
		/// <param name="subtree">
		///		the scope of the search - <code>bool</code> indicating whether to find writer into any of the descendants of
		///		<paramref name="folder"/> (<see langword="true"/>) or just <paramref name="folder"/> itself.
		/// </param>
		/// <returns>
		///		The list of existing writers, never <see langword="null"/>
		/// </returns>
		/// <exception cref="ArgumentException">
		///		The <paramref name="folder"/> does not belong to this repository
		/// </exception>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		/// </remarks>
		public IList<IRepositoryDataAccessor> GetWriters(IRepositoryFolder folder, bool subtree)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(folder, "folder");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, folder.Repository);
			_dataAccessorRegistryLock.EnterReadLock();
			try
			{
				return GetDataAccessors(_writers, folder, subtree, true);
			}
			finally
			{
				_dataAccessorRegistryLock.ExitReadLock();
			}
		}

		/// <summary>
		///		Get existing readers from either the specified folder or any of its descendants, depending on the parameter
		/// </summary>
		/// <param name="folder">
		///		<see cref="IRepositoryFolder"/> instance representing the folder or the whole subtree 
		///		(the folder and all its descendants), depending on <paramref name="subtree"/>
		/// </param>
		/// <param name="subtree">
		///		the scope of the search - <code>bool</code> indicating whether to find writer to any of the descendants of
		///		<paramref name="folder"/> (<see langword="true"/>) or just <paramref name="folder"/> itself.
		/// </param>
		/// <returns>
		///		The list of existing readers, never <see langword="null"/>
		/// </returns>
		/// <exception cref="ArgumentException">
		///		The <paramref name="folder"/> does not belong to this repository
		/// </exception>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		/// </remarks>
		public IList<IRepositoryDataAccessor> GetReaders(IRepositoryFolder folder, bool subtree)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(folder, "folder");
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, folder.Repository);
			_dataAccessorRegistryLock.EnterReadLock();
			try
			{
				return GetDataAccessors(_readers, folder, subtree, false);
			}
			finally
			{
				_dataAccessorRegistryLock.ExitReadLock();
			}
		}

		/// <summary>
		/// 	Get or set non-persistent settings.
		/// </summary>
		public IRepositorySettings Settings
		{ get; set; }

		#endregion  IRepositoryManager implementation

		/// <summary>
		///		Throws ObjectDisposedException if disposed
		/// </summary>
		private void CheckNotDisposed()
		{
			CheckHelper.CheckRepositoryNotDisposed(this);
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				if (_dataAccessorRegistryLock != null)
				{
					_dataAccessorRegistryLock.Dispose();
					_dataAccessorRegistryLock = null;
				}
				IsDisposed = true;
			}
		}

		/// <summary>
		///		Whether the instance has been disposed.
		/// </summary>
		public bool IsDisposed
		{ get; private set; }

		/// <summary>
		///		Check read and/or write access status to the <paramref name="folder"/>.
		/// </summary>
		/// <param name="folder">
		///		Folder to check current access to.
		/// </param>
		/// <param name="subtree">
		///		Whether to check access to all folders in the subtree or only to the <paramref name="folder"/> itself
		/// </param>
		/// <param name="read">
		///		Whether to check read access, i.e. whether target folder[s] is/are being read from
		/// </param>
		/// <param name="write">
		///		Whether to check write access, i.e. whether target folder[s] is/are being written to
		/// </param>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		/// </remarks>
		private bool IsDataBeingAccessed(IRepositoryFolder folder, bool subtree, bool write, bool read)
		{
			Exceptions.DifferentRepositoriesExceptionHelper.Check(this, folder.Repository);
			Util.Check.Require(write || read);

			IFolder f = RepositoryFolder.CastFolder(folder, "folder");

			bool retval = false;
			_dataAccessorRegistryLock.EnterReadLock();
			try
			{
				if (read)
				{
					retval = IsDataBeingAccessed(_readers, f, subtree);
				}
				if (!retval && write)
				{
					retval = IsDataBeingAccessed(_writers, f, subtree);
				}
			}
			finally
			{
				_dataAccessorRegistryLock.ExitReadLock();
			}
			return retval;
		}

		private static bool IsDataBeingAccessed<T>(
			LinkedList<Util.WeakReferenceT<T>> list
			, IFolder folder
			, bool subtree)
			where T : class, IRepositoryDataAccessor
		{
			return GetDataAccessors(list, folder, subtree, true).Count > 0;
		}

		private static List<T> GetDataAccessors<T>(
			LinkedList<Util.WeakReferenceT<T>> list
			, IRepositoryFolder folder
			, bool subtree
			, bool firstOnly)
			where T : class, IRepositoryDataAccessor
		{
			List<T> retval = new List<T>();
			for (LinkedListNode<Util.WeakReferenceT<T>> node = list.First; node != null; )
			{
				LinkedListNode<Util.WeakReferenceT<T>> nextNode = node.Next;
				IRepositoryDataAccessor accessor = node.Value.Target;
				if (accessor == null)
				{
					_logger.Info("Purging dead accessor");
					list.Remove(node);
				}
				else
				{
					if (accessor.IsAccessing(RepositoryFolder.CastFolder(folder, "folder"), subtree))
					{
						_logger.InfoFormat("GetDataAccessors found alive writer for {0}", folder.LogicalPath);
						retval.Add((T)accessor);
						if (firstOnly)
						{
							break;
						}
					}
				}
				node = nextNode;
			}
			return retval;
		}

		private static bool RemoveDeadReferences<T>(LinkedList<Util.WeakReferenceT<T>> list)
			where T : class
		{
			return RemoveReferences<T>(list, null);
		}

		/// <summary>
		///		Remove the specified reference if specified and/or dead references until the reference to be removed is found
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="referenceToRemove">
		///		If null, the list will be scanned in full and all dead references removed.
		///		Otherwise, the list will be scanned until the specific reference is found, removing dead and the specific reference.
		///		After the specific reference is found scanning stops.
		/// </param>
		/// <returns>
		///		When removing specific reference (<paramref name="referenceToRemove"/> is not null) - whether the specific reference
		///		has been removed. Otherwise - whether any dead reference has been removed.
		/// </returns>
		private static bool RemoveReferences<T>(LinkedList<Util.WeakReferenceT<T>> list, T referenceToRemove)
			where T : class
		{
			bool specificReferenceRemoved = false;
			bool deadReferenceRemoved = false;
			for (LinkedListNode<Util.WeakReferenceT<T>> node = list.First; node != null;)
			{
				LinkedListNode<Util.WeakReferenceT<T>> nextNode = node.Next;
				if (!node.Value.IsAlive)
				{
					deadReferenceRemoved = true;
					list.Remove(node);
				}
				else if (referenceToRemove != null && referenceToRemove == node.Value.Target)
				{
					specificReferenceRemoved = true;
					list.Remove(node);
					break;
				}
				node = nextNode;
			}
			return referenceToRemove == null ? deadReferenceRemoved : specificReferenceRemoved;
		}

		private void Init(IObjectFactory factory, string repositoryRoot)
		{
			Check.DoRequireArgumentNotNull(factory, "factory");
			Check.DoRequireArgumentNotNull(repositoryRoot, "repositoryRoot");

			_repositoryRoot = repositoryRoot;
			this.ObjectFactory = factory;
			factory.Repository = this;

			_rootFolder = ObjectFactory.GetFolder(this);

			_readers = new LinkedList<Util.WeakReferenceT<IRepositoryDataAccessor>>();
			_writers = new LinkedList<Util.WeakReferenceT<IRepositoryDataAccessor>>();
			_dataAccessorRegistryLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			
			Settings = new RepositorySettings(this);
		}
	}
}
