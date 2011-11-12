using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	static internal class CollectionUtils
	{

		/// <summary>
		///		Sort LinkedList items in chronological order
		/// </summary>
		/// <typeparam name="T">
		///		Type of items in the list
		/// </typeparam>
		/// <param name="listToSort">
		///		The list to sort
		/// </param>
		/// <param name="getSortDate">
		///		Function retrieving a date value from the list item to sort by
		/// </param>
		/// <returns>
		///		Copy of <paramref name="listToSort"/> with items sorted
		///		by value returned by <paramref name="getSortDate"/>
		/// </returns>
		public static LinkedList<T> SortLinkedListByDate<T>(
			LinkedList<T> listToSort
			, Func<T, DateTime> getSortDate)
		{
			return SortLinkedList<T>(
				listToSort
				, new Comparison<T>(
					(t1, t2) => DateTime.Compare(getSortDate(t1), getSortDate(t2))
				));
		}

		/// <summary>
		///		Sort linked list by custom comparer.
		/// </summary>
		/// <typeparam name="T">
		///		List item tipe
		/// </typeparam>
		/// <param name="listToSort">
		///		The list to sort
		/// </param>
		/// <param name="comparison">
		///		Comparison for sorting
		/// </param>
		/// <returns>
		///		Copy of <paramref name="listToSort"/> with items sorted
		///		by value returned by <paramref name="comparison"/>
		/// </returns>
		public static LinkedList<T> SortLinkedList<T>(
			LinkedList<T> listToSort
			, Comparison<T> comparison)
		{
			List<T> tmpList = new List<T>(listToSort);
			tmpList.Sort(comparison);
			LinkedList<T> retval = new LinkedList<T>(tmpList);
			return retval;
		}

		/// <summary>
		///		Find dictionary items around the specified key value
		/// </summary>
		/// <typeparam name="K">
		///		The dictionary Key type; must implement <see cref="IComparable&lt;K&gt;"/>
		/// </typeparam>
		/// <typeparam name="V">
		///		The dictionary Value type
		/// </typeparam>
		/// <param name="dictionary">
		///		The dictionary to cut
		/// </param>
		/// <param name="cutKey">
		///		The key value around which to find items in the dictionary
		/// </param>
		/// <param name="lowItem">
		///		On output - value of the immediate predecessor of <paramref name="cutKey"/>; the value's key will always be
		///		strictly less than <paramref name="cutKey"/>; if node exists, <code>default(V)</code> is returned
		///		(for reference types it's <see langword="null"/>);
		/// </param>
		/// <param name="highItem">
		///		On output - value of the immediate successor of <paramref name="cutKey"/>; the value's key will always be
		///		strictly greater than <paramref name="cutKey"/>; if node exists, <code>default(V)</code> is returned
		///		(for reference types it's <see langword="null"/>);
		/// </param>
		/// <returns>
		///		<see langword="true"/> if a value with exact same key as <paramref name="cutKey"/> exists in the <paramref name="dictionary"/>;
		///		<see langword="false"/> otherwise
		/// </returns>
		public static bool CutTreeDictionary<K, V>(C5.ISortedDictionary<K, V> dictionary, K cutKey, out V lowItem, out V highItem)
			where K : IComparable<K>
		{
			C5.KeyValuePair<K, V> lowEntry;
			C5.KeyValuePair<K, V> highEntry;
			bool lowExists;
			bool highExists;

			bool retval = dictionary.Cut(cutKey, out lowEntry, out lowExists, out highEntry, out highExists);

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

		/// <summary>
		///		Weak get value from dictionary by key
		/// </summary>
		/// <typeparam name="K">
		///		The dictionary Key type; must implement <see cref="IComparable&lt;K&gt;"/>
		/// </typeparam>
		/// <typeparam name="V">
		///		The dictionary Value type
		/// </typeparam>
		/// <param name="dictionary">
		///		The dictionary from which to get value
		/// </param>
		/// <param name="key">
		///		The key value
		/// </param>
		/// <returns>
		///		<code>default(V)</code> if the key does not exist in the <paramref name="dictionary"/>
		///		otherwise the value with the <paramref name="key"/>
		/// </returns>
		public static V WeakGetByKey<K, V>(C5.ISortedDictionary<K, V> dictionary, K key)
			where K : IComparable<K>
		{
			V retval = default(V);
			dictionary.Find(key, out retval);
			return retval;
		}

		public static V GetPredecessor<K, V>(C5.ISortedDictionary<K, V> dictionary, K key)
			where K : IComparable<K>
		{
			C5.KeyValuePair<K, V> predecessor;
			V retval;
			if (dictionary.TryPredecessor(key, out predecessor))
			{
				retval = predecessor.Value;
			}
			else
			{
				retval = default(V);
			}
			return retval;
		}

		public static V GetSuccessor<K, V>(C5.ISortedDictionary<K, V> dictionary, K key)
			where K : IComparable<K>
		{
			C5.KeyValuePair<K, V> successor;
			V retval;
			if (dictionary.TrySuccessor(key, out successor))
			{
				retval = successor.Value;
			}
			else
			{
				retval = default(V);
			}
			return retval;
		}

		public static V GetOneByComparison<V>(V first, V second, bool greater)
			where V : IComparable<V>
		{
			int compareResult = first.CompareTo(second);
			if (greater)
			{
				return compareResult > 0 ? first : second;
			}
			else
			{
				return compareResult > 0 ? second : first;
			}
		}
	}

}
