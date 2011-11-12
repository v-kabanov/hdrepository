using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	/// <summary>
	///		The interface expresses common time comparison information to allow polymorphic usage in reverse time
	///		scenario.
	/// </summary>
	public interface IDirectedTimeComparison : IComparer<DateTime>
	{
		/// <summary>
		///		Logically maximum value respecting direction in which time is traversed
		/// </summary>
		DateTime MaxValue
		{ get; }

		/// <summary>
		///		Logically minimum value respecting direction in which time is traversed
		/// </summary>
		DateTime MinValue
		{ get; }
	}
}
