//-----------------------------------------------------------------------------
// <created>1/25/2010 9:49:07 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		The interface of the data item to be stored in the repository.
	///		Clients have to implement this interface in their classes representing data items.
	/// </summary>
	/// <remarks>
	///		The concrete class implementing the interface needs to be serializable employing either
	///		automatic or custom serialization. Automatic serialization requires only <see cref="SerializableAttribute"/>,
	///		but it may be difficult to implement versioning. With custom serialization the class has to implement
	///		the <see cref="ISerializable"/> interface and a special constructor.
	/// </remarks>
	public interface IDataItem
	{
		/// <summary>
		///		Path to the subfolder (relative to the repo folder which is the direct target of the writer) into which
		///		the data item has to go.
		/// </summary>
		/// <remarks>
		///		Use lower case and '/' as path separator, no leading or trailing separators:
		///		root/sub1/sub2 for best performance. Use <code>string.Empty</code> to write directly into writer's
		///		target folder. The client has to choose whether to persist the path; it may not be relavant when reading
		///		data from repository.
		/// </remarks>
		string RelativePath
		{ get; }

		/// <summary>
		///		Get data item timestamp. It is recommended to use UTC date-time to avoid ambiguity of date-time during
		///		switches between daylight saving modes.
		/// </summary>
		DateTime DateTime
		{ get; }

		/// <summary>
		///		Get stable hash code which will survive serialization, primarily for verification purposes.
		/// </summary>
		/// <returns>
		///		Int32
		/// </returns>
		/// <remarks>
		///		The code must not represent a concrete instance of the class but rather on contents of the class, i.e.
		///		business key. It must not change after serialization-deserialization. Repository will internally use it
		///		to verify reading position. If you do not wish to use such verification, return <code>0</code>.
		/// </remarks>
		int GetBusinessHashCode();
	}
}
