using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		The interface exposes routing functionality, mapping/redirecting data items to repository subfolders.
	/// </summary>
	/// <remarks>
	///		This allows to structute data storage independently of data items contents. Initial aim is to optimise
	///		performance and allow fine-grained data reading. For example, data source provides data items tagged
	///		with integer object ID and the number of objects is not known. If data from all of the objects goes into
	///		the same folder and subsequently you need to read data of a small subset of objects you would need to
	///		read and unpack a lot of data from objects you do not need. Also, the number of files in the leaf data
	///		folders will be excessive which will affect performance. Providing a simple implementation of this interface
	///		however you can for example break the whole collection of objects into reasonably small groups (say,
	///		10 objects per folder). Then, when reading, you would only read data from those subfolders which
	///		contain data for objects you are interested in thus avoiding reading and unpacking data for most of
	///		the objects you are not interested in.
	/// </remarks>
	/// <example>
	///		public string GetRelativePath(IDataItem dataItem)
	///		{
	///			return (((MyDataItem)dataItem).ObjectID % 10).ToString();
	///		}
	/// </example>
	public interface IDataRouter
	{
		string GetRelativePath(IDataItem dataItem);
	}
}
