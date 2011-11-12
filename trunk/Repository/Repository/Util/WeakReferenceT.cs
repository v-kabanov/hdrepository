using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	class WeakReferenceT<T>
		where T : class
	{
		private WeakReference _reference;

		public WeakReferenceT(T obj)
		{
			_reference = new WeakReference(obj);
		}

		public T Target
		{ get { return (T)_reference.Target; } }

		public bool IsAlive
		{ get { return _reference.IsAlive; } }
	}
}
