//-----------------------------------------------------------------------------
// <copyright file="IRepositoryManager.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>1/25/2010 10:12:36 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Root repository interface.
	/// </summary>
	public interface IRepositoryManager : IDisposable
	{
		/// <summary>
		///		Get root repository folder.
		/// </summary>
		/// <remarks>
		///		The root folder cannot be deleted or renamed (its name is empty string), it has no parent.
		/// </remarks>
		IRepositoryFolder RootFolder
		{ get; }

		/// <summary>
		///		Get repository root path.
		/// </summary>
		/// <remarks>
		///		The path points to folder which contains entire repository. Thus to back up the repository one needs to xcopy recursively the repository root.
		/// </remarks>
		string RepositoryRoot
		{ get; }

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
		///		<see langword="true"/> if there are active readers or writers to the specified folder[s];
		///		<see langword="false"/> otherwise.
		/// </returns>
		/// <remarks>
		///		Access to readers and writers registry is synchronised; all concurrent calls accessing registry of readers and writers
		///		will wait until this method finishes.
		/// </remarks>
		bool IsDataBeingAccessed(IRepositoryFolder folder, bool subtree);

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
		/// </remarks>
		bool IsDataBeingWrittenTo(IRepositoryFolder folder, bool subtree);

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
		/// </remarks>
		bool IsDataBeingReadFrom(IRepositoryFolder folder, bool subtree);

		/// <summary>
		///		Get existing writers into either the specified folder or any of its descendants, depending on the parameter
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
		///		The list of existing writers, never <see langword="null"/>
		/// </returns>
		/// <exception cref="ArgumentException">
		///		The <paramref name="folder"/> does not belong to this repository
		/// </exception>
		IList<IRepositoryDataAccessor> GetWriters(IRepositoryFolder folder, bool subtree);

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
		IList<IRepositoryDataAccessor> GetReaders(IRepositoryFolder folder, bool subtree);
		
		/// <summary>
		/// 	Get or set non-persistent settings.
		/// </summary>
		IRepositorySettings Settings
		{ get; set; }

		/// <summary>
		///		Whether the instance has been disposed.
		/// </summary>
		bool IsDisposed
		{ get; }
	}
}
