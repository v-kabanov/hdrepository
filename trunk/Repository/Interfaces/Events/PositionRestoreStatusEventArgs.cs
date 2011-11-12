using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Storage;

namespace bfs.Repository.Events
{
	/// <summary>
	///		Class instances of which are passed to receivers of deferred reading position restoration messages. It contains
	///		status of position restoration for a single repository folder.
	/// </summary>
	public class PositionRestoreStatusEventArgs : EventArgs
	{
		/// <summary>
		///		Create new instance.
		/// </summary>
		/// <param name="status">
		///		The position restoration status.
		/// </param>
		public PositionRestoreStatusEventArgs(FolderSeekStatus status)
		{
			Status = status;
		}

		/// <summary>
		///		Get position restoration status.
		/// </summary>
		public FolderSeekStatus Status
		{ get; private set; }
	}
}
