using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	/// <summary>
	///		The interface of an indexed dictionary of non-overlapping ranges of values idintified by range start, allowing
	///		to quickly find/select/order items by specifying a range of keys rather than an exact key value.
	/// </summary>
	/// <typeparam name="K">
	///		The type of range delimiters (such as <see cref="DateTime"/>)
	/// </typeparam>
	/// <typeparam name="V">
	///		The type of indexed items; instances of the type must provide their range start and range end
	/// </typeparam>
	/// <remarks>
	///		Each data item must provide 2 comparable values of type <typeparamref name="K"/> - start (inclusive) and end (exclusive).
	///		The range start serves and is referred to as item/range key. All key values between range start/end are referred to as
	///		covered keys or keys in range.
	/// </remarks>
	public interface IIndexedRangeCollection<K, V> : IEnumerable<V>
		where K : IComparable<K>
		//where V : class
	{
		/// <summary>
		///		Get number of items in the collection
		/// </summary>
		int Count
		{ get; }

		/// <summary>
		///		Get read-only collection of items in the collection. Order is not enforced.
		/// </summary>
		IReadOnlyCollection<V> Values
		{ get; }

		/// <summary>
		///		Get boolean value indicationg whether an item with the specified key exists in the collection
		/// </summary>
		/// <param name="rangeKey">
		///		The item key (which is the start of the associated range)
		/// </param>
		/// <returns>
		///		<see langword="true"/> if does contain, <see langword="false"/> otherwise
		/// </returns>
		bool ContainsKey(K rangeKey);

		/// <summary>
		///		Check whether the collection contains an item with the same key as <paramref name="item"/>
		/// </summary>
		/// <param name="item">
		///		Item whose key to probe
		/// </param>
		/// <returns>
		///		<see langword="true"/> if the collection contains an item with key equal to the <paramref name="item"/>'s key
		/// </returns>
		bool Contains(V item);

		/// <summary>
		///		Get an item by item key (which is the start of the associated range)
		/// </summary>
		/// <param name="rangeKey">
		///		The item key value
		/// </param>
		/// <returns>
		///		<code>default(V)</code> item with the specified <paramref name="rangeKey"/> does not exist in the collection
		/// </returns>
		V GetExact(K rangeKey);

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
		void GetItems(K coveredKey, out V predecessor, out V owner, out V successor);

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
		V GetOwner(K coveredKey);

		/// <summary>
		///		Add the specified item to the collection.
		/// </summary>
		/// <param name="item">
		///		Th eitem to add to the collection.
		/// </param>
		/// <exception cref="Exceptions.OverlappingRangesException">
		///		The range represented by <paramref name="item"/> overlaps with a range already in the collection
		/// </exception>
		void Add(V item);

		/// <summary>
		///		Returns true if the collection contains an item with the key equal to <paramref name="item"/>'s key
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
		bool FindOrAdd(ref V item);

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
		bool Remove(V item);

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
		bool RemoveByKey(K rangeKey);

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
		IDirectedEnumerable<V> Select(K fromRangeKey, bool backwards);

		/// <summary>
		///		Select ranges with keys in the specified range in key order (in the direction from <paramref name="fromRangeKey"/>
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
		IDirectedEnumerable<V> Select(K fromRangeKey, K toRangeKey);

		/// <summary>
		///		Get item with minimum key
		/// </summary>
		/// <returns>
		///		Contained item with minimum key or <code>default(V)</code> if the collection is empty
		/// </returns>
		V GetMin();

		/// <summary>
		///		Get item with maximum key
		/// </summary>
		/// <returns>
		///		Contained item with maximum key or <code>default(V)</code> if the collection is empty
		/// </returns>
		V GetMax();

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
		IDirectedEnumerable<V> SelectCovering(K fromCoveredKey, K toCoveredKey);

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
		IDirectedEnumerable<V> SelectCovering(K fromCoveredKey, bool backwards);

		/// <summary>
		///		Get sequence of all elements in the collection in the keys order
		/// </summary>
		/// <returns>
		///		<see cref="IDirectedEnumerable&lt;V&gt;"/>
		/// </returns>
		IDirectedEnumerable<V> RangeAll();

		/// <summary>
		///		Get item with the greatest contained key less than <paramref name="key"/>
		/// </summary>
		/// <param name="key">
		///		Key value
		/// </param>
		/// <returns>
		///		<see langword="null"/> if no such item exists
		/// </returns>
		V GetPredecessor(K key);

		/// <summary>
		///		Get item with the smallest contained key greater than <paramref name="key"/>
		/// </summary>
		/// <param name="key">
		///		Key value
		/// </param>
		/// <returns>
		///		<see langword="null"/> if no such item exists
		/// </returns>
		V GetSuccessor(K key);

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
		bool Cut(K cutKey, out V lowItem, out V highItem);
	}
}
