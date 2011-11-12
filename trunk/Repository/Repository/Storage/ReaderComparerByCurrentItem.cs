using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using bfs.Repository.Util;



namespace bfs.Repository.Storage
{
	/// <summary>
	///		Comparer for folder readers supporting primary comparison by current item timestamp in both directions
	///		and custom (optional) data items comparer to use when timesatmps are equal;
	/// </summary>
	/// <remarks>
	///		Can change direction any time.
	///		Direction must be set via property setter to initialise properly.
	/// </remarks>
	internal class ReaderComparerByCurrentItem : IComparer<RepositoryFolderReader>
	{
		private Util.EnumerationDirection _direction;

		/// <summary>
		///		Initialise with the specified direction
		/// </summary>
		/// <param name="direction">
		///		Reading direction
		/// </param>
		internal ReaderComparerByCurrentItem(Util.EnumerationDirection direction)
		{
			// have to set property to initialise
			this.Direction = direction;
		}

		/// <summary>
		///		Default direction - forward
		/// </summary>
		internal ReaderComparerByCurrentItem()
			: this(Util.EnumerationDirection.Forwards)
		{}

		public IDirectedTimeComparison PrimaryComparer
		{ get; private set; }

		public IComparer<IDataItem> DataItemComparer
		{ get; set; }

		public Util.EnumerationDirection Direction
		{
			get { return _direction; }
			set
			{
				PrimaryComparer = Util.TimeComparer.GetComparer(value);
				_direction = value;
			}
		}

		public int Compare(RepositoryFolderReader x, RepositoryFolderReader y)
		{
			int result = PrimaryComparer.Compare(x.CurrentItem.DateTime, y.CurrentItem.DateTime);
			if (0 == result && null != DataItemComparer)
			{
				result = DataItemComparer.Compare(x.CurrentItem, y.CurrentItem);
			}
			return result;
		}
	}
}
