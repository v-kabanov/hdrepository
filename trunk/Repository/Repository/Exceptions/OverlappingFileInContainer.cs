using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using bfs.Repository.Storage;

namespace bfs.Repository.Exceptions
{
	/// <summary>
	///		The exception is thrown when an attempt is made to add an overlapping file to a file container
	/// </summary>
	public class OverlappingFileInContainer : Exception
	{
		/// <summary>
		///		Create new instance
		/// </summary>
		/// <param name="inner">
		///		The exception thrown by ranges collection;
		/// </param>
		/// <param name="containerPath">
		///		File container path
		/// </param>
		/// <remarks>
		///		<see cref="OverlappingRangesException.FirstItem"/> is expected to contain item attempted to be added to the collection.
		///		<see cref="OverlappingRangesException.SecondItem"/> is expected to contain existing item overlapping with the item
		///		attempted to be added to the container.
		/// </remarks>
		public OverlappingFileInContainer(OverlappingRangesException inner, string containerPath)
			: base(
				string.Format(
					StorageResources.OverlappingFileInContainer
					, ((RepositoryFileName)inner.FirstItem).FileName
					, ((RepositoryFileName)inner.SecondItem).FileName
					, containerPath)
				, inner
			)
		{
		}

		public RepositoryFileName ExistingFile
		{
			get
			{
				return InnerException == null
					? (RepositoryFileName)null
					: (RepositoryFileName)((OverlappingRangesException)InnerException).SecondItem;
			}
		}

		public RepositoryFileName NewFile
		{
			get
			{
				return InnerException == null
					? (RepositoryFileName)null
					: (RepositoryFileName)((OverlappingRangesException)InnerException).FirstItem;
			}
		}
	}
}
