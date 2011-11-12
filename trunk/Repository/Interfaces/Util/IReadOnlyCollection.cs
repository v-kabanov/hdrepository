using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace bfs.Repository.Util
{
	public interface IReadOnlyCollection<T> : IEnumerable<T>, IEnumerable
	{
		int Count { get; }
		bool IsEmpty { get; }
	}
}
