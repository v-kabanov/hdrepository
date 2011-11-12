using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using bfs.Repository.Interfaces;

namespace bfs.Repository.Interfaces.Infrastructure
{
	public interface IReader : IRepositoryReader
	{
		/// <summary>
		///		Callback for reporting deferred seek status for folder readers
		/// </summary>
		/// <param name="status">
		///		Status
		/// </param>
		/// <remarks>
		///		To minimise resources consumption data is loaded just before it is required. Therefore when restoring positions
		///		<see cref="Seek(IReadingPosition)"/> the result may become available long after the call to the <see cref="Seek(IReadingPosition)"/>.
		///		This callback provides folder readers a way of reporting any issues when they are encountered.
		/// </remarks>
		void SeekStatusCallback(Storage.FolderSeekStatus status);
	}
}
