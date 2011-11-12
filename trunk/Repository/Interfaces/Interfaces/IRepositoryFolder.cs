//-----------------------------------------------------------------------------
// <created>1/25/2010 10:50:21 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces
{
	public interface IRepositoryFolder
	{
		/// <summary>
		///		The name must contain at least 1 letter (alpha character)
		/// </summary>
		string Name
		{ get; }

		/// <summary>
		///		Get physical path
		/// </summary>
		string FullPath
		{ get; }

		/// <summary>
		///		Get the path to the folder relative to repo root. Uses '/' as path separator.
		/// </summary>
		/// <remarks>
		///		Case-insensitive.
		/// </remarks>
		string LogicalPath
		{ get; }

		/// <summary>
		///		Get parent folder. If the property returns <see langword="null"/> then this is the repository root. User folders
		///		(created with <see cref="CreateSubfolder"/>) will always have a parent.
		/// </summary>
		IRepositoryFolder ParentFolder
		{ get; }

		/// <summary>
		///		Get read-only collection of all child folders.
		/// </summary>
		ICollection<IRepositoryFolder> SubFolders
		{ get; }

		/// <summary>
		///		Get last item timestamp; no recursion.
		/// </summary>
		/// <returns>
		///		DateTime.MinValue if no data
		/// </returns>
		DateTime LastTimestamp
		{ get; }

		/// <summary>
		///		Get first item timestamp; no recursion.
		/// </summary>
		/// <returns>
		///		DateTime.MinValue if no data
		/// </returns>
		DateTime FirstTimestamp
		{ get; }

		/// <summary>
		///		Get repository instance to which the folder belongs.
		/// </summary>
		IRepositoryManager Repository
		{ get; }

		/// <summary>
		///		Get folder persistent configuration
		/// </summary>
		IFolderProperties Properties
		{ get; }

		/// <summary>
		///		Get boolean value indicating whether child folders are loaded.
		/// </summary>
		/// <seealso cref="LoadSubfolders(bool, bool, bool)"/>
		/// <seealso cref="UnloadSubfolders"/>
		bool SubfoldersLoaded
		{ get; }

		/// <summary>
		///		Check whether this folder is attached to a repository
		/// </summary>
		bool IsDetached
		{ get; }

		/// <summary>
		///		Get normalized <see cref="LogicalPath"/>, the string which will be a unique and stable identifier
		///		of the folder in its repository.
		/// </summary>
		/// <remarks>
		///		If the folder is renamed in a way that does not change the target folder (such as when only character
		///		casing changes) the returned value must not change. Folder keys should be case-insensitive, in line
		///		with folder naming convention.
		/// </remarks>
		string FolderKey
		{ get; }

		/// <summary>
		///		Whether the underlyinig file system directory exists.
		/// </summary>
		bool Exists
		{ get; }

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
		IRepositoryFolder CreateSubfolder(string name);

		/// <summary>
		///		Get existing subfolder.
		/// </summary>
		/// <param name="name">
		///		Subfolder name, case insensitive
		/// </param>
		/// <returns>
		///		<see langword="null"/> if not found
		/// </returns>
		IRepositoryFolder GetSubFolder(string name);

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
		IRepositoryFolder GetDescendant(string relativePath, bool createIfMissing);

		/// <summary>
		///		Change the folder name.
		/// </summary>
		/// <param name="newName">
		///		New name.
		/// </param>
		/// <remarks>
		///		Renaming cannot be done while data in any of its descendant is being accessed.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		This folder is repository root (<see cref="IRepositoryManager.RootFolder"/>).
		/// </exception>
		/// <exception cref="ConcurrencyException">
		///		 Data in any of this folder's descendants is being accessed.
		/// </exception>
		/// <exception cref="ArgumentException">
		///		A folder with the name <paramref name="newName"/> already exists.
		/// </exception>
		void Rename(string newName);

		/// <summary>
		///		Get reader from this folder or repository subtree (with this folder as its root)
		/// </summary>
		/// <param name="recursive">
		///		Whether to read all subfolders
		/// </param>
		/// <param name="startPosition">
		///		The point in time from which to start reading; inclusive.
		/// </param>
		/// <returns>
		///		A <see cref="IRepositoryReader"/> ready to return data items.
		/// </returns>
		IRepositoryReader GetReader(DateTime startPosition, bool recursive);

		/// <summary>
		///		Get <see cref="IRepositoryFolder"/> instance pointing to
		///		this directory as target root folder. Data items may go to descendants folders,
		///		<see cref="bfs.Repository.Interfaces.IDataItem.RelativePath"/>
		/// </summary>
		/// <returns>
		///		New instance of <see cref="IRepositoryFolder"/>
		/// </returns>
		IRepositoryWriter GetWriter();

		/// <summary>
		///		Synchronise the state of the object with underlying repository; read from disk all subfolders,
		///		and all data folders (recursive, lazy)
		/// </summary>
		/// <remarks>
		///		This is same as <code>Refresh(true, false)</code>
		/// </remarks>
		void Refresh();

		/// <summary>
		///		Synchronise the state of the object with underlying repository. Refresh both contents and children/descendants.
		/// </summary>
		/// <param name="recursive">
		///		Whether to refresh all descendants as well.
		/// </param>
		/// <param name="eager">
		///		Whether to load subfolders if not yet loaded. If <see langword="false"/> subfolders collection will be loaded on demand
		///		if not already loaded.
		/// </param>
		/// <remarks>
		///		If <paramref name="recursive"/> is <see langword="false"/> only this folder's structure (data and child folders) is re-read
		///		from disk. The method preserves subfolders collection and instances of subfolders which are not deleted from disk.
		///		Folder metadata (<see cref="Properties"/> is also re-loaded from disk.
		///		Note that if <paramref name="recursive"/> is <see langword="false"/>, child folders' content is not reloaded (for those
		///		child folders which are already loaded).
		///		Otherwise content is reloaded for all descendants as well as this folder.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		/// <seealso cref="LoadSubfolders(bool, bool, bool)"/>
		/// <seealso cref="UnloadSubfolders()"/>
		void Refresh(bool recursive, bool eager);

		/// <summary>
		///		Delete this directory
		/// </summary>
		/// <param name="recursive">
		///		if this parameter is false and there are subfolders
		///		an exception is thrown
		/// </param>
		/// <param name="deleteUnknown">
		///		Whether to remove extra files/folders found in the folder
		/// </param>
		/// <remarks>
		///		Client should not attempt to use the instance of the folder
		///		for data access after calling this method. If this method
		///		throws an exception the client may retry deletion.
		/// </remarks>
		void Delete(bool recursive, bool deleteUnknown);

		/// <summary>
		///		Move this folder into another folder
		/// </summary>
		/// <param name="newParentFolder">
		///		New parent folder
		/// </param>
		/// <remarks>
		///		If IO operation fails the state of the folders should remain valid.
		/// </remarks>
		void Move(IRepositoryFolder newParentFolder);

		/// <summary>
		///		Add this folder to the target folders list of the specified <paramref name="reader"/>.
		/// </summary>
		/// <param name="reader">
		///		The reader to include this folder into its target list
		/// </param>
		/// <param name="recursive">
		///		Whether to include all subfolders as well
		/// </param>
		void AddToReader(IRepositoryReader reader, bool recursive);

		/// <summary>
		///		Get first item timestamp. <seealso cref="LastTimestamp"/>
		/// </summary>
		/// <param name="recursive">
		///		Whether to include all subfolders
		/// </param>
		/// <param name="fromEnd">
		///		Whether to find first item from start (<see langword="false"/>) or end (<see langword="true"/>)
		/// </param>
		/// <returns>
		///		<see cref="DateTime.MinValue"/> if no data
		/// </returns>
		DateTime GetFirstItemTimestamp(bool recursive, bool fromEnd);

		/// <summary>
		///		[Re]loading subfolders, their data and metadata
		/// </summary>
		/// <param name="reloadIfLoaded">
		///		Whether to reload subfolders if they are already loaded. Use <see langword="true"/> to pick up external changes.
		/// </param>
		/// <param name="recursive">
		///		Whether to load all descendants rather than immediate children only
		/// </param>
		/// <param name="refreshContent">
		///		Whether to refresh content of already loaded folders when reloading (those newly loaded will be up to date regardless).
		///		Contents include metadata (i.e. <see cref="Properties"/>) and data. Note that if <paramref name="reloadIfLoaded"/>
		///		is false the content will not be refreshed: it only has an effect when reloading already loaded folders.
		/// </param>
		/// <remarks>
		///		use cases:
		///			- just make sure that CHILD folders are synced eagerly: LoadSubfolders(false, false, false)
		///			- pick up added externally CHILD folders or purge deleted eagerly: LoadSubfolders(true, false, false)
		///			- regardless of current state fully load into memory whole subtree to minimise latency during subsequent calls:
		///				LoadSubfolders(true, true, true)
		///		To just make sure that CHILD folders are synced lazily use <see cref="UnloadSubfolders()"/>.
		/// </remarks>
		void LoadSubfolders(bool reloadIfLoaded, bool recursive, bool refreshContent);

		/// <summary>
		///		Refresh ([re-]read) contents of this folder from disk.
		///		(<see cref="SubfoldersLoaded"/> returns <see langword="false"/>), <paramref name="recursive"/> has no effect.
		/// </summary>
		/// <param name="recursive">
		///		Whether to refresh content of <b>loaded</b> descendant folders recursively.
		/// </param>
		/// <remarks>
		///		Contents include metadata (i.e. <see cref="Properties"/>) and data.
		///		The subfolders collection is not reloaded.
		///		That is, externally created subfolders will not be loaded; if child folders are not loaded.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		void RefreshContent(bool recursive);

		/// <summary>
		///		Causes deferred refresh of subfolders. After this call <see cref="SubfoldersLoaded"/> returns <see langword="false"/>
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		void UnloadSubfolders();

		/// <summary>
		///		Check whether this folder is a descendant of the specified folder; i.e. whether this folder belongs to
		///		the subtree with <paramref name="folder"/> at its root (including <paramref name="folder"/> itself)
		/// </summary>
		/// <param name="folder">
		///		A folder belonging to the same repository
		/// </param>
		/// <returns>
		///		<see langword="true"/> if this folder is a descendant of the <paramref name="folder"/>
		///		<see langword="false"/> otherwise
		/// </returns>
		/// <remarks>
		/// </remarks>
		/// <exception cref="Exceptions.ApplicationException">
		///		The folder's key (<see cref="FolderKey"/>) is inconsistent with that of its ancestor.
		/// </exception>
		/// <exception cref="DifferentRepositoriesException">
		///		<paramref name="folder"/> belongs to a different repository.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///		The folder is detached.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///		The repository is disposed.
		/// </exception>
		bool IsDescendantOf(IRepositoryFolder folder);
	}
}
