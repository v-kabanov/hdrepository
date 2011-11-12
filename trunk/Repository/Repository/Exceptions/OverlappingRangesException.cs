using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Exceptions
{
	/// <summary>
	///		Exception signalling violation of restriction on overlapping ranges.
	/// </summary>
	public class OverlappingRangesException : Exception
	{
		private object _item1;
		private object _item2;

		public OverlappingRangesException(object item1, object item2)
			: base(Storage.StorageResources.OverlappingItemsDetected)
		{
			_item1 = item1;
			_item2 = item2;
		}

		public object FirstItem
		{ get { return _item1; } }

		public object SecondItem
		{ get { return _item2; } }
	}
}
