using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	public class C5DirectedEnumerable<T> : IDirectedEnumerable<T>
	{
		private C5.IDirectedEnumerable<T> _value;

		public C5DirectedEnumerable(C5.IDirectedEnumerable<T> value)
		{
			_value = value;
		}

		public EnumerationDirection Direction
		{
			get { return (EnumerationDirection)(int)_value.Direction; }
		}

		public IDirectedEnumerable<T> Backwards()
		{
			return new C5DirectedEnumerable<T>(_value.Backwards());
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
