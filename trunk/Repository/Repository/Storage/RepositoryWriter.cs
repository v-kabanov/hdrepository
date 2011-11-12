//-----------------------------------------------------------------------------
// <created>2/26/2010 11:27:58 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;

using bfs.Repository.Exceptions;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;
using bfs.Repository.Storage.FileSystem;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		The class allows to write data to a repository subtree.
	/// </summary>
	/// <remarks>
	///		Writer's target is set upon creation; it is a repository folder representing a subtree (with its descendants) into which
	///		the writer can write. Within the same repository object model (instance) there can be no other writer writing data into or even having
	///		the folder as its target at the same time. Data can be written into any descendant folder using <see cref="IRepositoryWriter.DataRouter"/>
	///		or <see cref="IDataItem.RelativePath"/>.
	///		Note that compressor and encryptor are set when first item is written to a folder according to the folder's settings. Subsequent changes
	///		to the folder settings do not affect the existing writer.
	/// </remarks>
	internal class RepositoryWriter : IRepositoryWriter
	{
		class SimpleDataRouter : IDataRouter
		{
			public string GetRelativePath(IDataItem dataItem)
			{
				return dataItem.RelativePath;
			}
		}

		#region fields --------------------------------------------------------

		private IFolder _repoFolder;

		// writers by relative path
		Dictionary<string, DirectSingleFolderWriter> _directWritersByRelativePath;

		Dictionary<string, DirectSingleFolderWriter> _directWritersByFolderKey;

		private IDataRouter _customDataRouter;

		/// <summary>
		///		The router that is used for the purpose; must never be null;
		///		Set only in DataRouter setter
		/// </summary>
		private IDataRouter _activeDataRouter;

		private IComparer<IDataItem> _equalTimestampedItemsComparer;

		//TODO: shouldn't this event be on target folders?
		private FastSmartWeakEvent<EventHandler<Events.DataItemAddedEventArgs>> _itemAddedEvent;

		private bool _trackUnsavedItems;
		
		private LongSlaveTransactionManager _transactionManager;

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		/// <summary>
		///		Create new writer instance
		/// </summary>
		/// <param name="targetFolder">
		///		The target of the writer (subtree root).
		/// </param>
		internal RepositoryWriter(IFolder targetFolder)
		{
			_itemAddedEvent = new FastSmartWeakEvent<EventHandler<Events.DataItemAddedEventArgs>>();
			_repoFolder = targetFolder;
			_directWritersByRelativePath = new Dictionary<string, DirectSingleFolderWriter>();
			_directWritersByFolderKey = new Dictionary<string, DirectSingleFolderWriter>();
			_trackUnsavedItems = true;

			//this.DesiredFileSize = Constants.DefaultDataItemsPerFile;

			// must initialise active router
			this.DataRouter = null;

			_transactionManager = new LongSlaveTransactionManager(Folder.Repository, null);
		}

		#endregion constructors -----------------------------------------------


		#region protected methods

		/// <summary>
		///		Method called from <see cref="Write(IDataItem)"/>
		/// </summary>
		/// <param name="folder">
		///		Repository folder into which new data item has been added
		/// </param>
		/// <param name="file">
		///		The data file the data item is going to go to.
		/// </param>
		/// <param name="dataItem">
		///		The new data item
		/// </param>
		protected virtual void OnDataItemAdded(IRepositoryFolder folder, IRepositoryFileName file, IDataItem dataItem)
		{
			if (!_itemAddedEvent.HasNoSubscribers)
			{
				_itemAddedEvent.Raise(this, new bfs.Repository.Events.DataItemAddedEventArgs(folder, file, dataItem));
			}
		}

		#endregion protected methods

		#region private methods -----------------------------------------------

		private void CheckNotDisposed()
		{
			CheckHelper.CheckRepositoryNotDisposed(this);
			Check.DoAssertLambda(IsOpen, () => new ObjectDisposedException(string.Empty));
		}

		/// <summary>
		///		Get existing or [optionally] create new repo subfolder
		/// </summary>
		/// <param name="relativePath">
		///		Path relative to target folder (<code>this.Folder</code>)
		/// </param>
		/// <param name="createIfMissing">
		///		Whether to create missing subfolders
		/// </param>
		/// <returns>
		///		Existing subfolder or null
		/// </returns>
		private IRepositoryFolder GetSubfolder(string relativePath, bool createIfMissing)
		{
			return _repoFolder.GetDescendant(relativePath, createIfMissing);
		}

		private void CloseAllAccessors()
		{
			foreach (DirectSingleFolderWriter writer in _directWritersByRelativePath.Values)
			{
				writer.Close();
			}
			_directWritersByRelativePath.Clear();
			_directWritersByFolderKey.Clear();
		}

		/// <summary>
		///		Not thread safe
		/// </summary>
		private DirectSingleFolderWriter GetExistingWriterByRelativePath(string relativePath)
		{
			DirectSingleFolderWriter retval = null;
			_directWritersByRelativePath.TryGetValue(relativePath, out retval);
			return retval;
		}

		/// <summary>
		///		Not thread safe
		/// </summary>
		private DirectSingleFolderWriter GetExistingWriterByFolderKey(string folderKey)
		{
			DirectSingleFolderWriter retval = null;
			_directWritersByFolderKey.TryGetValue(folderKey, out retval);
			return retval;
		}

		private ICoder GetCompressor(IRepositoryFolder targetFolder)
		{
			return Repository.ObjectFactory.GetCompressor(targetFolder.Properties.Compressor);
		}

		private ICoder GetEncryptor(IRepositoryFolder targetFolder)
		{
			ICoder retval = null;
			if (targetFolder.Properties.EnableEncryption.HasValue && targetFolder.Properties.EnableEncryption.Value)
			{
				retval = Repository.ObjectFactory.GetEncryptor(targetFolder.Properties.Encryptor);
			}
			return retval;
		}

		/// <summary>
		///		Create single folder writer and add it to the collection of writers.
		/// </summary>
		/// <param name="targetFolder">
		///		Target folder for writer
		/// </param>
		/// <returns>
		///		New initialised instance. If the method succeeds (does not throw exception) 
		/// </returns>
		/// <remarks>
		///		Locking sequence: target folder lock start, then repo manager's registry lock/release, then direct writers collection lock/release,
		///		then target folder released.
		/// </remarks>
		private DirectSingleFolderWriter CreateWriter(IRepositoryFolder targetFolder, string normalisedRelativePath)
		{
			Check.RequireArgumentNotNull(targetFolder, "targetFolder");
			RepositoryFolder.CheckNotDetached(targetFolder);

			IFolder folder = RepositoryFolder.CastFolder(targetFolder);

			Exceptions.DifferentRepositoriesExceptionHelper.Check(Folder.Repository, targetFolder.Repository);
			
			DirectSingleFolderWriter writer = null;

			ICoder compressor = GetCompressor(targetFolder: targetFolder);
			ICoder encryptor = GetEncryptor(targetFolder: targetFolder);

			lock (targetFolder)
			{
				Check.DoAssertLambda(
					object.ReferenceEquals(targetFolder, _repoFolder)					// this writer has already seized this folder as target
					|| !Folder.Repository.IsDataBeingWrittenTo(targetFolder, false),
					() => new InvalidOperationException(string.Format(StorageResources.WriterAlreadyExistsForFolder, targetFolder.LogicalPath)));

				writer = new DirectSingleFolderWriter(folder, compressor, encryptor);
				writer.EqualTimestampedItemsComparer = this.EqualTimestampedItemsComparer;

				lock (_directWritersByRelativePath)
				{
					_directWritersByRelativePath.Add(normalisedRelativePath, writer);
					_directWritersByFolderKey.Add(targetFolder.FolderKey, writer);
				}
			}

			writer.TrackUnsavedItems = TrackUnsavedItems;

			Check.Ensure(Repository.IsDataBeingWrittenTo(targetFolder, false));
			return writer;
		}

		private void DropUnsavedItems()
		{
			foreach (DirectSingleFolderWriter writer in _directWritersByRelativePath.Values)
			{
				writer.DropUnsavedItems();
			}
		}

		#endregion private methods --------------------------------------------

		/// <summary>
		///		Get target root folder. Returns <see langword="null"/> when closed (<see cref="Close()"/>).
		/// </summary>
		public IFolder Folder
		{ get { return _repoFolder; } }

		#region IRepositoryWriter Members
		
		/// <summary>
		///		Get target root folder. Returns <see langword="null"/> when closed (<see cref="Close()"/>).
		/// </summary>
		IRepositoryFolder IRepositoryWriter.Folder
		{ get { return _repoFolder; } }

		/// <summary>
		///		Get target repository
		/// </summary>
		IRepositoryManager IRepositoryDataAccessor.Repository
		{ get { return Repository; } }

		/// <summary>
		///		Get target repository
		/// </summary>
		public IRepository Repository
		{ get { return Folder.Repository; } }

		/// <summary>
		///		Whether the writer can accept data
		/// </summary>
		/// <remarks>
		///		Writers must be registered with the repository manager (<see cref="IRepositoryDataAccessor.Repository"/>) before
		///		accepting any data.
		/// </remarks>
		public bool IsOpen
		{
			get { return Folder != null; }
		}

		/// <summary>
		///		Get or set data items router which maps data items to subfolders.
		///		If not set, <see cref="IDataItem.RelativePath"/> is used.
		/// </summary>
		public IDataRouter DataRouter
		{
			get { return _customDataRouter; }
			set
			{
				CheckNotDisposed();
				_customDataRouter = value;
				if (_customDataRouter != null)
				{
					_activeDataRouter = _customDataRouter;
				}
				else
				{
					_activeDataRouter = new SimpleDataRouter();
				}
			}
		}

		/// <summary>
		///		Get or set boolean value indicating whether new repo subfolders should be created on the fly according to <see cref="DataRouter"/>
		///		or <see cref="Interfaces.IDataItem.RelativePath"/>.
		/// </summary>
		/// <remarks>
		///		If this setting is <see langword="false"/> and a data item is submitted with relative path which points to a folder which does not exist
		///		an exception is thrown. Default setting is <see langword="false"/>.
		/// </remarks>
		public bool AllowSubfoldersCreation
		{ get; set; }

		/// <summary>
		///		Get or set comparer to use when sorting data items for items with equal timestamps.
		///		When timestamps are not equal the comparer has no effect.
		/// </summary>
		public IComparer<IDataItem> EqualTimestampedItemsComparer
		{
			get { return _equalTimestampedItemsComparer; }
			set
			{
				CheckNotDisposed();
				_equalTimestampedItemsComparer = value;
				foreach (DirectSingleFolderWriter writer in _directWritersByRelativePath.Values)
				{
					writer.EqualTimestampedItemsComparer = value;
				}
			}
		}

		/// <summary>
		///		Get or set whether to keep track of unsaved data items to be able to retrieve them.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		The value is set after some data is written.
		///		The writer is closed and will not be able to accept data.
		/// </exception>
		/// <remarks>
		///		The option must be set before writing any data.
		///		Default setting is <see langword="true"/>.
		/// </remarks>
		public bool TrackUnsavedItems
		{
			get { return _trackUnsavedItems; }
			set
			{
				CheckNotDisposed();
				Check.DoCheckOperationValid(_directWritersByFolderKey.Count == 0, () => StorageResources.ErOptionCannotBeSetAfterWriting);
				foreach (DirectSingleFolderWriter writer in _directWritersByFolderKey.Values)
				{
					writer.TrackUnsavedItems = value;
				}
				_trackUnsavedItems = value;
			}
		}

		public void Dispose()
		{
			Close();
		}

		/// <summary>
		///		Close writer, release any locks and free resources.
		/// </summary>
		/// <remarks>
		///		Closes all open folder accessors flushing any unsaved data. The writer becomes unable to recieve data. To continue writing into the same
		///		folder create another instance.
		/// </remarks>
		public void Close()
		{
			if (IsOpen)
			{
				CloseAllAccessors();

				_repoFolder.Repository.UnRegisterWriter(this);
				_repoFolder = null;
			}

			if (_transactionManager != null)
			{
				_transactionManager.Dispose();
			}

			Check.Ensure(!IsOpen);
		}

		/// <summary>
		///		Write data item to repository
		/// </summary>
		/// <param name="dataItem">
		///		Data item to write. Must be serializable.
		/// </param>
		/// <remarks>
		///		The <paramref name="dataItem"/> needs to be serializable employing either automatic or custom serialization. Automatic serialization
		///		requires only <see cref="SerializableAttribute"/>, but it may be difficult to implement versioning. With custom serialization
		///		the class has to implement the <see cref="ISerializable"/> interface and a special constructor. Note that no immediate check is performed
		///		in this method and if the <paramref name="dataItem"/> is not serializable the subsequent flushing of the data to disk may fail.
		///		Not thread safe, must not be used in more than 1 thread at once.
		/// </remarks>
		public void Write(IDataItem dataItem)
		{
			CheckNotDisposed();
			Check.DoRequireArgumentNotNull(dataItem, "dataItem");

			Check.DoRequire(dataItem.RelativePath != null, "RelativePath must not be null");

			Check.Invariant(_activeDataRouter != null);

			// if new transaction scope is created for ambient managed here and the transaction gets disposed
			using (var scope = _transactionManager.GetLazyTransactionScope())
			{
				DirectSingleFolderWriter targetWriter;
				// relative path as directed by active router
				string originalRelativePath = _activeDataRouter.GetRelativePath(dataItem);
	
				// have a look at exact match in the cache
				targetWriter = GetExistingWriterByRelativePath(originalRelativePath);
				if (null == targetWriter)
				{
					// no exact match, let's normalise path
					string normalizedRelativePath = RepositoryFolder.GetFolderPathKey(originalRelativePath);
	
					if (!string.Equals(normalizedRelativePath, originalRelativePath, StringComparison.Ordinal))
					{
						targetWriter = GetExistingWriterByRelativePath(normalizedRelativePath);
					}
					// if normilised is the same as original  or not same but still there's no writer in cache
					if (null == targetWriter)
					{
						// writing to the folder for the first time
						// using original relative path to preserve casing when creating
						IRepositoryFolder targetFolder = GetSubfolder(originalRelativePath, this.AllowSubfoldersCreation);
	
						if (null == targetFolder)
						{
							throw FolderNotFoundExceptionHelper.GetForWriter(this.Folder, originalRelativePath);
						}
	
						targetWriter = CreateWriter(targetFolder, normalizedRelativePath);
					}
				}
	
				Check.Ensure(targetWriter.TrackUnsavedItems == this.TrackUnsavedItems);
	
				targetWriter.Write(dataItem);
	
				OnDataItemAdded(targetWriter.Folder, targetWriter.CurrentFile, dataItem);
				
				scope.Complete();
			}
		}

		/// <summary>
		///		Flush all unsaved data to disk.
		/// </summary>
		public void Flush()
		{
			bool isTransactionOwner;

			using (var scope = _transactionManager.GetTransactionScope())
			{
				// if the flag equals true the data will be on the disk after disposing the scope
				isTransactionOwner = scope.IsTransactionOwner;

				foreach (DirectSingleFolderWriter writer in _directWritersByRelativePath.Values)
				{
					writer.Flush();
				}

				scope.Complete();
			}
			
			// if we are here and flushing was done in transaction, it must have been successfully committed
			Check.Ensure(!isTransactionOwner || (!TrackUnsavedItems || GetUnsavedItems().Sum((p) => p.Value.Count) == 0));
		}

		/// <summary>
		///		Get data items which have been submitted but not yet flushed to the disk.
		/// </summary>
		/// <returns>
		///		<code>IDictionary</code> of unsaved items lists by repository folder logical path
		/// </returns>
		/// <remarks>
		///		In the returned dictionary the key is the logical path (<see cref="IRepositoryFolder.LogicalPath"/>) to the folder into
		///		which the associated list of data items would be writen.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		<see cref="TrackUnsavedItems"/> is <see langword="false"/>
		/// </exception>
		public IDictionary<string, IList<IDataItem>> GetUnsavedItems()
		{
			Check.DoCheckOperationValid(TrackUnsavedItems, () => StorageResources.UnsavedItemsTrackingIsOff);
			Dictionary<string, IList<IDataItem>> retval = new Dictionary<string, IList<IDataItem>>(_directWritersByFolderKey.Count);

			foreach (DirectSingleFolderWriter writer in _directWritersByFolderKey.Values)
			{
				retval.Add(writer.Folder.LogicalPath, writer.UnsavedItems);
			}

			return retval;
		}

		/// <summary>
		///		Event is raised every time a new data item is written (added).
		/// </summary>
		/// <remarks>
		///		This is a weak event and subscribers will not be prevented from being garbage collector by subscription to this event
		/// </remarks>
		public event EventHandler<bfs.Repository.Events.DataItemAddedEventArgs> ItemAdded
		{
			add { _itemAddedEvent.Add(value); }
			remove { _itemAddedEvent.Remove(value); }
		}

		/// <summary>
		///		Check whether the writer is accessing data in the <paramref name="folder"/> or any of its descendants.
		///		Descandants include <paramref name="folder" /> itself.
		/// </summary>
		/// <param name="folder">
		///		<see cref="IRepositoryFolder"/> instance representing the subtree (the folder and all its descendants) root
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
		/// <exception cref="ArgumentException">
		///		<paramref name="folder"/> is not attached to the same <see cref="IRepositoryManager"/> instance (<see cref="Repository"/>)
		/// </exception>
		/// <remarks>
		///		If <paramref name="folder"/> is the same as <see cref="Folder"/> the method returns <see langword="true"/>, even before acceppting
		///		any data.
		/// </remarks>
		public bool IsAccessing(IRepositoryFolder folder, bool subtree)
		{
			Check.DoRequireArgumentNotNull(folder, "folder");

			if (!IsOpen)
			{
				return false;
			}

			Exceptions.DifferentRepositoriesExceptionHelper.Check(folder.Repository, _repoFolder.Repository);

			bool retval = object.ReferenceEquals(folder, this.Folder);

			if (!retval)
			{
				lock (_directWritersByRelativePath)
				{
					if (subtree)
					{
						for (
							var e = _directWritersByRelativePath.Values.GetEnumerator();
							!retval && e.MoveNext();
							retval = e.Current.Folder.IsDescendantOf(folder))
						{ }
					}
					else
					{
						DirectSingleFolderWriter writer = GetExistingWriterByFolderKey(folder.FolderKey);
						retval = writer != null && object.ReferenceEquals(folder, writer.Folder);
					}
				}
			}

			return retval;
		}

		#endregion
	}
}
