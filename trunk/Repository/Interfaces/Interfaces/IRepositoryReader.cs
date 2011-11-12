//-----------------------------------------------------------------------------
// <created>1/25/2010 10:51:25 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Util;
using bfs.Repository.Storage;
using bfs.Repository.Events;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		Read one or more repository folders in chronological order.
	/// </summary>
	public interface IRepositoryReader : IRepositoryDataAccessor
	{
		/// <summary>
		///		The event is raised when an issue is detected during deferred position restoration.
		/// </summary>
		/// <remarks>
		///		This is a weak event, it does not hold strong references to the listeners allow them to be garbage collected.
		///		As a result the client does not have to unsubscribe from the event when last reference is removed.
		///		As a consequence, do not provide an anonymous method as an event handler that captures a variable.
		///		In this case, the delegate's target object is the closure, which can be immediately collected because there are no other references to it.
		///		<code>
		///			string localVariable = "Problem detected";
		///			reader.SeekStatus += delegate { Console.WriteLine(localVariable); };
		///		</code>
		///		Does not work in partial trust because it uses reflection on private methods.
		/// </remarks>
		event EventHandler<PositionRestoreStatusEventArgs> SeekStatus;

		/// <summary>
		///		Add <paramref name="folder"/> to the list of target folders.
		/// </summary>
		/// <param name="folder">
		///		The folder to read.
		/// </param>
		/// <returns>
		///		true if successfully added
		///		false if the folder is already being read by the reader
		/// </returns>
		/// <remarks>
		///		If seek (<see cref="Seek(System.DateTime)"/> or <see cref="Seek(IReadingPosition)"/>) has not yet been called or the end was reached after reading
		///		the folder reader is not opened or positioned. If <see cref="HasData"/> is <see langword="true"/>
		///		(reading is under way) the folder is immediately included
		///		into output stream and positioned to <see cref="NextItemTimestamp"/> (inclusive).
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		bool AddFolder(IRepositoryFolder folder);

		/// <summary>
		///		Add a folder to the list of folders being read and prepare it for reading from the specified position
		/// </summary>
		/// <param name="folder">
		///		Repository folder
		/// </param>
		/// <param name="seekTime">
		///		Seek timestamp for the folder
		/// </param>
		/// <returns>
		///		<see langword="false"/> - the folder is already being read
		///		<see langword="true"/> otherwise
		/// </returns>
		/// <remarks>
		///		Any subsequent Seek overrides the position used here
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		bool AddFolder(IRepositoryFolder folder, DateTime seekTime);

		/// <summary>
		///		Stop reading from the specified folder
		/// </summary>
		/// <param name="folder">
		///		The folder to exclude from the reading list.
		/// </param>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		void RemoveFolder(IRepositoryFolder folder);

		/// <summary>
		///		Add folder and restore its reading position. Folder is identified by its <see cref="IRepositoryFolder.FolderKey"/>
		///		, which is specified by <see cref="IFolderReadingPosition.FolderKey"/>
		/// </summary>
		/// <param name="folderPosition">
		///		Folder reading position
		/// </param>
		/// <returns>
		///		<see langword="true"/> - success
		///		<see langword="false"/> - folder is already being read
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		bool AddFolder(IFolderReadingPosition folderPosition);

		/// <summary>
		///		Get ready to read starting from the specified timestamp (see <paramref name="seekTime"/>)
		/// </summary>
		/// <param name="seekTime">
		///		The timestamp from which to start reading, inclusive
		/// </param>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		void Seek(DateTime seekTime);

		/// <summary>
		///		Restore reading position
		/// </summary>
		/// <param name="position">
		///		Position to restore
		/// </param>
		/// <remarks>
		///		The <paramref name="position"/> will usually contain positions for individual folders, in which case
		///		the reader will be closed and position will be restored completely; regardless of the state of the reader
		///		before the call to this method reader will get ready to read the folders from the position <paramref name="position"/>.
		///		However, the position may be used to store information about time and direction only
		///		(<see cref="IReadingPosition.ContainsFolderPositions"/> will return <see langword="false"/> in this case).
		///		In such scenario the reader will continue reading folders it was reading before the call to this method, only time
		///		and direction will be changed.
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		void Seek(IReadingPosition position);

		/// <summary>
		///		Read next available data item
		/// </summary>
		/// <returns>
		///		<see cref="IDataItemRead"/>
		///		<see langword="null"/> if the end is reached (<see cref="HasData"/> will return <see langword="false"/>)
		/// </returns>
		/// <remarks>
		///		The order of data items coming from multiple folders is guaranteed only if item timestamps are different.
		///		If more than one folder being read contains data item with a particular timestamp
		///		the order in which those data items are read may change next time you read same data from those same folders.
		///		Use <see cref="IRepositoryFolder.LastTimestamp"/> to position precisely every single data source.
		///		<seealso cref="IRepositoryReader.GetLastItemTimestamp"/>
		///		<seealso cref="IDataItemRead.RepositoryFolder"/>
		/// </remarks>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		IDataItemRead Read();

		/// <summary>
		///		Get last (logically, according to <see cref="Direction"/> data item timestamp in all folders.
		/// </summary>
		/// <returns>
		///		<code>System.DateTime</code>
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		DateTime GetLastItemTimestamp();

		/// <summary>
		///		Get the collection currently being the target of the reader.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		ICollection<IRepositoryFolder> Folders
		{ get; }

		/// <summary>
		///		Get next item (which will be returned by <see cref="Read()"/>) timestamp.
		///		Returns <see cref="TimeComparer.MaxValue"/> is there's no more data (<see cref="HasData"/>) or seek
		/// 	(<see cref="Seek(System.DateTime)"/> or <see cref="Seek(IReadingPosition)"/>) has not yet been called.
		/// </summary>
		DateTime NextItemTimestamp
		{ get; }

		/// <summary>
		///		Get whether there is data item to read.
		/// </summary>
		/// <remarks>
		///		Note that this property returns <see langword="false"/> before first call to  <see cref="Seek(System.DateTime)"/> or <see cref="Seek(IReadingPosition)"/>
		/// </remarks>
		bool HasData
		{ get; }

		/// <summary>
		///		The comparer to use for sorting data items with <b>equal</b> timestamps.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		Setting value after reader has been disposed.
		/// </exception>
		IComparer<IDataItem> DataItemComparer
		{ get; set; }

		/// <summary>
		///		Get the time comparer according to <see cref="Direction"/>
		/// </summary>
		IDirectedTimeComparison TimeComparer
		{ get; }

		/// <summary>
		///		Get or set reading direction (chronologically)
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		Setting value after reader has been disposed.
		/// </exception>
		EnumerationDirection Direction
		{ get; set; }

		/// <summary>
		///		Check whether <see cref="Direction"/> can be changed
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		///		The reader has been disposed.
		/// </exception>
		bool CanChangeDirection
		{ get; }

		/// <summary>
		///		Get current reading position.
		/// </summary>
		/// <remarks>
		///		This is a singleton property so if you save the reference and then call <see cref="Read()"/>
		///		the referenced instance will be updated. Call <see cref="ICloneable.Clone()"/> to get a copy of it.
		///		The returned object must be serializable.
		///		The value of this property is valid after a successful seek. Until that the value of this property is undefined.
		///		The position can be saved and later passed to <see cref="IObjectFactory.GetReader(IReadingPosition, EventHandler&lt;PositionRestoreStatusEventArgs&gt;)"/>
		///		or <see cref="Seek(IReadingPosition)"/> to resume interrupted reading.
		/// </remarks>
		IReadingPosition Position
		{ get; }
	}
}
