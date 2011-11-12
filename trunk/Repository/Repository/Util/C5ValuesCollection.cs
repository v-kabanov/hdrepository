using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	/// <summary>
	///		Thin wrapper around <see cref="C5.ICollectionValue&lt;T&gt;"/>
	/// </summary>
	/// <typeparam name="T">
	///		Type of items in the collection
	/// </typeparam>
	class C5ValuesCollection<T> : IReadOnlyCollection<T>
	{
		private C5.ICollectionValue<T> _value;

		public C5ValuesCollection(C5.ICollectionValue<T> value)
		{
			_value = value;
		}

		public int Count
		{
			get { return _value.Count; }
		}

		public bool IsEmpty
		{
			get { return _value.IsEmpty; }
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _value.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
