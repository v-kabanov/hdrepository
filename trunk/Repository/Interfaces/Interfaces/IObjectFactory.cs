//-----------------------------------------------------------------------------
// <created>2/18/2010 10:01:37 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Storage;
using bfs.Repository.Events;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Interface of dependency resolver for the repository manager (<see cref="IRepository"/>).
	/// </summary>
	public interface IObjectFactory
	{
		/// <summary>
		///		Get or set the default compressor key to use for new data - <see cref="ICoder.KeyCode"/>.
		/// </summary>
		/// <remarks>
		///		The compressor must be registered beforehand with <see cref="AddCompressor"/>.
		///		The compressor code uses case-insensitive comparison semantics.
		/// </remarks>
		string DefaultCompressor
		{ get; set; }

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
		/// <remarks>
		///		The interface allows to organise data files differently in different repository folders.
		/// </remarks>
		IRepoFileContainerBrowser GetDataFileBrowser(IFolder folder, IRepoFileContainerDescriptor fileContainer);

		/// <summary>
		///		Get object representing file name.
		/// </summary>
		/// <param name="fileName">
		///		File name (not file path).
		/// </param>
		/// <returns>
		///		<see cref="IRepositoryFileName"/> instance representing the file name.
		/// </returns>
		IRepositoryFileName GetFileDescriptor(string fileName);

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
		IRepositoryFileName CreateNewFile(IFolder folder);

		/// <summary>
		///		Get data folders traits for the specified repository folder.
		/// </summary>
		/// <param name="folder">
		///		A repository folder.
		/// </param>
		/// <returns>
		///		<see cref="IHistoricalFoldersTraits"/>
		/// </returns>
		/// <remarks>
		///		The intent is to be able to build different data folder trees in different repo folders.
		/// </remarks>
		IHistoricalFoldersTraits GetHistoricalFoldersTraits(IFolder folder);

		/// <summary>
		///		Create object representing data folder.
		/// </summary>
		/// <returns>
		///		New <see cref="IRepoFileContainerDescriptor"/> instance.
		/// </returns>
		IRepoFileContainerDescriptor GetFileContainerDescriptor();

		/// <summary>
		///		Create object representing root data folder of the specified repository folder.
		/// </summary>
		/// <param name="folder">
		///		Repository folder for each to create root data folder object.
		/// </param>
		/// <returns>
		///		New <see cref="IDataFolder"/> instance where <see cref="IDataFolder.IsVirtualRoot"/> is true;
		/// </returns>
		IDataFolder GetDataFolderRoot(IFolder folder);

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
		void AddCompressor(ICoder coder, bool replaceExisting);

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
		void AddEncryptor(ICoder coder, bool replaceExisting);

		/// <summary>
		///		Get compressor by its key (<see cref="ICoder.KeyCode"/>).
		/// </summary>
		/// <param name="code">
		///		The compressor key <see cref="ICoder.KeyCode"/>.
		///		Use empty string to get the default compressor (<see cref="IObjectFactory.GetDefaultCompressor"/>).
		/// </param>
		/// <returns>
		///		Compressor instance, stateless, may be singleton.
		/// </returns>
		/// <exception cref="KeyNotFoundException">
		///		Compressor with the specified key is not registered.
		/// </exception>
		ICoder GetCompressor(string code);

		/// <summary>
		///		Get compressor by its key (<see cref="ICoder.KeyCode"/>).
		/// </summary>
		/// <param name="code">
		///		The compressor key <see cref="ICoder.KeyCode"/>.
		/// </param>
		/// <returns>
		///		Encryptor instance, stateless, may be singleton.
		/// </returns>
		/// <exception cref="KeyNotFoundException">
		///		Encryptor with the specified key is not registered.
		/// </exception>
		ICoder GetEncryptor(string code);

		/// <summary>
		///		Get compressor associated with <see cref="IObjectFactory.DefaultCompressor"/>
		/// </summary>
		/// <returns>
		///		Compressor instance, stateless, may be singleton.
		/// </returns>
		/// <remarks>
		///		By default the method returns <see cref="DeflateCoder"/>.
		/// </remarks>
		ICoder GetDefaultCompressor();

		/// <summary>
		///		Get reader for this repository
		/// </summary>
		/// <param name="targetFolder">
		///		Target folder to read
		/// </param>
		/// <returns>
		///		New instance of repository reader.
		/// </returns>
		/// <remarks>
		///		Implementation needs to handle concurrency rules and register the reader to enable further concurrency control,
		///		<see cref="IRepositoryManager.RegisterReader(IRepositoryReader)"/>
		///		, <see cref="IRepositoryManager.GetReaders(IRepositoryFolder, bool)"/>.
		///		While data is being accessed renaming, deleting, moving or unloading descendant folders should be
		///		blocked in the folder or any of its ancestors.
		/// </remarks>
		IRepositoryReader GetReader(IRepositoryFolder targetFolder);

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
		///		Implementation needs to handle concurrency rules and register the reader to enable further concurrency control,
		///		<see cref="IRepositoryManager.RegisterReader(IRepositoryReader)"/>
		///		, <see cref="IRepositoryManager.GetReaders(IRepositoryFolder, bool)"/>.
		///		While data is being accessed renaming, deleting, moving or unloading descendant folders should be
		///		blocked in the folder or any of its ancestors.
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
		IRepositoryReader GetReader(IReadingPosition position, EventHandler<PositionRestoreStatusEventArgs> handler);

		/// <summary>
		///		Get writer for this repository
		/// </summary>
		/// <param name="targetFolder">
		///		Target root folder to write.
		/// </param>
		/// <returns>
		///		New instance of <see cref="IRepositoryWriter"/>.
		/// </returns>
		/// <remarks>
		///		Implementation needs to handle concurrency rules and register the writer to enable further concurrency control,
		///		<see cref="IRepositoryManager.RegisterWriter(IRepositoryWriter)"/>, <see cref="IRepositoryManager.GetWriters(IRepositoryFolder, bool)"/>.
		///		The default implementation will not allow to create 2 writers for the same folder.
		///		While data is being accessed renaming, deleting, moving or unloading descendant folders should be
		///		blocked in the folder or any of its ancestors.
		/// </remarks>
		IRepositoryWriter GetWriter(IRepositoryFolder targetFolder);

		/// <summary>
		/// 	Get file system provider.
		/// </summary>
		IFileSystemProvider FileSystemProvider
		{ get; }

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
		IFolder GetFolder(IFolder parent, string name);

		/// <summary>
		///		Create repository root folder instance
		/// </summary>
		/// <param name="repository">
		///		Owning repository
		/// </param>
		/// <returns>
		///		New instance
		/// </returns>
		IFolder GetFolder(IRepository repository);

		/// <summary>
		///		Get or set the owning repository manager
		/// </summary>
		IRepository Repository
		{ get; set; }
		
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
		IDataFileIterator GetDataFileIterator(IRepositoryFolder folder, bool backwards);

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
		IDataFileAccessor GetDataFileAccessor(IDataFolder folder, IRepositoryFileName file);
	}
}
