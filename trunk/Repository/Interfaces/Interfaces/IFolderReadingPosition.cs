using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Util;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		The interface of a reading position for a single folder.
	/// </summary>
	/// <remarks>
	///		Reading position is normally represented by last read item rather than next item; this is to allow data to change
	///		outside of what is read in addition to other reasons. However, when after initial Seek by time only (rather than
	///		position) no data was read (either did not exist yet or existed but was not read) the only attribute of the position
	///		becomes seek time and the position indicates that data with the specified timestamp was not read.
	///		(<see cref="IFolderReadingPosition.IsExact"/> will be false).
	///		The folder reading position is not used on its own, only as part of <see cref="IReadingPosition"/>, therefore
	///		it does not contain information about direction.
	/// </remarks>
	public interface IFolderReadingPosition : ICloneable
	{
		/// <summary>
		///		Get the position timestamp.
		/// </summary>
		/// <remarks>
		///		This is either timestamp of last read data item (<see cref="IsExact"/> is <see langword="true"/>)
		///		or seek timestamp  (<see cref="IsExact"/> is <see langword="false"/>)
		/// </remarks>
		DateTime Time
		{ get; }

		/// <summary>
		///		Get status of the position timestamp.
		/// </summary>
		/// <remarks>
		///		Indicates whether data item with the timestamp was read or just sought. If <see langword="false"/> it means no data items
		///		with the timestamp equal to <see cref="IFolderReadingPosition.Time"/> have yet been read; so <see cref="NumberOfItemsWithTheTimestampRead"/>
		///		is equal to <code>0</code> then.
		/// </remarks>
		bool IsExact
		{ get; }

		/// <summary>
		///		Get number of data items with the position timestamp (<see cref="Time"/>) read by the reader.
		/// </summary>
		int NumberOfItemsWithTheTimestampRead
		{ get; }

		/// <summary>
		///		Get hash code of last read data item to verify position when restoring. This should be stable persistent
		///		business key. If you do not wish this verification to happen just return zero.
		/// </summary>
		int VerificationLastReadItemHash
		{ get; }

		/// <summary>
		///		Get normalised [relative] path of the target folder (<see cref="IRepositoryFolder.FolderKey"/>)
		/// </summary>
		string FolderKey
		{ get; }
	}
}
