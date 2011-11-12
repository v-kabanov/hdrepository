using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		The interface of repository folder as necessary for inner implementation of the repository as opposed to usage.
	/// </summary>
	/// <remarks>
	///		Detaching and attaching are used to move folders within repository.
	/// </remarks>
	public interface IFolder : IRepositoryFolder
	{
		/// <summary>
		///		Create new subfolder.
		/// </summary>
		/// <param name="name">
		///		The name of the new subfolder, It must not be equel to the reserved root data folder
		///		(<see cref="DataFolderName"/>, it must be shorter than <see cref="IDirectoryProvider.MaxPathElementLength"/>
		///		and the resultant full path must be less than the <see cref="IDirectoryProvider.MaxDirectoryPathLengh"/>
		///		and leaving 30 characters for data folders and files.
		/// </param>
		/// <returns>
		///		New repository folder instance ready to receive data.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///		<paramref name="name"/> is <see langword="null"/>
		/// </exception>
		/// <exception cref="ArgumentException">
		///		- <paramref name="name"/> is reserved (<see cref="DataFolderName"/>)
		///		- <paramref name="name"/> is too long (<see cref="IDirectoryProvider.MaxPathElementLength"/>)
		///		- A folder with the specified <paramref name="name"/> already exists.
		/// </exception>
		/// <exception cref="PathTooLongException">
		///		Full path to the new directory would be too long (<see cref="IDirectoryProvider.MaxDirectoryPathLengh"/>).
		///		30 characters are reserved for data folders and files, out of 32000 windows can handle.
		/// </exception>
		new IFolder CreateSubfolder(string name);

		/// <summary>
		///		Get existing subfolder.
		/// </summary>
		/// <param name="name">
		///		Subfolder name, case insensitive
		/// </param>
		/// <returns>
		///		<see langword="null"/> if not found
		/// </returns>
		new IFolder GetSubFolder(string name);

		/// <summary>
		///		Get existing or [optionally] create new repo subfolder which is a descendant
		///		of this folder.
		/// </summary>
		/// <param name="relativePath">
		///		Path relative to this folder
		/// </param>
		/// <param name="createIfMissing">
		///		Whether to create missing folders
		/// </param>
		/// <returns>
		///		<code>this</code> if <paramref name="relativePath"/> is empty or <see langword="null"/>;
		///		Existing descendant folder if already existed or <paramref name="createIfMissing"/> is <see langword="true"/>
		///		<see langword="null"/> if descendant with <paramref name="relativePath"/> did not exist and
		///			<paramref name="createIfMissing"/> is <see langword="false"/>
		/// </returns>
		new IFolder GetDescendant(string relativePath, bool createIfMissing);

		//------------------------------------------------------------
		/// <summary>
		///		Detach this folder instance from the repository.
		/// </summary>
		/// <param name="removeFromParentsList">
		///		Whether to remove this folder from its parent's list of child folders.
		/// </param>
		/// <remarks>
		///		The intent is to preserve state to an extent that the instance can be re-attached with minimal overhead.
		///		Subtree remains unchanged but folders in it will not function normally. This method should be used
		///		only internally and the subtree should not be left in the abnormal state after the client call.
		///		After this method completes successfully <see cref="IsDetached"/> will return <see langword="true"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is repository root (<see cref="IRepositoryManager.RootFolder"/>) or already detached.
		/// </exception>
		void Detach(bool removeFromParentsList);

		/// <summary>
		///		Attach this detached folder to a repository.
		/// </summary>
		/// <param name="parent">
		///		The [parent] folder under which to attach this folder.
		/// </param>
		/// <param name="addToParentsList">
		///		Whether to add this folder to parent's list of child folders.
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		This folder is repository root (<see cref="IRepositoryManager.RootFolder"/>) or not detached.
		/// </exception>
		void Attach(IFolder parent, bool addToParentsList);

		/// <summary>
		///		Add a folder to the list of this folder's child folders.
		/// </summary>
		/// <param name="child">
		///		The folder to add to the list.
		/// </param>
		/// <param name="attachChild">
		///		Whether to attach <paramref name="child"/> to its parent (this folder) afterwards (by calling <see cref="Attach(IFolder, bool)"/>)
		/// </param>
		/// <exception cref="InvalidOperationException">
		///		This folder is detached.
		/// </exception>
		void AddToChildList(IFolder child, bool attachChild);

		/// <summary>
		///		Remove a folder from the list of this folder's child folders.
		/// </summary>
		/// <param name="child">
		///		The folder to remove from the list.
		/// </param>
		/// <param name="detachChild">
		///		Whether to detach <paramref name="child"/> from its parent (this folder) afterwards (by calling <see cref="Detach(bool)"/>)
		/// </param>
		/// <returns>
		///		<see langword="true"/> - the <paramref name="child"/> was successfully removed from the list
		///		<see langword="false"/> - the <paramref name="child"/> was not found in the list
		/// </returns>
		/// <remarks>
		///		
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///		This <paramref name="child"/> object is a null reference.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		A folder with the same name as that of <paramref name="child"/> was found but it was not the same instance.
		/// </exception>
		bool RemoveFromChildList(IFolder child, bool detachChild);

		/// <summary>
		///		To be called by immediate child folder after being renamed
		/// </summary>
		/// <param name="folder">
		///		Child folder just renamed, <see cref="Name"/> returning new name
		/// </param>
		/// <param name="oldName">
		///		The old folder name
		/// </param>
		void OnChildFolderRenamed(IFolder folder, string oldName);

		/// <summary>
		///		Update location in the repository.
		/// </summary>
		/// <remarks>
		///		After an ancestor folder is renamed or moved cached logical path and folder key must be synchronised
		/// </remarks>
		void UpdateLogicalPath();

		/// <summary>
		///		Ge the repository to which the folder belongs.
		/// </summary>
		/// <remarks>
		///		Extending the type of the property inherited from <see cref="IRepositoryFolder"/>.
		/// </remarks>
		new IRepository Repository
		{ get; }

		/// <summary>
		///		Get parent folder. Returns null for root folder.
		/// </summary>
		new IFolder ParentFolder
		{ get; }

		/// <summary>
		///		Get data folders tree virtual root.
		/// </summary>
		IDataFolder RootDataFolder
		{ get; }

		/// <summary>
		///		Get predefined name of the child folder which contains all data files.
		///		The name cannot be used for repository folders.
		/// </summary>
		string DataFolderName
		{ get; }

		/// <summary>
		///		Get data folders explorer.
		/// </summary>
		IHistoricalFoldersExplorer DataFoldersExplorer
		{ get; }

		/// <summary>
		///		Get full path to the root data directory of this folder.
		/// </summary>
		string DataFolderFullPath
		{ get; }

		/// <summary>
		///		Path to the <see cref="RootDataFolder"/> relative to the repository root.
		/// </summary>
		string DataFolderRootRelativePath
		{ get; }

		/// <summary>
		///		Ensure child folders are loaded; eagerly load child folders; do nothing if already loaded.
		/// </summary>
		void EnsureChildFoldersLoaded();

		/// <summary>
		///		Get iterator over contained data files.
		/// </summary>
		/// <param name="backwards">
		///		Whether to initialize iterator in backward direction (chronologically).
		/// </param>
		/// <returns>
		///		New iterator.
		/// </returns>
		/// <remarks>
		///		This is a convenience method calling
		///		<see cref="IObjectFactory.GetDataFileIterator(IRepositoryFolder, bool)"/>
		/// </remarks>
		IDataFileIterator GetDataFileIterator(bool backwards);
	}
}
