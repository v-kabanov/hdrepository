//-----------------------------------------------------------------------------
// <created>2/15/2010 5:16:08 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		Descriptor of a data folder (any level).
	/// </summary>
	public interface IRepoFileContainerDescriptor
	{
		/// <summary>
		///		Inclusive
		/// </summary>
		DateTime Start
		{ get; }

		/// <summary>
		///		Exclusive
		/// </summary>
		DateTime End
		{ get; }

		/// <summary>
		///		Path relative to the data folders root.
		/// </summary>
		string RelativePath
		{ get; }

		/// <summary>
		///		0 - leaf level (contains files)
		///		1.. - upper levels
		/// </summary>
		int Level
		{ get; }
	}
}
