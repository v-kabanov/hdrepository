using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using bfs.Repository.Exceptions;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		Interface of an object implementing access to data in a repository data file.
	/// </summary>
	public interface IDataFileAccessor : IDisposable
	{
		/// <summary>
		/// 	Direct access to the list of data items.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// 	<see cref="OverrideMode" /> is OFF <see langword="false"/>
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// 	Setting <see langword="null"/> value.
		/// </exception>
		/// <remarks>
		/// 	The property never returns <see langword="null"/>.
		/// </remarks>
		IList<IDataItem> ItemListDirect
		{ get; set; }

		/// <summary>
		///		Get the number of data items currently in the file buffer.
		/// </summary>
		int ItemCount
		{ get; }

		/// <summary>
		///		Get or set compressor to be used when saving or reading the target file.
		/// </summary>
		ICoder Coder
		{ get; set; }

		/// <summary>
		/// 	Whether the list of data items is sorted by timestamp.
		/// </summary>
		/// <remarks>
		/// 	This flag is not maintained when accessing and modifying the <see cref="ItemListDirect"/>.
		/// 	The accessor must be in override mode (<see cref="OverrideMode" />) to be able to set this property.
		/// </remarks>
		bool IsSorted
		{ get; set; }

		/// <summary>
		///		Get or set encryptor to be used when saving or reading the target file.
		/// </summary>
		ICoder Encryptor
		{ get; set; }

		/// <summary>
		///		Instructs the accessor to reject items with timestamp less than the specified date-time.
		/// </summary>
		DateTime MinTimestampToAccept
		{ get; set; }

		/// <summary>
		///		Instructs the accessor to reject items with timestamp greater than the specified value. Inclusive.
		/// </summary>
		DateTime MaxTimestampToAccept
		{ get; set; }

		/// <summary>
		/// 	Get path of the currently open file as it would be after flushing the accessor (<see cref="Flush()"/>)
		/// </summary>
		string FilePath
		{ get; }

		/// <summary>
		/// 	Get or set whether the accessor is in a mode allowing to manually override certain functions and restrictions.
		/// </summary>
		/// <remarks>
		/// 	When the mode is ON:
		/// 	- <see cref="Sort()" /> will sort the list of items regardless of the <see cref="IsSorted" />;
		/// 	- <see cref="ItemListDirect" /> can be used to access and modify the list of items directly;
		/// </remarks>
		bool OverrideMode
		{ get; set; }

		/// <summary>
		///		Get path of the target file as it exists on disk.
		/// </summary>
		/// <remarks>
		///		File name may change when its content changes; when updated content is flushed to disk old file is replaced with new file having [potentially]
		///		different name.
		/// </remarks>
		string ExistinglFilePath
		{ get; }

		/// <summary>
		///		Get chronologically first data item in the current file.
		/// </summary>
		/// <remarks>
		///		The data items collection must be sorted when the property is accessed, <see cref="Sort()"/>.
		///		File may not exist on disk until data is flushed (<see cref="Flush()"/>).
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The data items collection is not sorted (<see cref="IsSorted"/> returns false).
		/// </exception>
		IDataItem FirstItem
		{ get; }

		/// <summary>
		///		Get chronologically last data item in the current file.
		/// </summary>
		/// <remarks>
		///		The data items collection must be sorted when the property is accessed, <see cref="Sort()"/>.
		///		File may not exist on disk until data is flushed (<see cref="Flush()"/>).
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		///		The data items collection is not sorted (<see cref="IsSorted"/> returns false).
		/// </exception>
		IDataItem LastItem
		{ get; }

		/// <summary>
		///		Get timestamp of the chronologically first data item in the file.
		/// </summary>
		/// <remarks>
		///		Does not require data collection to be sorted.
		/// </remarks>
		DateTime FirstItemTimestamp
		{ get; }

		/// <summary>
		///		Get timestamp of the chronologically last data item in the file.
		/// </summary>
		/// <remarks>
		///		Does not require data collection to be sorted.
		/// </remarks>
		DateTime LastItemTimestamp
		{ get; }

		/// <summary>
		///		Get target file descriptor.
		/// </summary>
		/// <remarks>
		///		Note that the descriptor contains current file name as it exists on disk or as it was instantiated.
		///		There may be in-memory changes to the file content which will alter the name when the accessor is flushed.
		/// </remarks>
		IRepositoryFileName TargetFile
		{ get; }

		/// <summary>
		///		Get or set comparer to use when sorting data items for items with equal timestamps.
		///		When timestamps are not equal the comparer has no effect.
		/// </summary>
		IComparer<IDataItem> EqualTimestampItemsComparer
		{ get; set; }

		/// <summary>
		///		Get read only collection of all contained items in chronological order.
		/// </summary>
		/// <returns>
		///		Read-only collection of data items which may reflect further updates to the data items collection contained in
		///		this instance of file accessor.
		/// </returns>
		/// <remarks>
		/// 	The collection is sorted if necessary if <see cref="OverrideMode" /> is OFF. Otherwise it will be returned as is.
		/// </remarks>
		IList<IDataItem> GetAllItems();

		/// <summary>
		///		Add data item to the file buffer; no IO involved
		/// </summary>
		/// <param name="dataItem"></param>
		/// <returns>
		///		<see langword="true"/> - successfully added
		///		<see langword="false"/> - item's timestamp does fall into configured datetime range
		///		<see cref="MinTimestampToAccept"/>, <see cref="MaxTimestampToAccept"/>
		/// </returns>
		bool Add(IDataItem dataItem);

		/// <summary>
		///		Flush unsaved changes.
		/// </summary>
		/// DOCO:
		/// <remarks>
		///		If the number of items is zero, the file is deleted from disk.
		///		If saving over existing file it first gets deleted and then saved. Deletion and addition are reported to the corresponding
		///		file container browser to synchronise the files collection. Any discrepancies (e.g. when deleting existing and not finding it
		///		on disk or in the file container's collection result in exception. Data access should be stopped as soon as possible
		///		to prevent data corruption in such cases.
		/// 	The collection of data items is sorted if necessary if <see cref="OverrideMode" /> is OFF. Otherwise the caller is
		/// 	responsible for sorting the collection (may call <see cref="Sort()" /> when necessary).
		/// 	When file system supports transactions and their use is not prohibited by the repository settings (<see cref="IRepositoryManager.Settings"/>)
		/// 	flushing should be made atomic.
		/// </remarks>
		/// <exception cref="FileContainerNotificationException">
		///		The file cannot be found in the container; possible concurrency issue.
		/// </exception>
		void Flush();

		/// <summary>
		///		Close the accessor.
		/// </summary>
		/// <remarks>
		///		Discarding changes if dirty. Call <see cref="Flush"/> to save changes.
		/// </remarks>
		void Close();

		/// <summary>
		///		Read data from disk.
		/// </summary>
		/// <exception cref="System.IO.FileNotFoundException">
		///		<see cref="ExistingFilePath"/> is <see langword="null"/>;
		///		Target file (<see cref="ExistingFilePath"/>) does not exist.
		/// </exception>
		void ReadFromFile();

		/// <summary>
		/// 	Sort the list of data items by their timestamp (and by <see cref="EqualTimestampItemsComparer" /> if set) if it is not
		/// 	sorted <see cref="IsSorted" /> is <see langword="false"/> or if <see cref="OverrideMode" /> is ON.
		/// </summary>
		/// <remarks>
		/// 	Note that in manual mode call to this method will force sorting.
		/// </remarks>
		void Sort();

		/// <summary>
		///		Check whether the specified timestamp is within the range of contained data.
		/// </summary>
		/// <param name="itemTimestamp">
		///		The data item timestamp
		/// </param>
		/// <returns>
		///		true if te file contains at least 2 data items and the specified timestamp
		///			falls in the datetime range in between first and last contained data items
		///			inclusive
		///		false otherwise
		/// </returns>
		/// <remarks>
		///		If this method returns true the data item with the specified timestamp
		///		must be stored in this file
		/// </remarks>
		bool IsTimestampCovered(DateTime itemTimestamp);
	}
}
