using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		The interface describes operations with data in a single database folder. File structure is revealed.
	///		Data can be read and written at the same time. This is suitable for data updates.
	/// </summary>
	public interface IFolderDataAccessor : IRepositoryDataAccessor
	{
		/// <summary>
		///		Get the target repository folder
		/// </summary>
		IRepositoryFolder Folder
		{ get; }

		ICoder Coder
		{ get; set; }

		ICoder Encryptor
		{ get; set; }

		/// <summary>
		///		Get or set default direction used in 
		/// </summary>
		Util.EnumerationDirection DefaultDirection
		{ get; set; }

		/// <summary>
		///		Get or set boolean value indicating whether to sort data items in the current file by timestamp before saving.
		///		Note that if this setting is <see langword="false"/> it's the responsibility of the caller to ensure the order
		///		is not broken; if the order is broken it may result in incorrect file naming and overlapping with other files;
		///		as a result, the integrity of the repository may be compromised. Default setting is <see langword="true"/>.
		/// </summary>
		bool SortBeforeSave
		{ get; set; }

		/// <summary>
		///		Get current repository data file
		/// </summary>
		IRepositoryFile CurrentFile
		{ get; }

		/// <summary>
		///		Get next (chronologically) existing repository file
		/// </summary>
		IRepositoryFile NextNewestFile
		{ get; }

		/// <summary>
		///		Get previous (chronologically) existing repository file
		/// </summary>
		IRepositoryFile NextOldestFile
		{ get; }

		/// <summary>
		///		Collection of data items in the currently opened file.
		/// </summary>
		IList<IDataItem> DataItems
		{ get; }

		/// <summary>
		///		Sort data items in the current file.
		/// </summary>
		void SortItemsInCurrentFile();

		/// <summary>
		///		Write data item into the folder
		/// </summary>
		/// <param name="dataItem">
		///		Data item to store in the repository folder
		/// </param>
		/// <remarks>
		///		This can change <see cref="CurrentFile"/>
		///		Use <see cref="AddToCurrentFile"/> to ensure writing to the current file (<see cref="CurrentFile"/>)
		/// </remarks>
		void Add(IDataItem dataItem);

		void AddToCurrentFile(IDataItem dataItem);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataItem"></param>
		void AddToNewFile(IDataItem dataItem);

		/// <summary>
		///		Remove data item from the current data file (<see cref="CurrentFile"/>)
		/// </summary>
		/// <param name="index">
		///		Index of data item in <see cref="DataItems"/> to delete
		/// </param>
		void RemoveDataItem(int index);

		void RemoveDatItems(int startIndex, int count);

		/// <summary>
		///		
		/// </summary>
		/// <param name="seekTimestamp">
		/// </param>
		void Seek(DateTime seekTimestamp);

		/// <summary>
		///		Open <see cref="NextFile"/> and make it current
		/// </summary>
		void OpenNextFile();

		/// <summary>
		///		Open <see cref="PreviousFile"/> and make it current
		/// </summary>
		void OpenPreviousFile();

		/// <summary>
		///		Clear all data from the current file.
		/// </summary>
		/// <remarks>
		///		If you call <see cref="Flush"/> immediately after this method the underlying data file
		///		(<see cref="CurrentFile"/>) will be deleted from disk.
		/// </remarks>
		void ClearCurrentFile();

		/// <summary>
		///		Delete the specified data file belonging to this directory.
		/// </summary>
		/// <param name="file">
		///		File to delete.
		/// </param>
		void DeleteFile(IRepositoryFileName file);

		/// <summary>
		///		Save current file
		/// </summary>
		void Flush();
	}
}
