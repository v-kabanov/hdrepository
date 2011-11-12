using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.Runtime.Serialization;
using System.Security.Permissions;
using bfs.Repository.Util;
using System.Diagnostics;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Encapsulates reading position which can be used to resume reading after stopping.
	/// </summary>
	/// <remarks>
	///		Positions are provided by repository readers (<see cref="IRepositoryReader.Position"/>) after being positioned
	///		and updated after each data item being read. Position can also be constructed programmatically. This is the
	///		default implementation.
	///		Position can list individual repository folders and their positions (in which case it may be referred to as "precise")
	///		or it can contain only timestamp and direction ("relative" position). In the first case the position alone is sufficient to
	///		resume reading (<see cref="IObjectFactory.GetReader(IReadingPosition, EventHandler<PositionRestoreStatusEventArgs>)"/>). In the second case
	///		you can create reader with <see cref="IObjectFactory.GetReader(IRepositoryFolder)"/>,
	///		add more target folders with <see cref="IRepositoryReader.AddFolder(IRepositoryFolder)"/> and call
	///		<see cref="IRepositoryReader.Seek(IReadingPosition)"/>.
	/// </remarks>
	[Serializable]
	[DebuggerDisplay("{Time}@{Direction}")]
	public class ReadingPosition : IReadingPosition, ISerializable
	{
		// TODO: update revision; automate update?
		// position version; note that revision is day of build
		// this is own, native version
		private static readonly Version _version = new Version(0, 9, 1, 20101120);
		private const string serializationFieldNameVersion = "version";
		private const string serializationFieldNameDirection = "direction";
		private const string serializationFieldNameFolderPositions = "folderPositions";
		private const string serializationFieldNamePositionTime = "posTime";

		private Dictionary<string, IFolderReadingPosition> _positionsDictionary;
		private IDictionary<string, IFolderReadingPosition> _positionsDictionaryReadonly;
		private EnumerationDirection _direction;

		/// <summary>
		///		Constructor required for custom serialization.
		/// </summary>
		/// <param name="info">info</param>
		/// <param name="context">context</param>
		/// <remarks>
		///		Avoiding serializing dictionary to ensure folder keys are not duplicated as dictionary keys in
		///		addition to being serialized as folder position properties.
		/// </remarks>
		protected ReadingPosition(SerializationInfo info, StreamingContext context)
			: this()
		{
			Version version = (Version)info.GetValue(serializationFieldNameVersion, typeof(Version));
			if (version.CompareTo(_version) > 0)
			{
				// formatting parameters: component name, expected, then found versions
				throw new SerializationException(string.Format(StorageResources.IncompatibleVersion, typeof(ReadingPosition).Name, _version, version));
			}

			this.Time = info.GetDateTime(serializationFieldNamePositionTime);

			this.Direction = (Util.EnumerationDirection)info.GetValue(serializationFieldNameDirection
				, typeof(Util.EnumerationDirection));

			List<IFolderReadingPosition> folderPositions = (List<IFolderReadingPosition>)info.GetValue(
				serializationFieldNameFolderPositions
				, typeof(List<IFolderReadingPosition>));

			folderPositions.ForEach((position) => this.Add(position));
		}

		/// <summary>
		///		Default constructor, empty position.
		/// </summary>
		public ReadingPosition()
		{
			_positionsDictionary = new Dictionary<string, IFolderReadingPosition>();
			_positionsDictionaryReadonly = new Util.ReadOnlyDictionary<string, IFolderReadingPosition>(_positionsDictionary);
			Direction = Util.EnumerationDirection.Forwards;
		}

		/// <summary>
		///		Copy constructor.
		/// </summary>
		/// <param name="other">
		///		The instance to copy.
		/// </param>
		public ReadingPosition(IReadingPosition other)
			: this()
		{
			CopyFrom(other);
		}

		/// <summary>
		///		Get or set reading direction.
		/// </summary>
		/// <remarks>
		///		If the position does not contain any folder reading positions the position time is set to
		///		logical start (logical minimum time according to <see cref="Direction"/>)
		/// </remarks>
		public Util.EnumerationDirection Direction
		{
			get { return _direction; }
			set
			{
				_direction = value;
				if (!ContainsFolderPositions)
				{
					Time = TimeComparer.GetComparer(Direction).MinValue;
				}
			}
		}

		/// <summary>
		///		Get read-only collection of folder positions by folder keys (<see cref="IRepositoryFolder.FolderKey"/>)
		/// </summary>
		public IDictionary<string, IFolderReadingPosition> FolderPositions
		{
			get { return _positionsDictionaryReadonly; }
		}

		/// <summary>
		///		Create a copy of this position.
		/// </summary>
		/// <returns>
		///		new ReadingPosition instance.
		/// </returns>
		public object Clone()
		{
			return new ReadingPosition(this);
		}

		/// <summary>
		///		Whether the position lists individual repository folders (and their positions).
		/// </summary>
		public bool ContainsFolderPositions
		{
			get { return _positionsDictionary.Count > 0; }
		}

		/// <summary>
		///		Add folder reading position
		/// </summary>
		/// <param name="position">
		///		Folder reading position to add
		/// </param>
		/// <exception cref="Util.PreconditionException">
		///		Position for the folder referenced by <paramref name="position"/> already exists.
		/// </exception>
		public void Add(IFolderReadingPosition position)
		{
			Check.DoRequire(!ContainsPosition(position.FolderKey), StorageResources.PositionForFolderAlreadyExists);
			_positionsDictionary.Add(position.FolderKey, position);
		}

		/// <summary>
		///		Set contained folder position.
		/// </summary>
		/// <param name="position">
		///		IFolderReadingPosition instance
		/// </param>
		public void SetFolderPosition(IFolderReadingPosition position)
		{
			Check.DoRequireArgumentNotNull(position, "position");
			_positionsDictionary[position.FolderKey] = position;
		}

		/// <summary>
		///		Check whether this position contains folder reading position for the specified repository folder.
		/// </summary>
		/// <param name="folder">
		///		Repository folder.
		/// </param>
		/// <returns>
		///		bool
		/// </returns>
		public bool ContainsPosition(IRepositoryFolder folder)
		{
			Check.DoRequireArgumentNotNull(folder, "folder");
			return ContainsPosition(folder.FolderKey);
		}

		/// <summary>
		///		Check whether this position contains folder reading position for the specified repository folder.
		/// </summary>
		/// <param name="folderKey">
		///		Repository folder key (<see cref="IRepositoryFolder.FolderKey"/>).
		/// </param>
		/// <returns>
		///		bool
		/// </returns>
		public bool ContainsPosition(string folderKey)
		{
			Check.DoRequireArgumentNotNull(folderKey, "folderKey");
			return _positionsDictionary.ContainsKey(RepositoryFolder.GetFolderPathKey(folderKey));
		}

		/// <summary>
		///		Get contained specific position for the specified repository folder.
		/// </summary>
		/// <param name="folder">
		///		Repository folder.
		/// </param>
		/// <returns>
		///		IFolderReadingPosition
		/// </returns>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">
		///		The position does not contain specific position for the <paramref name="folder"/>.
		/// </exception>
		public IFolderReadingPosition GetPosition(IRepositoryFolder folder)
		{
			Check.DoRequireArgumentNotNull(folder, "folder");
			return _positionsDictionary[folder.FolderKey];
		}

		/// <summary>
		///		Remove the specific folder reading position from this position.
		/// </summary>
		/// <param name="position">
		///		Specific folder reading position.
		/// </param>
		/// <returns>
		///		true - the specific folder position was removed from this position
		///		false - this position does not contain specific position with folder key equal to <code>position.FolderKey</code>
		/// </returns>
		public bool Remove(IFolderReadingPosition position)
		{
			Check.DoRequireArgumentNotNull(position, "position");
			return _positionsDictionary.Remove(position.FolderKey);
		}

		/// <summary>
		///		Remove the specific folder reading position from this position.
		/// </summary>
		/// <param name="folderLogicalPath">
		///		Target folder logical path (<see cref="IRepositoryFolder.LogicalPath"/>).
		/// </param>
		/// <returns>
		///		true - the specific folder position was removed from this position
		///		false - this position does not contain specific position for a folder with the specified logical path
		/// </returns>
		public bool Remove(string folderLogicalPath)
		{
			Check.DoRequireArgumentNotNull(folderLogicalPath, "folderLogicalPath");
			return _positionsDictionary.Remove(RepositoryFolder.GetFolderPathKey(folderLogicalPath));
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(serializationFieldNameVersion, _version, typeof(Version));
			info.AddValue(serializationFieldNameDirection, this.Direction, typeof(Util.EnumerationDirection));
			info.AddValue(serializationFieldNamePositionTime, Time, typeof(DateTime));
			// avoiding serializing dictionary to ensure folder keys are not duplicated as dictionary keys in
			// addition to being folder position properties
			List<IFolderReadingPosition> list = new List<IFolderReadingPosition>(_positionsDictionary.Values);
			info.AddValue(serializationFieldNameFolderPositions, list, typeof(List<IFolderReadingPosition>));
		}

		/// <summary>
		///		This is either last read item timestamp or seek time if none.
		/// </summary>
		/// <remarks>
		///		After restoring position and before first item is read any new folder added to the reader should start reading
		///		from this time inclusive.
		/// </remarks>
		public DateTime Time
		{ get; set; }

		/// <summary>
		///		Update position after a data item has been read.
		/// </summary>
		/// <param name="dataItemRead">
		///		Data item just read
		/// </param>
		public void Update(IDataItem dataItemRead)
		{
			Time = dataItemRead.DateTime;
		}

		/// <summary>
		///		Update position when starting reading from the specified timestamp.
		/// </summary>
		/// <param name="seekTime">
		///		Time used to seek the reader
		/// </param>
		/// <param name="direction">
		///		Reading direction (chronologically); <see cref="IRepositoryReader.Direction"/>
		/// </param>
		public void Update(DateTime seekTime, EnumerationDirection direction)
		{
			Direction = direction;
			Time = seekTime;
		}

		/// <summary>
		///		Make this position a copy of another position
		/// </summary>
		/// <param name="position">
		///		The position to copy
		/// </param>
		public void CopyFrom(IReadingPosition position)
		{
			_positionsDictionary.Clear();
			this.Direction = position.Direction;
			this.Time = position.Time;
			foreach (IFolderReadingPosition pos in position.FolderPositions.Values)
			{
				Add(pos);
			}
		}

		/// <summary>
		///		Clear position and set its time to logical minimum (meaning read from start)
		/// </summary>
		/// <remarks>
		///		The position time is set to logical start (logical minimum time according to <see cref="Direction"/>)
		/// </remarks>
		public void Clear()
		{
			_positionsDictionary.Clear();
			Time = TimeComparer.GetComparer(Direction).MinValue;
		}
	}
}
