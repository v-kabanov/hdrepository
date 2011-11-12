using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;
using bfs.Repository.Util;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Reading position of an individual repository folder.
	/// </summary>
	/// <remarks>
	///		Reading position is normally represented by last read item rather than next item; this is to allow data to change
	///		outside of what is read in addition to other reasons. However, when after initial Seek by time only (rather than
	///		position) no data was read (either did not exist yet or existed but was not read) the only attribute of the position
	///		becomes seek time and the position indicates that data with the specified timestamp was not read
	///		(<see cref="IFolderReadingPosition.IsExact"/>).
	/// </remarks>
	[Serializable]
	//TODO: custom serialization with versioning
	[DebuggerDisplay("{FolderKey}/{Time}[{NumberOfItemsWithTheTimestampRead}]")]
	public class FolderReadingPosition : IFolderReadingPosition, ISerializable
	{
		private static readonly Version _version = new Version(0, 9, 1, 20110712);
		private const string serializationFieldNameVersion = "version";
		private const string serializationFieldNameNumberOfItems = "numberOfItems";
		private const string serializationFieldNameFolderKey = "folderKey";
		private const string serializationFieldNameVerificationHash = "verificationHash";
		private const string serializationFieldNamePositionTime = "posTime";

		protected FolderReadingPosition(SerializationInfo info, StreamingContext context)
		{
			Version version = (Version)info.GetValue(serializationFieldNameVersion, typeof(Version));
			if (version.CompareTo(_version) > 0)
			{
				// formatting parameters: component name, expected, then found versions
				throw new SerializationException(string.Format(
					StorageResources.IncompatibleVersion, typeof(FolderReadingPosition).Name, _version, version));
			}
			this.FolderKey = info.GetString(serializationFieldNameFolderKey);
			this.NumberOfItemsWithTheTimestampRead = info.GetInt32(serializationFieldNameNumberOfItems);
			this.Time = info.GetDateTime(serializationFieldNamePositionTime);
			this.VerificationLastReadItemHash = info.GetInt32(serializationFieldNameVerificationHash);
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(serializationFieldNameVersion, _version, typeof(Version));
			info.AddValue(serializationFieldNameFolderKey, FolderKey, typeof(string));
			info.AddValue(serializationFieldNameNumberOfItems, NumberOfItemsWithTheTimestampRead, typeof(int));
			info.AddValue(serializationFieldNamePositionTime, Time, typeof(DateTime));
			info.AddValue(serializationFieldNameVerificationHash, VerificationLastReadItemHash, typeof(int));
		}

		public FolderReadingPosition(string folderKey)
		{
			this.FolderKey = folderKey;
		}

		/// <summary>
		///		Create and initialise new empty position
		/// </summary>
		/// <param name="folder"></param>
		public FolderReadingPosition(IRepositoryFolder folder)
			: this(folderKey: folder.FolderKey)
		{
		}

		/// <summary>
		///		Create new instance and initialise it with seek timestamp
		/// </summary>
		/// <param name="folder">
		///		Target folder being read
		/// </param>
		/// <param name="seekTime">
		///		Seek time (<see cref="IRepositoryReader.Seek(System.DateTime)"/>)
		/// </param>
		public FolderReadingPosition(IRepositoryFolder folder, DateTime seekTime)
			: this(folder)
		{
			FromSeek(seekTime);
		}

		/// <summary>
		///		Create new instance and populate it with existing position data.
		/// </summary>
		/// <param name="position">
		///		Position to copy
		/// </param>
		public FolderReadingPosition(IFolderReadingPosition position)
		{
			FromSeek(position);
		}

		/// <summary>
		///		Get position version.
		/// </summary>
		public static Version PositionVersion
		{ get { return _version; } }

		/// <summary>
		///		Get the position timestamp.
		/// </summary>
		/// <remarks>
		///		This is either timestamp of last read data item (<see cref="IsExact"/> is <see langword="true"/>)
		///		or seek timestamp  (<see cref="IsExact"/> is <see langword="false"/>)
		/// </remarks>
		public DateTime Time
		{ get; set; }

		/// <summary>
		///		Get status of the position timestamp.
		/// </summary>
		/// <remarks>
		///		Indicates whether data item with the timestamp was read or just sought. If <see langword="false"/> it means no data items
		///		with the timestamp equal to <see cref="IFolderReadingPosition.Time"/> have yet been read; so <see cref="NumberOfItemsWithTheTimestampRead"/>
		///		is irrelevant then.
		/// </remarks>
		//[NonSerialized]
		public bool IsExact
		{
			get { return NumberOfItemsWithTheTimestampRead > 0; }
		}

		/// <summary>
		///		Get ordinal of last read item in the sequence of the previous items with the same timestamp
		/// </summary>
		public int NumberOfItemsWithTheTimestampRead
		{ get; set; }

		/// <summary>
		///		Get hash code of last read data item to verify position when restoring. This should be stable persistent
		///		business key. If you do not wish this verification to happen just return zero.
		/// </summary>
		public int VerificationLastReadItemHash
		{ get; set; }

		/// <summary>
		///		Create a copy of this position instance
		/// </summary>
		/// <returns>
		///		New instance of <code>FolderReadingPosition</code>
		/// </returns>
		public object Clone()
		{
			return new FolderReadingPosition(this);
		}

		/// <summary>
		///		Update this position after reading another data item
		/// </summary>
		/// <param name="lastReadDataItem">
		///		Last data item that has been read from this folder
		/// </param>
		public void Update(IDataItem lastReadDataItem)
		{
			if (Time == lastReadDataItem.DateTime)
			{
				++NumberOfItemsWithTheTimestampRead;
			}
			else
			{
				Time = lastReadDataItem.DateTime;
				NumberOfItemsWithTheTimestampRead = 1;
			}
			VerificationLastReadItemHash = lastReadDataItem.GetBusinessHashCode();
		}

		/// <summary>
		///		Update this position with seek time (<see cref="IRepositoryReader.Seek(System.DateTime)"/>)
		/// </summary>
		/// <param name="seekTime">
		///		Time used in <see cref="IRepositoryReader.Seek(System.DateTime)"/>
		/// </param>
		public void FromSeek(DateTime seekTime)
		{
			Time = seekTime;
			NumberOfItemsWithTheTimestampRead = 0;
			VerificationLastReadItemHash = 0;
		}

		/// <summary>
		///		Update this position with another reading position (<see cref="IRepositoryReader.Seek(IFolderReadingPosition)"/>)
		/// </summary>
		/// <param name="position">
		///		Position used in <see cref="IRepositoryReader.Seek(IFolderReadingPosition)"/>
		/// </param>
		public void FromSeek(IFolderReadingPosition position)
		{
			this.FolderKey = position.FolderKey;
			this.NumberOfItemsWithTheTimestampRead = position.NumberOfItemsWithTheTimestampRead;
			this.Time = position.Time;
			this.VerificationLastReadItemHash = position.VerificationLastReadItemHash;
		}

		/// <summary>
		///		Empty position will suggest reading from the beginning
		/// </summary>
		/// <param name="readingDirection">
		///		The reading direction
		/// </param>
		public void SetEmpty(Util.EnumerationDirection readingDirection)
		{
			NumberOfItemsWithTheTimestampRead = 0;
			this.Time = Util.TimeComparer.GetComparer(readingDirection).MinValue;
		}

		/// <summary>
		///		Get normalised [relative] path of the target folder (<see cref="IRepositoryFolder.FolderKey"/>)
		/// </summary>
		public string FolderKey
		{ get; private set; }

		/// <summary>
		///		Given first logical (according to direction) item timestamp in file and logical time comparison (again, according
		///		to direction) determine whether the file is to be read in full when restoring this position
		/// </summary>
		/// <param name="firstItemTimestampInFile">
		///		First logical item timestamp in the file (of either first or last item when going forward and backwards respectively)
		/// </param>
		/// <param name="comparison">
		///		Time comparison implementation
		/// </param>
		/// <returns>
		///		<see langword="true"/> if first item in the file represented by <paramref name="firstItemTimestampInFile"/> will have to be read
		///		<see langword="false"/> otherwise, i.e. when at least 1 item will have to be skipped
		/// </returns>
		public bool IsFileToBeReadInFull(DateTime firstItemTimestampInFile, Util.IDirectedTimeComparison comparison)
		{
			int cmp = comparison.Compare(firstItemTimestampInFile, Time);
			return cmp > 0 || (cmp == 0 && NumberOfItemsWithTheTimestampRead == 0);
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(this.FolderKey, this.NumberOfItemsWithTheTimestampRead, this.Time, this.VerificationLastReadItemHash);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			FolderReadingPosition that = obj as FolderReadingPosition;
			if (obj == null)
			{
				return false;
			}
			return this.FolderKey == that.FolderKey && this.Time == that.Time && this.NumberOfItemsWithTheTimestampRead == that.NumberOfItemsWithTheTimestampRead
				&& this.VerificationLastReadItemHash == that.VerificationLastReadItemHash;
		}
	}
}
