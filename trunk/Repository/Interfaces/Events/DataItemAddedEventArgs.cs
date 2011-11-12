//-----------------------------------------------------------------------------
// <created>2/17/2010 2:44:16 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using bfs.Repository.Storage;

namespace bfs.Repository.Events
{
	/// <summary>
	/// 	Arguments passed to handlers of the event signaling that a new data item has been added to the repository.
	/// </summary>
	public class DataItemAddedEventArgs : System.EventArgs
	{
		/// <summary>
		/// 	Create and initialize new instance.
		/// </summary>
		/// <param name="folder">
		/// 	Repository folder to which a new data item has been added.
		/// </param>
		/// <param name="file">
		/// 	Repository file to which a new data item has been added.
		/// </param>
		/// <param name="dataItem">
		/// 	Newly added data item.
		/// </param>
		public DataItemAddedEventArgs(
			IRepositoryFolder folder
			, IRepositoryFileName file
			, IDataItem dataItem)
		{
			this.Folder = folder;
			this.File = file;
			this.DataItem = dataItem;
		}

		/// <summary>
		/// 	Get repository folder to which a new data item has been added.
		/// </summary>
		public IRepositoryFolder Folder
		{
			get;
			private set;
		}
		
		/// <summary>
		/// 	Get repository file to which a new data item has been added.
		/// </summary>
		internal IRepositoryFileName File
		{
			get;
			private set;
		}
		
		/// <summary>
		/// 	Get newly added data item.
		/// </summary>
		public IDataItem DataItem
		{
			get;
			private set;
		}
	}
}
