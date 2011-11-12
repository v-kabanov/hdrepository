//-----------------------------------------------------------------------------
// <created>2/15/2010 4:54:39 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Interfaces.Infrastructure
{
	/// <summary>
	///		Adding the ability to set root path to <see cref="IHistoricalFoldersExplorer"/>.
	/// </summary>
	public interface IHistoricalFoldersTraits : IHistoricalFoldersExplorer
	{
		/// <summary>
		///		Get or set directory tree root.
		/// </summary>
		new string RootPath
		{ get; set; }
	}
}
