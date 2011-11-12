using System;
using System.Collections.Generic;
using System.Text;

using bfs.Repository.Exceptions;

namespace bfs.Repository.Util
{
	/// <summary>
	///		Indexed dictionary implementation based on <see cref="C5.TreeDictionary"/> of non-overlapping ranges of values idintified
	///		by range start, allowing to quickly find/select/order items by specifying a range of keys rather than an exact key value.
	/// </summary>
	/// <typeparam name="K">
	///		The type of range delimiters (such as <see cref="DateTime"/>)
	/// </typeparam>
	/// <typeparam name="V">
	///		The type of indexed items; instances of the type must provide their range start and range end via lambda expressions
	///		passed to the constructor
	/// </typeparam>
	/// <remarks>
	///		Each data item must provide 2 comparable values of type <typeparamref name="K"/> - start (inclusive) and end (exclusive).
	///		The range start serves and is referred to as item/range key. All key values between range start/end are referred to as
	///		covered keys or keys in range.
	/// </remarks>
	public class IndexedRangeTreeDictionary<K, V> : IIndexedRangeCollection<K, V>
		where K : IComparable<K>
		where V : class
	{
		private C5.TreeDictionary<K, V> _dictionary;
		private Func<V, K> _getRangeStart;
		private Func<V, K> _getRangeEnd;

		public IndexedRangeTreeDictionary(Func<V, K> getRangeStart, Func<V, K> getRangeEnd)
		{
			_dictionary = new C5.TreeDictionary<K, V>();
			_getRangeStart = getRangeStart;
			_getRangeEnd = getRangeEnd;
		}

		#region protected methods


		/// <summary>
		///		Check whether the items do not overlap
		/// </summary>
		/// <param name="item">
		///		first item in the sequence
		/// </param>
		/// <param name="nextFile">
		///		Next item in the sequence
		/// </param>
		/// <returns>
		///		<see langword="true"/> - all ok
		///		<see langword="false"/> - overlapping detected
		/// </returns>
		private bool AreOverlapping(V item, V nextItem)
		{
			return GetRangeEnd(item).CompareTo(GetRangeStart(nextItem)) > 0;
		}

		/// <param name="changePlacesInException">
		///		When throwing <code>OverlappingRangesException</code> put <paramref name="nextItem"/> into first one
		/// </param>
		private void CheckOverlapping(V item, V nextItem, bool changePlacesInException)
		{
			Check.RequireLambda(!AreOverlapping(item, nextItem)
				, () => new OverlappingRangesException(
					changePlacesInException ? nextItem : item, changePlacesInException ? item : nextItem));
		}

		/// <summary>
		///		Check whether the specified item overlaps with another (different) items in the collection.
		/// </summary>
		/// <param name="item">
		///		The item to check. It may be contained or not. It is compared
		///		against adjacent items to the left and right. Item with the same key makes no difference.
		/// </param>
		/// <exception cref="Exceptions.OverlappingRangesException">
		///		The range represented by <paramref name="item"/> overlaps with a range already in the collection.
		///		<see cref="OverlappingRangesException.FirstItem"/> will contain <paramref name="item"/>
		/// </exception>
		private void CheckOverlapping(V item)
		{
			V lowItem;
			V highItem;

			if (Cut(GetItemKey(item), out lowItem, out highItem))
			{
				throw new OverlappingRangesException(item, GetExact(GetItemKey(item)));
			}

			// if this is the only file in the collection, there's no overlapping

			if (lowItem != null)
			{
				CheckOverlapping(lowItem, item, true);
			}

			if (highItem != null)
			{
				CheckOverlapping(item, highItem, false);
			}
		}

		#endregion protected methods

		#region private methods

		/// <summary>
		///		Get covering item key if the specified key is covered by a contained item or original key value
		///		otherwise
		/// </summary>
		/// <param name="coveredKey">
		///		The key value to probe
		/// </param>
		/// <returns>
		///		Key of a contained item covering the <paramref name="coveredKey"/> or <paramref name="coveredKey"/>
		///		if such item does not exist
		/// </returns>
		private K GetReplacementCoveringItemKey(K coveredKey)
		{
			V coveringItem = GetOwner(coveredKey);
			if (coveringItem != null)
			{
				return GetItemKey(coveringItem);
			}
			else
			{
				return coveredKey;
			}
		}

		private K GetRangeStart(V item)
		{
			return _getRangeStart(item);
		}

		private K GetItemKey(V item)
		{
			return GetRangeStart(item);
		}

		private K GetRangeEnd(V item)
		{
			return _getRangeEnd(item);
		}

		#endregion private methods

		#region IIndexedRangeCollection<K, V> members

		/*
		//TODO: low priority: convert handlers

		public event C5.ItemsAddedHandler<C5.KeyValuePair<K, V>> ItemsAdded
		{
			add { _dictionary.ItemsAdded += value; }
			remove { _dictionary.ItemsAdded -= value; }
		}

		public event C5.CollectionChangedHandler<C5.KeyValuePair<K, V>> CollectionChanged
		{
			add { _dictionary.CollectionChanged += value; }
			remove { _dictionary.CollectionChanged -= value; }
		}

		public event C5.ItemsRemovedHandler<C5.KeyValuePair<K, V>> ItemsRemoved
		{
			add { _dictionary.ItemsRemoved += value; }
			remove { _dictionary.ItemsRemoved -= value; }
		}*/

		/// <summary>
		///		Get number of items in the collection
		/// </summary>
		public int Count
		{ get { return _dictionary.Count; } }

		/// <summary>
		///		Get read-only collection of items in the collection. Order is not enforced.
		/// </summary>
		public IReadOnlyCollection<V> Values
		{ get { return new C5ValuesCollection<V>(_dictionary.Values); } }

		/// <summary>
		///		Get boolean value indicationg whether an item with the specified key exists in the collection
		/// </summary>
		/// <param name="rangeKey">
		///		The item key (which is the start of the associated range)
		/// </param>
		/// <returns>
		///		<see langword="true"/> if does contain, <see langword="false"/> otherwise
		/// </returns>
		public bool ContainsKey(K rangeKey)
		{
			return _dictionary.Contains(rangeKey);
		}

		/// <summary>
		///		Check whether the collection contains an item with the same key as <paramref name="item"/>
		/// </summary>
		/// <param name="item">
		///		Item whose key to probe
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the collection contains an item with key equal to the <paramref name="item"/>'s key
		/// </returns>
		public bool Contains(V item)
		{
			return ContainsKey(GetItemKey(item));
		}

		/// <summary>
		///		Get an item by item key (which is the start of the associated range)
		/// </summary>
		/// <param name="rangeKey">
		///		The item key value
		/// </param>
		/// <returns>
		///		<code>default(V)</code> item with the specified <paramref name="rangeKey"/> does not exist in the collection
		/// </returns>
		public V GetExact(K rangeKey)
		{
			V retval = default(V);
			_dictionary.Find(rangeKey, out retval);
			return retval;
		}

		/// <summary>
		///		Get items (ranges) around the specified key value
		/// </summary>
		/// <param name="coveredKey">
		///		The covered key value (key covered by items' ranges as opposed to range/item key)
		/// </param>
		/// <param name="predecessor">
		///		Output, the predecessor of <paramref name="coveredKey"/>
		/// </param>
		/// <param name="owner">
		///		Output, the item, covering <paramref name="coveredKey"/>
		/// </param>
		/// <param name="successor">
		///		Output, the successor of <paramref name="coveredKey"/>
		/// </param>
		public void GetItems(K targetKeyValue, out V predecessor, out V owner, out V successor)
		{
			owner = default(V);
			if (Cut(targetKeyValue, out predecessor, out successor))
			{
				// item with exact same key exists, and predecessor-successor retrieved
				owner = GetExact(targetKeyValue);
			}
			else
			{
				// item with exact same key (range Start) does not exist
				// successor retrieved, but predecessor needs checking
				if (predecessor != null && _getRangeEnd(predecessor).CompareTo(targetKeyValue) > 0)
				{
					// predecessor item starts before the probe key and ends after - it's an owner
					// looking for predecessor of the owner
					owner = predecessor;
					predecessor = GetPredecessor(GetItemKey(owner));
				}
			}

			Check.Ensure(successor == null || GetRangeStart(successor).CompareTo(targetKeyValue) > 0);
			Check.Ensure(owner == null
				|| (GetRangeStart(owner).CompareTo(targetKeyValue) <= 0 && GetRangeEnd(owner).CompareTo(targetKeyValue) > 0));
			Check.Ensure(predecessor == null || GetRangeEnd(predecessor).CompareTo(targetKeyValue) <= 0);

			Check.Ensure(predecessor != owner || (predecessor == null && owner == null));
			Check.Ensure(predecessor != successor || (predecessor == null && successor == null));
			Check.Ensure(successor != owner || (successor == null && owner == null));
		}

		/// <summary>
		///		Get contained item covering the <paramref name="coveredKey"/>.
		/// </summary>
		/// <param name="coveredKey">
		///		Key value covered by an item/range
		/// </param>
		/// <returns>
		///		The contained item covering the <paramref name="coveredKey"/> by its range
		///		or <see langword="null"/> if there's no such item.
		/// </returns>
		public V GetOwner(K key)
		{
			V predecessor;
			V successor;
			V owner;
			GetItems(key, out predecessor, out owner, out successor);
			return owner;
		}

		/// <summary>
		///		Add the specified item to the collection.
		/// </summary>
		/// <param name="item">
		///		Th eitem to add to the collection.
		/// </param>
		/// <exception cref="Exceptions.OverlappingRangesException">
		///		The range represented by <paramref name="item"/> overlaps with a range already in the collection.
		///		<see cref="OverlappingRangesException.FirstItem"/> will contain <paramref name="item"/>
		/// </exception>
		public void Add(V item)
		{
			CheckOverlapping(item);
			_dictionary.Add(GetItemKey(item), item);
		}

		/// <summary>
		///		returns true if the collection contains an item with the key equal to <paramref name="item"/>'s key
		///		and in that case binds one such item to the ref parameter <paramref name="item"/>; otherwise returns
		///		false and adds <paramref name="item"/> to the collection.
		/// </summary>
		/// <param name="item">
		///		Ref Item to find and return or add
		/// </param>
		/// <returns>
		///		<see langword="true"/> if an item with the same key exists
		///		<see langword="false"/> otherwise (item gets added)
		/// </returns>
		public bool FindOrAdd(ref V item)
		{
			V existingItem = GetExact(GetItemKey(item));
			bool retval = existingItem == null;
			if (retval)
			{
				Add(item);
			}
			else
			{
				item = existingItem;
			}
			return retval;
		}

		/// <summary>
		///		Get sequence from the item key value to end or start of the collection in the key order
		/// </summary>
		/// <param name="fromRangeKey">
		///		Item key from which to start; inclusive if <paramref name="backwards"/> is <see langword="false"/>
		///		and exclusive otherwise
		/// </param>
		/// <param name="backwards">
		///		Direction of the sequence: ascending if <see langword="false"/> and descending otherwise
		/// </param>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;V&gt;"/>
		/// </returns>
		/// <remarks>
		///		Note that when going forward the item with range covering <paramref name="fromRangeKey"/> but starting before
		///		it will not be returned.
		/// </remarks>
		public IDirectedEnumerable<V> Select(K fromRangeKey, bool backwards)
		{
			C5.IDirectedEnumerable<C5.KeyValuePair<K, V>> seq;
			if (backwards)
			{
				seq = _dictionary.RangeTo(fromRangeKey).Backwards();
			}
			else
			{
				seq = _dictionary.RangeFrom(fromRangeKey);
			}
			return new C5DirectedEnumerable<V>(new DictionaryEnumerableValueAdapter<K, V>(seq));
		}

		/// <summary>
		///		Remove the specified item from collection
		/// </summary>
		/// <param name="item">
		///		The item to remove
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the item was removed
		///		<see langword="false"/> if the item was not in the collection
		/// </returns>
		public bool Remove(V item)
		{
			return RemoveByKey(GetItemKey(item));
		}

		/// <summary>
		///		Remove an item with the specified key from the collection
		/// </summary>
		/// <param name="rangeKey">
		///		The item key to remove
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the item was removed
		///		<see langword="false"/> if the item was not in the collection
		/// </returns>
		public bool RemoveByKey(K rangeKey)
		{
			return _dictionary.Remove(rangeKey);
		}

		/// <summary>
		///		Select items with keys in the specified range in key order (in the direction from <paramref name="fromRangeKey"/>
		///		to <paramref name="toRangeKey"/>. Lower limit is inclusive, higher - exclusive
		/// </summary>
		/// <param name="fromRangeKey">
		///		Selection range start  (inclusive if less than <paramref name="toRangeKey"/> and exclusive otherwise)
		/// </param>
		/// <param name="toRangeKey">
		///		Selection range end (inclusive if less than <paramref name="fromRangeKey"/> and exclusive otherwise)
		/// </param>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;V&gt;"/>
		/// </returns>
		/// <remarks>
		///		Note that when going forward the item with range covering <paramref name="fromRangeKey"/> but starting before
		///		it will not be returned. Similarly when going backwards the last range starting before the end of the range
		///		will not be returned.
		/// </remarks>
		public IDirectedEnumerable<V> Select(K fromRangeKey, K toRangeKey)
		{
			C5.IDirectedEnumerable<C5.KeyValuePair<K, V>> seq;
			if (fromRangeKey.CompareTo(toRangeKey) < 0)
			{
				// ascending
				seq = _dictionary.RangeFromTo(fromRangeKey, toRangeKey);
			}
			else
			{
				// descending
				seq = _dictionary.RangeFromTo(toRangeKey, fromRangeKey).Backwards();
			}
			return new C5DirectedEnumerable<V>(new DictionaryEnumerableValueAdapter<K, V>(seq));
		}

		/// <summary>
		///		Get item with minimum key
		/// </summary>
		/// <returns>
		///		Contained item with minimum key or <see langword="null"/> if the collection is empty
		/// </returns>
		public V GetMin()
		{
			if (this.Count > 0)
			{
				return _dictionary.FindMin().Value;
			}
			return default(V);
		}

		/// <summary>
		///		Get item with maximum key
		/// </summary>
		/// <returns>
		///		Contained item with maximum key or <see langword="null"/> if the collection is empty
		/// </returns>
		public V GetMax()
		{
			if (this.Count > 0)
			{
				return _dictionary.FindMax().Value;
			}
			return default(V);
		}

		/// <summary>
		///		Select ranges covering keys in the specified range, in key order (in the direction from <paramref name="fromCoveredKey"/>
		///		to <paramref name="toCoveredKey"/>. Lower limit is inclusive, higher - exclusive
		/// </summary>
		/// <param name="fromCoveredKey">
		///		Selection range start  (inclusive if less than <paramref name="toCoveredKey"/> and exclusive otherwise)
		/// </param>
		/// <param name="toCoveredKey">
		///		Selection range end (inclusive if less than <paramref name="fromCoveredKey"/> and exclusive otherwise)
		/// </param>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;V&gt;"/> with items covering keys in range [lowerLimit, higherLimit)
		/// </returns>
		public IDirectedEnumerable<V> SelectCovering(K fromCoveredKey, K toCoveredKey)
		{
			// correct lower end to make it inclusive
			if (fromCoveredKey.CompareTo(toCoveredKey) < 0)
			{
				fromCoveredKey = GetReplacementCoveringItemKey(fromCoveredKey);
			}
			else
			{
				toCoveredKey = GetReplacementCoveringItemKey(fromCoveredKey);
			}

			return Select(fromCoveredKey, toCoveredKey);
		}

		/// <summary>
		///		Select items covering keys in range [<param name="fromCoveredKey" />, ...) or [..., <param name="fromCoveredKey" />)
		///		in the key order
		/// </summary>
		/// <param name="fromCoveredKey">
		///		Key value from which to start; inclusive if <paramref name="backwards"/> is <see langword="false"/>
		///		and exclusive otherwise
		/// </param>
		/// <param name="backwards">
		///		Direction of the sequence: ascending if <see langword="false"/> and descending otherwise
		/// </param>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;V&gt;"/>
		/// </returns>
		public IDirectedEnumerable<V> SelectCovering(K fromCoveredKey, bool backwards)
		{
			if (!backwards)
			{
				// correct lower end to make it inclusive
				fromCoveredKey = GetReplacementCoveringItemKey(fromCoveredKey);
			}
			return Select(fromCoveredKey, backwards);
		}

		/// <summary>
		///		Get sequence of all elements in the collection in the keys order
		/// </summary>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;V&gt;"/>
		/// </returns>
		public IDirectedEnumerable<V> RangeAll()
		{
			return new C5DirectedEnumerable<V>(new Util.DictionaryEnumerableValueAdapter<K, V>(_dictionary.RangeAll()));
		}

		/// <summary>
		///		Get item with the greatest contained key less than <paramref name="key"/>
		/// </summary>
		/// <param name="key">
		///		Key value
		/// </param>
		/// <returns>
		///		<see langword="null"/> if no such item exists
		/// </returns>
		public V GetPredecessor(K key)
		{
			return CollectionUtils.GetPredecessor<K, V>(_dictionary, key);
		}

		/// <summary>
		///		Get item with the smallest contained key greater than <paramref name="key"/>
		/// </summary>
		/// <param name="key">
		///		Key value
		/// </param>
		/// <returns>
		///		<see langword="null"/> if no such item exists
		/// </returns>
		public V GetSuccessor(K key)
		{
			return CollectionUtils.GetSuccessor<K, V>(_dictionary, key);
		}

		/// <summary>
		///		Find dictionary items around the specified key value
		/// </summary>
		/// <param name="cutKey">
		///		The covered key value around which to find items in the dictionary
		/// </param>
		/// <param name="lowItem">
		///		On output - the immediate predecessor of <paramref name="cutKey"/>; the value's key will always be
		///		strictly less than <paramref name="cutKey"/>; if node exists, <code>default(V)</code> is returned
		///		(for reference types it's <see langword="null"/>);
		/// </param>
		/// <param name="highItem">
		///		On output - the immediate successor of <paramref name="cutKey"/>; the value's key will always be
		///		strictly greater than <paramref name="cutKey"/>; if node exists, <code>default(V)</code> is returned
		///		(for reference types it's <see langword="null"/>);
		/// </param>
		/// <returns>
		///		<see langword="true"/> if item with exact same key as <paramref name="cutKey"/> exists in the dictionary;
		///		<see langword="false"/> otherwise
		/// </returns>
		public bool Cut(K cutKey, out V lowItem, out V highItem)
		{
			C5.KeyValuePair<K, V> lowEntry;
			C5.KeyValuePair<K, V> highEntry;
			bool lowExists;
			bool highExists;

			bool retval = _dictionary.Cut(cutKey, out lowEntry, out lowExists, out highEntry, out highExists);

			if (lowExists)
			{
				lowItem = lowEntry.Value;
			}
			else
			{
				lowItem = default(V);
			}

			if (highExists)
			{
				highItem = highEntry.Value;
			}
			else
			{
				highItem = default(V);
			}

			return retval;
		}

		public IEnumerator<V> GetEnumerator()
		{
			return _dictionary.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion IIndexedRangeCollection<K, V> members
	}
}
