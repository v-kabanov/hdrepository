//-----------------------------------------------------------------------------
// <created>1/25/2010 10:55:03 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using bfs.Repository.Events;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Writer can write to a subtree with target folder at its root.
	/// </summary>
	/// <remarks>
	///		When writer is closed any unsaved data is automatically flushed to disk. The writer becomes unable to recieve data.
	///		To continue writing into the same folder create another instance.
	/// </remarks>
	public interface IRepositoryWriter : IRepositoryDataAccessor
	{
		/// <summary>
		///		Get target root folder. Returns <see langword="null"/> when disposed.
		/// </summary>
		IRepositoryFolder Folder
		{ get; }

		/// <summary>
		///		Whether the writer can accept data
		/// </summary>
		/// <remarks>
		///		Writers must be registered with the repository manager (<see cref="IRepositoryDataAccessor.Repository"/>) before
		///		accepting any data.
		/// </remarks>
		bool IsOpen
		{ get; }

		/// <summary>
		///		Get or set data items router which maps data items to subfolders.
		///		If not set, <see cref="IDataItem.RelativePath"/> is used.
		/// </summary>
		IDataRouter DataRouter
		{ get; set; }

		/// <summary>
		///		Get or set boolean value indicating whether new repo subfolders should be created on the fly according to <see cref="DataRouter"/>
		///		or <see cref="Interfaces.IDataItem.RelativePath"/>.
		/// </summary>
		/// <remarks>
		///		If this setting is <see langword="false"/> and a data item is submitted with relative path which points to a folder which does not exist
		///		an exception is thrown. Default setting is <see langword="false"/>.
		/// </remarks>
		bool AllowSubfoldersCreation
		{ get; set; }

		/// <summary>
		///		Get or set comparer to use when sorting data items for items with equal timestamps.
		///		When timestamps are not equal the comparer has no effect.
		/// </summary>
		IComparer<IDataItem> EqualTimestampedItemsComparer
		{ get; set; }

		/// <summary>
		///		Get or set whether to keep track of unsaved data items to be able to retrieve them.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		The value is set after some data is written.
		/// </exception>
		/// <remarks>
		///		The option must be set before writing any data.
		///		Default setting is <see langword="true"/>.
		/// </remarks>
		bool TrackUnsavedItems
		{ get; set; }

		/// <summary>
		///		Write data item to repository
		/// </summary>
		/// <param name="dataItem">
		///		Data item to write. Must be serializable.
		/// </param>
		/// <remarks>
		///		The <paramref name="dataItem"/> needs to be serializable employing either automatic or custom serialization. Automatic serialization
		///		requires only <see cref="SerializableAttribute"/>, but it may be difficult to implement versioning. With custom serialization
		///		the class has to implement the <see cref="System.Runtime.Serialization.ISerializable"/> interface and a special constructor. Note that no immediate check is performed
		///		in this method and if the <paramref name="dataItem"/> is not serializable the subsequent flushing of the data to disk may fail.
		///		Not thread safe, must not be used in more than 1 thread at once.
		/// </remarks>
		void Write(IDataItem dataItem);

		/// <summary>
		///		Flush all unsaved data to disk.
		/// </summary>
		void Flush();

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
		IDictionary<string, IList<IDataItem>> GetUnsavedItems();

		/// <summary>
		///		Event is raised every time a new data item is written (added) through this writer.
		/// </summary>
		event EventHandler<DataItemAddedEventArgs> ItemAdded;
	}
}
