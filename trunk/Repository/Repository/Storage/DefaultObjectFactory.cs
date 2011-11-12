//-----------------------------------------------------------------------------
// <created>2/18/2010 10:53:40 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using bfs.Repository.Compressors;
using bfs.Repository.Events;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Storage.DataFolders.Traits.CalendarDefault;
using bfs.Repository.Storage.FileSystem;
using bfs.Repository.Util;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Default object factory.
	/// </summary>
	/// <remarks>
	///		This factory provides default component implementations. You can inherit it and override a few methods or implement your
	///		own factory alltogether.
	/// </remarks>
	public class DefaultObjectFactory : IObjectFactory
	{
		private Dictionary<string, ICoder> _compressors;
		private Dictionary<string, ICoder> _encryptors;
		private string _defaultCompressor;
		private DataFolderLevel _dataFoldersDepth;
		private IRepository _repository;

		/// <summary>
		///		Create new instance.
		/// </summary>
		public DefaultObjectFactory()
		{
			_compressors = new Dictionary<string, ICoder>();
			_encryptors = new Dictionary<string, ICoder>();

			ICoder defaultCoder = new DeflateCoder();
			AddCompressor(defaultCoder, false);
			this.DefaultCompressor = defaultCoder.KeyCode;
			_dataFoldersDepth = CalendarHistoricalFoldersTraits.defaultDepth;

			
			// can use new StandardDirectoryProvider() new StandardFileProvider()
			FileSystemProvider = new bfs.Repository.IO.WinNtfs.WinLongFileSystemProvider();
		}

		/// <summary>
		///		Get or set the lowest data folder level
		/// </summary>
		/// <remarks>
		///		The factory uses default implementation of data folder traits - <see cref="CalendarHistoricalFoldersTraits"/>.
		///		It allows to customise the depth of the data folders tree
		/// </remarks>
		public DataFolderLevel DataFoldersDepth
		{
			get { return _dataFoldersDepth; }
			set
			{
				CalendarHistoricalFoldersTraits.CheckDepth(value);
				_dataFoldersDepth = value;
			}
		}

		#region IObjectFactory Members

		/// <summary>
		///		Get or set the default compressor key to use for new data - <see cref="ICoder.KeyCode"/>.
		/// </summary>
		/// <remarks>
		///		The compressor must be registered beforehand with <see cref="AddCompressor"/>.
		///		The compressor code uses case-insensitive comparison semantics.
		///		The property can be accessed before setting <see cref="Repository"/>.
		/// </remarks>
		public virtual string DefaultCompressor
		{
			get
			{
				return _defaultCompressor;
			}
			set
			{
				if (Repository != null)
					CheckHelper.CheckRepositoryNotDisposed(Repository);

				Check.RequireArgumentNotNull(value, "value");

				ICoder coder = GetCompressor(value);
				if (coder == null)
				{
					throw new ArgumentException("Unknown compressor key.");
				}

				_defaultCompressor = coder.KeyCode;
			}
		}

		/// <summary>
		///		Get browser which provides access to data files in a leaf data folder.
		/// </summary>
		/// <param name="folder">
		///		The repository folder for which the browser is required.
		/// </param>
		/// <param name="fileContainer">
		///		Descriptor of leaf data folder.
		/// </param>
		/// <returns>
		///		New browser instance.
		/// </returns>
		public virtual IRepoFileContainerBrowser GetDataFileBrowser(IFolder folder, IRepoFileContainerDescriptor fileContainer)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(folder, "folder");
			Check.DoRequireArgumentNotNull(fileContainer, "fileContainer");

			return new RepoFileContainerBrowser(folder, fileContainer);
		}

		/// <summary>
		///		Get object representing file name.
		/// </summary>
		/// <param name="fileName">
		///		File name (not file path).
		/// </param>
		/// <returns>
		///		<see cref="IRepositoryFileName"/> instance representing the file.
		/// </returns>
		public virtual IRepositoryFileName GetFileDescriptor(string fileName)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(fileName, "fileName");
			return RepositoryFileName.GetFileDescriptorImpl(fileName);
		}

		/// <summary>
		///		Create new object representing file name (<see cref="IRepositoryFileName"/> instance).
		/// </summary>
		/// <param name="folder">
		///		Folder in which the file will be saved.
		/// </param>
		/// <returns>
		///		New instance implementing <see cref="IRepositoryFileName"/> for the <paramref name="folder"/>.
		/// </returns>
		/// <remarks>
		///		The actual physical file is not created here and the instance does not have to be registered anywhere.
		/// </remarks>
		public virtual IRepositoryFileName CreateNewFile(IFolder folder)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(folder, "folder");
			return new RepositoryFileName();
		}

		/// <summary>
		///		Get data folders traits for the specified repository folder.
		/// </summary>
		/// <param name="folder">
		///		The repository folder
		/// </param>
		/// <returns>
		///		<see cref="IHistoricalFoldersTraits"/>
		/// </returns>
		/// <remarks>
		///		The intent is to be able to build different data folder trees in different repo folders.
		/// </remarks>
		public virtual IHistoricalFoldersTraits GetHistoricalFoldersTraits(IFolder folder)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(folder, "folder");
			return new CalendarHistoricalFoldersTraits(DataFoldersDepth, Repository);
		}

		/// <summary>
		///		Create object representing data folder.
		/// </summary>
		/// <returns>
		///		New <see cref="IRepoFileContainerDescriptor"/> instance.
		/// </returns>
		public virtual IRepoFileContainerDescriptor GetFileContainerDescriptor()
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			return new RepoFileContainerDescriptor();
		}

		/// <summary>
		///		Create object representing root data folder of the specified repository folder.
		/// </summary>
		/// <param name="folder">
		///		Repository folder for each to create root data folder object.
		/// </param>
		/// <returns>
		///		New <see cref="IDataFolder"/> instance where <see cref="IDataFolder.IsVirtualRoot"/> is true;
		/// </returns>
		public virtual IDataFolder GetDataFolderRoot(IFolder folder)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(folder, "folder");

			return new DataFolder(folder);
		}

		/// <summary>
		///		Get compressor by its key (which is same as compressed file extension).
		/// </summary>
		/// <param name="keyCode">
		///		The compressor key <see cref="ICoder.KeyCode"/>.
		///		Use empty string to get the default compressor (<see cref="IObjectFactory.GetDefaultCompressor"/>).
		/// </param>
		/// <returns>
		///		Compressor instance, stateless, may be singleton.
		/// </returns>
		/// <remarks>
		///		The method may be called before setting <see cref="Repository"/>.
		/// </remarks>
		/// <exception cref="KeyNotFoundException">
		///		Compressor with the specified key is not registered.
		/// </exception>
		public virtual ICoder GetCompressor(string keyCode)
		{
			if (Repository != null)
				CheckHelper.CheckRepositoryNotDisposed(Repository);

			if (string.IsNullOrEmpty(keyCode))
			{
				return GetDefaultCompressor();
			}

			return _compressors[NormalizeCoderCode(keyCode)];
		}

		/// <summary>
		///		Get compressor by its key (<see cref="ICoder.KeyCode"/>).
		/// </summary>
		/// <param name="code">
		///		The compressor key <see cref="ICoder.KeyCode"/>.
		/// </param>
		/// <returns>
		///		Encryptor instance, stateless, may be singleton.
		/// </returns>
		/// <remarks>
		///		The method may be called before setting <see cref="Repository"/>.
		/// </remarks>
		/// <exception cref="KeyNotFoundException">
		///		Encryptor with the specified key is not registered.
		/// </exception>
		public virtual ICoder GetEncryptor(string code)
		{
			if (Repository != null)
				CheckHelper.CheckRepositoryNotDisposed(Repository);

			return _encryptors[NormalizeCoderCode(code)];
		}

		/// <summary>
		///		Register compressor.
		/// </summary>
		/// <param name="coder">
		///		New compressor to be registered.
		/// </param>
		/// <param name="replaceExisting">
		///		Whether to substitute already registered compressor with the same key (<code>coder.KeyCode</code>).
		/// </param>
		/// <remarks>
		///		The coder will be used to compress/decompress all data files having compressor key (<see cref="IRepositoryFileName.CompressorCode"/>
		///		equal to <code>coder.KeyCode</code>, which is the coder's unique key.
		///		The method may be called before setting <see cref="Repository"/>.
		///		Compressor must be registered before configuring repository folders to use it, <see cref="IRepositoryFolder.IFolderProperties"/>,
		///		<see cref="IFolderProperties.Compressor"/>.
		/// </remarks>
		public virtual void AddCompressor(ICoder coder, bool replaceExisting)
		{
			if (Repository != null)
				CheckHelper.CheckRepositoryNotDisposed(Repository);

			Check.DoRequireArgumentNotNull(coder, "coder");
			CheckHelper.CheckRealCoderCode(coder.KeyCode);

			string code = NormalizeCoderCode(coder.KeyCode);

			Check.DoAssertLambda(replaceExisting || !IsCompressorRegistered(code)
				, () => new ArgumentException(StorageResources.DuplicateCompressorCode));

			if (replaceExisting)
			{
				_compressors[code] = coder;
			}
			else
			{
				_compressors.Add(code, coder);
			}
		}

		/// <summary>
		///		Register encryptor.
		/// </summary>
		/// <param name="coder">
		///		New encryptor to be registered.
		/// </param>
		/// <param name="replaceExisting">
		///		Whether to substitute already registered encryptor with the same key (<code>coder.KeyCode</code>).
		/// </param>
		/// <remarks>
		///		The coder will be used to encrypt/decrypt all data files having encryptor key (<see cref="IRepositoryFileName.EncryptorCode"/>
		///		equal to <code>coder.KeyCode</code>, which is the coder's unique key.
		///		The method may be called before setting <see cref="Repository"/>.
		///		Encryptor must be registered before configuring repository folders to use it, <see cref="IRepositoryFolder.IFolderProperties"/>,
		///		<see cref="IFolderProperties.Encryptor"/>.
		/// </remarks>
		public virtual void AddEncryptor(ICoder coder, bool replaceExisting)
		{
			if (Repository != null)
				CheckHelper.CheckRepositoryNotDisposed(Repository);

			Util.Check.RequireArgumentNotNull(coder, "coder");
			CheckHelper.CheckRealCoderCode(coder.KeyCode);
			
			string code = NormalizeCoderCode(coder.KeyCode);
			Check.DoCheckArgument(replaceExisting || !IsCompressorRegistered(code), () => StorageResources.DuplicateEncryptorCode);

			if (replaceExisting)
			{
				_encryptors[code] = coder;
			}
			else
			{
				_encryptors.Add(code, coder);
			}
		}

		/// <summary>
		///		Get reader for this repository
		/// </summary>
		/// <param name="targetFolder">
		///		Target folder to read. Specify <see langword="null"/> to add target folders later.
		/// </param>
		/// <returns>
		///		New instance of repository reader.
		/// </returns>
		public virtual IRepositoryReader GetReader(IRepositoryFolder targetFolder)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);

			IRepositoryReader retval = new RepositoryReader(Repository);
			Repository.RegisterReader(retval);
			if (null != targetFolder)
			{
				retval.AddFolder(targetFolder);
			}

			return retval;
		}

		/// <summary>
		///		Get reader to contunue reading from the specified position.
		/// </summary>
		/// <param name="position">
		///		The position to restore.
		/// </param>
		/// <param name="handler">
		///		Receiver of the deferred position restoration messages.
		/// </param>
		/// <returns>
		///		Reader which is ready to continue reading without returning duplicates.
		/// </returns>
		/// <remarks>
		///		The <paramref name="position"/> must contain positions for individual folders (its
		///		<see cref="IReadingPosition.ContainsFolderPositions"/> must return true).
		///		The full position restoration will be deferred and the outcome can be monitored by subscribing to <see cref="SeekStatus"/>.
		/// </remarks>
		/// <seealso cref="IRepositoryReader.Seek(IReadingPosition)"/>
		/// <seealso cref="IRepositoryReader.Position"/>
		/// <seealso cref="PositionRestoreStatusEventArgs"/>
		/// <exception cref="ObjectDisposedException">
		///		The repository has been disposed.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The <paramref name="position"/> does not contain folder positions. You can create reader with <see cref="GetReader(IRepositoryFolder)"/>,
		///		add more target folders with <see cref="IRepositoryReader.AddFolder(IRepositoryFolder)"/> and call
		///		<see cref="IRepositoryReader.Seek(IReadingPosition)"/>.
		/// </exception>
		public virtual IRepositoryReader GetReader(IReadingPosition position, EventHandler<PositionRestoreStatusEventArgs> handler)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(position, "position");
			Check.DoCheckArgument(position.ContainsFolderPositions, () => StorageResources.ReaderTargetFoldersMissing);

			RepositoryReader retval = new RepositoryReader(Repository);
			Repository.RegisterReader(retval);
			if (handler != null)
			{
				retval.SeekStatus += handler;
			}
			retval.Seek(position);
			return retval;
		}

		/// <summary>
		///		Get writer for this repository
		/// </summary>
		/// <param name="targetFolder">
		///		Target root folder to write. Data items may go to descendants folders,
		///		<see cref="bfs.Repository.Interfaces.IDataItem.RelativePath"/>
		/// </param>
		/// <returns>
		///		New instance of <see cref="IRepositoryWriter"/>.
		/// </returns>
		/// <remarks>
		/// 	Only 1 writer can be created for a particular folder.
		/// </remarks>
		public virtual IRepositoryWriter GetWriter(IRepositoryFolder targetFolder)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.RequireArgumentNotNull(targetFolder, "targetFolder");

			RepositoryWriter retval;
			lock (targetFolder)
			{
				RepositoryFolder.CheckNotDetached(targetFolder);

				Check.DoAssertLambda(!Repository.IsDataBeingWrittenTo(targetFolder, false),
					() => new InvalidOperationException(string.Format(StorageResources.WriterAlreadyExistsForFolder, targetFolder.LogicalPath)));

				retval = new RepositoryWriter(RepositoryFolder.CastFolder(targetFolder));
				Repository.RegisterWriter(retval);
			}
			Check.Ensure(Repository.IsDataBeingWrittenTo(targetFolder, false));
			return retval;
		}

		/// <summary>
		///		Get compressor associated with <see cref="IObjectFactory.DefaultCompressor"/>
		/// </summary>
		/// <returns>
		///		Compressor instance, stateless, may be singleton.
		/// </returns>
		/// <remarks>
		///		By default the method returns <see cref="DeflateCoder"/>.
		/// </remarks>
		public virtual ICoder GetDefaultCompressor()
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.Require(!string.IsNullOrEmpty(_defaultCompressor), "Default compressor not set");
			return GetCompressor(_defaultCompressor);
		}

		/// <summary>
		/// 	Get file system provider.
		/// </summary>
		public virtual IFileSystemProvider FileSystemProvider
		{ get; private set; }

		/// <summary>
		///		Create regular folder object instance
		/// </summary>
		/// <param name="parent">
		///		Parent folder
		/// </param>
		/// <param name="name">
		///		Folder name
		/// </param>
		/// <returns>
		///		New instance of regular repository folder
		/// </returns>
		public virtual IFolder GetFolder(IFolder parent, string name)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(parent, "parent");
			RepositoryFolder.CheckNotDetached(parent);
			Exceptions.DifferentRepositoriesExceptionHelper.Check(Repository, parent.Repository);

			return new RepositoryFolder(parent, name);
		}

		/// <summary>
		///		Create repository root folder instance
		/// </summary>
		/// <param name="repository">
		///		Owning repository
		/// </param>
		/// <returns>
		///		New instance
		/// </returns>
		public virtual IFolder GetFolder(IRepository repository)
		{
			Check.DoRequireArgumentNotNull(repository, "repository");
			CheckHelper.CheckRepositoryNotDisposed(repository);
			return new RepositoryFolder(repository);
		}

		/// <summary>
		///		Get or set the owning repository manager.
		/// </summary>
		/// <remarks>
		///		This property is (and should be) set only by the <see cref="RepositoryManager"/> during initialisation immediately
		///		before requesting virtual root folder.
		/// </remarks>
		public IRepository Repository
		{
			get { return _repository; }
			set
			{
				Check.DoAssertLambda(null == _repository, () => new InvalidOperationException(StorageResources.CannotChangeRepoInFactory));
				_repository = value;
			}
		}
		
		/// <summary>
		/// 	Get data file iterator.
		/// </summary>
		/// <param name="folder">
		/// 	Folder whose data files to iterate over.
		/// </param>
		/// <param name="backwards">
		/// 	Initial iteration direction.
		/// </param>
		/// <returns>
		/// 	<see cref="IDataFileIterator"/>
		/// </returns>
		/// <remarks>
		///		Note that you must call one of the Seek methods to start iteration after the iterator instance is created.
		/// </remarks>
		public IDataFileIterator GetDataFileIterator(IRepositoryFolder folder, bool backwards)
		{
			CheckHelper.CheckRepositoryNotDisposed(Repository);
			Check.DoRequireArgumentNotNull(folder, "folder");
			RepositoryFolder.CheckNotDetached(folder);
			return new DataFileIterator(folder, backwards);
		}

		/// <summary>
		///		Create data file accessor instance.
		/// </summary>
		/// <param name="folder">
		///		Data folder containig the file.
		/// </param>
		/// <param name="file">
		///		Data file to be accessed; may not exist on disk.
		/// </param>
		/// <returns>
		///		New <see cref="IDataFileAccessor"/> instance.
		/// </returns>
		public IDataFileAccessor GetDataFileAccessor(IDataFolder folder, IRepositoryFileName file)
		{
			Check.DoRequireArgumentNotNull(folder, "folder");
			Check.DoRequireArgumentNotNull(file, "file");
			RepositoryFolder.CheckNotDetached(folder.RepoFolder);

			return new RepositoryFileAccessor(folder, file);
		}
		
		#endregion IObjectFactory Members

		private bool IsCompressorRegistered(string normalizedCode)
		{
			return _compressors.ContainsKey(normalizedCode);
		}

		private bool IsEncryptorRegistered(string normalizedCode)
		{
			return _encryptors.ContainsKey(normalizedCode);
		}

		private string NormalizeCoderCode(string code)
		{
			if (null == code)
			{
				return string.Empty;
			}
			return code.ToLowerInvariant();
		}

		/// <summary>
		///		Check that the code is either an empty string or a compressor with this code is registered.
		/// </summary>
		/// <param name="code">
		///		Code of a compressor which is required to be registered with the factory (the code should not be normalized).
		/// </param>
		/// <returns>
		///		Normalized code.
		/// </returns>
		internal string CheckExistingCompressorReference(string code)
		{
			code = NormalizeCoderCode(code);
			Check.DoAssertLambda(
				code.Length == 0
				|| _compressors.ContainsKey(code)
				, () => new ArgumentException(string.Format(StorageResources.UnknownCompressor_Name, code)));
			return code;
		}

		/// <summary>
		///		Check that an encryptor with the specified code is registered.
		/// </summary>
		/// <param name="code">
		///		Code of an encryptor which is required to be registered with the factory (the code should not be normalized).
		/// </param>
		/// <returns>
		///		Normalized code.
		/// </returns>
		internal string CheckExistingEncryptorReference(string code)
		{
			code = NormalizeCoderCode(code);
			Check.DoAssertLambda(
				_encryptors.ContainsKey(code)
				, () => new ArgumentException(string.Format(StorageResources.UnknownEncryptor_Name, code)));
			return code;
		}

	}
}
