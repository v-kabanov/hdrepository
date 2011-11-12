using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	/// <summary>
	///		Static class providing access to directed time comparers
	/// </summary>
	/// <remarks>
	///		The reason for existence is to replace ugly conditional logic and improve performance.
	/// </remarks>
	internal static class TimeComparer
	{
		internal class ForwardComparer : IDirectedTimeComparison
		{
			public int Compare(DateTime x, DateTime y)
			{
				return DateTime.Compare(x, y);
			}

			public DateTime MaxValue
			{ get { return DateTime.MaxValue; } }

			public DateTime MinValue
			{ get { return DateTime.MinValue; } }
		}

		internal class BackwardComparer : IDirectedTimeComparison
		{
			public int Compare(DateTime x, DateTime y)
			{
				return DateTime.Compare(y, x);
			}

			public DateTime MaxValue
			{ get { return DateTime.MinValue; } }

			public DateTime MinValue
			{ get { return DateTime.MaxValue; } }
		}

		internal static readonly IDirectedTimeComparison Forward = new ForwardComparer();
		internal static readonly IDirectedTimeComparison Backard = new BackwardComparer();

		internal static IDirectedTimeComparison GetComparer(Util.EnumerationDirection direction)
		{
			return GetComparer(direction == EnumerationDirection.Backwards);
		}

		internal static IDirectedTimeComparison GetComparer(bool backward)
		{
			return backward ? Backard : Forward;
		}
	}
}
