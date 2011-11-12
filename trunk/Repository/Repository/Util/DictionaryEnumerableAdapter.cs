using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	internal class DictionaryEnumerableAdapter<TValue, TResult> : C5.IDirectedEnumerable<TResult>
	{
		private C5.IDirectedEnumerable<TValue> _sourceCollection;
		private Func<TValue, TResult> _converter;

		public DictionaryEnumerableAdapter(
			C5.IDirectedEnumerable<TValue> sourceCollection
			, Func<TValue, TResult> converter)
		{
			_sourceCollection = sourceCollection;
			_converter = converter;
		}

		#region IDirectedEnumerable<TResult> Members

		public C5.IDirectedEnumerable<TResult> Backwards()
		{
			return new DictionaryEnumerableAdapter<TValue, TResult>(
				_sourceCollection.Backwards(), _converter);
		}

		public C5.EnumerationDirection Direction
		{
			get { return _sourceCollection.Direction; }
		}

		#endregion

		#region IEnumerable<TResult> Members

		public IEnumerator<TResult> GetEnumerator()
		{
			return new DictionaryEnumeratorAdapter<TValue, TResult>(
				_sourceCollection.GetEnumerator(), _converter);
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion
	}

	internal class DictionaryEnumerableValueAdapter<TKey, TValue>
		: DictionaryEnumerableAdapter<C5.KeyValuePair<TKey, TValue>, TValue>
	{
		public DictionaryEnumerableValueAdapter(
			C5.IDirectedEnumerable<C5.KeyValuePair<TKey, TValue>> sourceCollection)
			: base(sourceCollection, p => p.Value)
		{
		}
	}
}
