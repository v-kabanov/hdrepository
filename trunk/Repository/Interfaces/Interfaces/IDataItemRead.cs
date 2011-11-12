using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces
{
	/// <summary>
	///		The data item read from a repository packaged with additional information.
	/// </summary>
	public interface IDataItemRead
	{
		/// <summary>
		///		Get data item
		/// </summary>
		IDataItem DataItem
		{ get; }

		/// <summary>
		///		Get repository folder from which the data item was read
		/// </summary>
		IRepositoryFolder RepositoryFolder
		{ get; }
	}
}
