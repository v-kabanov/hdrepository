using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Util;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		The interface represents reading position which can be restored to resume after stopping.
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
	///		The concrete class implementing the interface should be serializable employing either
	///		automatic or custom serialization. Automatic serialization requires only <see cref="SerializableAttribute"/>,
	///		but it may be difficult to implement versioning. With custom serialization the class has to implement
	///		the <see cref="ISerializable"/> interface and a special constructor.
	/// </remarks>
	public interface IReadingPosition : ICloneable
	{
		/// <summary>
		///		This is either last read item timestamp or seek time if none.
		/// </summary>
		/// <remarks>
		///		After restoring position and before first item is read any new folder added to the reader should start reading
		///		from this time inclusive.
		/// </remarks>
		DateTime Time
		{ get; }

		/// <summary>
		///		Get the reading direction
		/// </summary>
		EnumerationDirection Direction
		{ get; }

		/// <summary>
		///		Whether this position contains positions for individual folders.
		/// </summary>
		/// <remarks>
		///		If the position does not contain positions for individual folders it still contains information
		///		about time and direction.
		/// </remarks>
		bool ContainsFolderPositions
		{ get; }

		/// <summary>
		///		Get read-only collection of folder positions by folder keys (<see cref="IRepositoryFolder.FolderKey"/>)
		/// </summary>
		IDictionary<string, IFolderReadingPosition> FolderPositions
		{ get; }
	}
}
