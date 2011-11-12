using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace bfs.Repository.Util
{
	public interface IDirectedEnumerable<T> : IEnumerable<T>, IEnumerable
	{
		EnumerationDirection Direction { get; }

		IDirectedEnumerable<T> Backwards();
	}
}
