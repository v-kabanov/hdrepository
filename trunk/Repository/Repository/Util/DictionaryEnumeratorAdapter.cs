using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	internal class DictionaryEnumeratorAdapter<TValue, TResult> : IEnumerator<TResult>
	{
		private IEnumerator<TValue> _sourceCollection;
		private Func<TValue, TResult> _converter;

		public DictionaryEnumeratorAdapter(
			IEnumerator<TValue> sourceCollection
			, Func<TValue, TResult> converter)
		{
			_sourceCollection = sourceCollection;
			_converter = converter;
		}

		#region IEnumerator<TResult> Members

		public TResult Current
		{
			get
			{
				return _converter(_sourceCollection.Current);
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			_sourceCollection.Dispose();
		}

		#endregion

		#region IEnumerator Members

		object System.Collections.IEnumerator.Current
		{
			get { return this.Current; }
		}

		public bool MoveNext()
		{
			return _sourceCollection.MoveNext();
		}

		public void Reset()
		{
			_sourceCollection.Reset();
		}

		#endregion
	}
}
