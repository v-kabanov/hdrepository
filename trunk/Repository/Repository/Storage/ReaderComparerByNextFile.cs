using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		Comparer for folder readers supporting comparison by NextFileFirstTimestampToRead in both directions
	/// </summary>
	/// <remarks>
	///		Can change direction any time.
	///		Direction must be set via property setter to initialise properly.
	/// </remarks>
	internal class ReaderComparerByNextFile : IComparer<RepositoryFolderReader>
	{
		private Util.EnumerationDirection _direction;

		/// <summary>
		///		Initialise with the specified direction
		/// </summary>
		/// <param name="direction">
		///		Reading direction
		/// </param>
		internal ReaderComparerByNextFile(Util.EnumerationDirection direction)
		{
			// have to set property to initialise
			this.Direction = direction;
		}

		/// <summary>
		///		Default direction - forward
		/// </summary>
		internal ReaderComparerByNextFile()
			: this(Util.EnumerationDirection.Forwards)
		{}

		public IComparer<DateTime> Comparer
		{ get; private set; }

		public Util.EnumerationDirection Direction
		{
			get { return _direction; }
			set
			{
				Comparer = Util.TimeComparer.GetComparer(value);
				_direction = value;
			}
		}

		public int Compare(RepositoryFolderReader x, RepositoryFolderReader y)
		{
			return Comparer.Compare(x.NextFileFirstTimestampToRead.Value, y.NextFileFirstTimestampToRead.Value);
		}
	}
}
