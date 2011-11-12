using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace bfs.Repository.Util
{
	/// <summary>
	///		Read only collection wrapper casting elements to a super type
	/// </summary>
	/// <typeparam name="Source">
	///		The type contained in wrapped collection
	/// </typeparam>
	/// <typeparam name="Target">
	///		The type to expose
	/// </typeparam>
	[DebuggerDisplay("Count = {Count}")]
	public class ReadOnlyCollection<Source, Target> : ICollection<Target>
		where Source : Target
	{
		private ICollection<Source> _source;

		public ReadOnlyCollection(ICollection<Source> source)
		{
			_source = source;
		}

		public void Add(Target item)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(Target item)
		{
			return _source.Contains((Source)item);
		}

		public void CopyTo(Target[] array, int arrayIndex)
		{
			((ICollection)_source).CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _source.Count; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(Target item)
		{
			throw new NotSupportedException();
		}

		public IEnumerator<Target> GetEnumerator()
		{
			return _source.Cast<Target>().GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _source.GetEnumerator();
		}
	}
}
