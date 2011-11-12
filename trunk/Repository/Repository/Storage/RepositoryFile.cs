using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.Diagnostics;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Util;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		This class represents repository file.
	/// </summary>
	/// <remarks>
	///		The class adds location awareness to <see cref="RepositoryFileName"/>. It is a wrapper around <see cref="IRepositoryFileName"/>
	///		and the containing <see cref="IDataFolder"/> and does not have a factory method in <see cref="IObjectFactory"/>.
	///		To create functional instance
	/// </remarks>
	[DebuggerDisplay("{Path}, exists = {Exists}")]
	public class RepositoryFile : IRepositoryFile
	{
		/// <summary>
		///		Create new instance.
		/// </summary>
		/// <param name="containingFolder">
		///		Leaf data folder object reference, mandatory.
		/// </param>
		/// <param name="fileName">
		///		Object representing data file name, optional.
		/// </param>
		public RepositoryFile(IDataFolder containingFolder, IRepositoryFileName fileName)
		{
			Check.DoRequireArgumentNotNull(containingFolder, "containingFolder");
			RepositoryFolder.CheckNotDetached(containingFolder.RepoFolder);

			ContainingFolder = containingFolder;
			Name = fileName;
		}

		/// <summary>
		///		Get file name object.
		/// </summary>
		public IRepositoryFileName Name { get; private set; }

		/// <summary>
		///		Get containing data folder object.
		/// </summary>
		public IDataFolder ContainingFolder { get; private set; }

		/// <summary>
		///		Get repository's file frovider.
		/// </summary>
		public IFileProvider FileProvider
		{ get { return ContainingFolder.RepoFolder.Repository.ObjectFactory.FileSystemProvider.FileProvider; } }

		/// <summary>
		///		Get full path to the file.
		/// </summary>
		public string Path
		{ get { return System.IO.Path.Combine(ContainingFolder.FullPath, Name.FileName); } }

		/// <summary>
		///		Whether the file exists on disk.
		/// </summary>
		public bool Exists
		{ get { return this.FileProvider.Exists(this.Path); } }

		/// <summary>
		///		Delete the file from repository and disk.
		/// </summary>
		/// <remarks>
		///		Deletes from disk and then notifies the containing folder.
		/// </remarks>
		public void Delete()
		{
			this.FileProvider.Delete(this.Path);
			ContainingFolder.DataFileBrowser.FileDeleted(Name.FirstItemTimestamp);
		}

		/// <summary>
		///		Copy file to the specified location.
		/// </summary>
		/// <param name="newPath">
		///		New file path.
		/// </param>
		/// <param name="overwrite">
		///		Whether to overwrite existing file if any.
		/// </param>
		public void Copy(string newPath, bool overwrite)
		{
			this.FileProvider.Copy(this.Path, newPath, overwrite);
		}

		/// <summary>
		///		Get next data file in the same repo folder.
		/// </summary>
		/// <param name="backwards">
		///		The direction in which to look for data file relative to this file: to the past or to the future
		/// </param>
		/// <returns>
		///		Next data file or <see langword="null"/> if none exists.
		/// </returns>
		public IRepositoryFile GetNext(bool backwards)
		{
			IRepositoryFile retval = null;

			IRepositoryFileName fileInSameFolder = ContainingFolder.GetNextDataFile(this.Name, backwards);

			if (null != fileInSameFolder)
			{
				retval = new RepositoryFile(containingFolder: ContainingFolder, fileName: fileInSameFolder);
			}
			else
			{
				// scanning sibling leaf folders until first data file is found
				for (
					IDataFolder nextFolder = ContainingFolder.GetNextSiblingInTree(backwards);
					nextFolder != null && retval == null;
					nextFolder = nextFolder.GetNextSiblingInTree(backwards))
				{
					retval = nextFolder.FindFirstDataFile(backwards);
				}
			}

			return retval;
		}
	}
}
