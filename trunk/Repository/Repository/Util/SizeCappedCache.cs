//-----------------------------------------------------------------------------
// <copyright file="SizeCappedCache.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/16/2010 3:16:22 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace bfs.Repository.Util
{
	/// <summary>
	///		The class implement simple queue-like cache where
	///		the size is limited and when the limit is reached
	///		the specified proportion of the least recently accessed
	///		items is deleted from cache 
	/// </summary>
	/// <typeparam name="KeyT">
	///		Items' key type
	/// </typeparam>
	/// <typeparam name="ValueT">
	///		Item type
	/// </typeparam>
	//TODO: rewrite with C5
	public class SizeCappedCache<KeyT, ValueT>
	{
		struct TaggedValue
		{
			public ValueT value;
			public DateTime tag;
		}


		#region const declarations --------------------------------------------

		public const int minSizeCap = 10;
		public const int maxTrimPercent = 70;

		public const int defaultSizeCap = 100;
		public const int defaultTrimPercent = 10;

		#endregion const declarations -----------------------------------------

		#region fields --------------------------------------------------------

		private int _sizeCap;
		private int _trimPercent;

		private Dictionary<KeyT, TaggedValue> _cachedItems = new Dictionary<KeyT, TaggedValue>();

		#endregion fields -----------------------------------------------------

		#region constructors --------------------------------------------------

		public SizeCappedCache()
		{
			_sizeCap = defaultSizeCap;
			_trimPercent = defaultTrimPercent;
		}

		#endregion constructors -----------------------------------------------

		#region public properties ---------------------------------------------

		/// <summary>
		///		Get or set maximum cache size after achieving which cache is trimmed
		///		by deleting <code>TrimPercent</code> the least recently accessed items
		/// </summary>
		public int SizeCap
		{
			get
			{
				return _sizeCap;
			}
			set
			{
				if (value < minSizeCap)
				{
					throw new ArgumentException(string.Format("SizeCap must be greater or equal {1}", minSizeCap));
				}
				_sizeCap = value;
				Trim();
			}
		}

		public int TrimPercent
		{
			get
			{
				return _trimPercent;
			}
			set
			{
				if (value > maxTrimPercent)
				{
					throw new ArgumentException(string.Format("TrimPercent must be less or equal {1}", maxTrimPercent));
				}
				_trimPercent = value;
			}
		}

		#endregion public properties ------------------------------------------

		#region public methods ------------------------------------------------

		public void Clear()
		{
			_cachedItems.Clear();
		}

		public bool TryGetItem(KeyT key, out ValueT item)
		{
			TaggedValue val;
			bool retval = _cachedItems.TryGetValue(key, out val);
			if (retval)
			{
				val.tag = DateTime.UtcNow;
				item = val.value;
			}
			else
			{
				item = default(ValueT);
			}
			return retval;
		}

		public void PutItem(KeyT key, ValueT item)
		{
			TaggedValue val = new TaggedValue() { tag = DateTime.UtcNow, value = item };
			_cachedItems[key] = val;
			Trim();
		}

		#endregion public methods ---------------------------------------------

		#region private methods -----------------------------------------------

		/// <summary>
		///		Trim if required
		/// </summary>
		private void Trim()
		{
			if (_cachedItems.Count > this.SizeCap)
			{
				DoTrim();
			}
		}

		private void DoTrim()
		{
			int targetSize = (int)(this.SizeCap - (double)this.SizeCap * (double)this.TrimPercent / 100.0);
			int purgeCount = _cachedItems.Count - targetSize;

			if (purgeCount > 0)
			{
				List<KeyT> purgingItemKeys =
					(from c in _cachedItems
					 orderby c.Value.tag descending
					 select c.Key).Take(purgeCount).ToList();

				foreach (KeyT key in purgingItemKeys)
				{
					_cachedItems.Remove(key);
				}
			}
		}

		#endregion private methods --------------------------------------------
	}
}
