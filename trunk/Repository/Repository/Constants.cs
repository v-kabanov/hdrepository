//-----------------------------------------------------------------------------
// <copyright file="Constants.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/2/2010 4:11:05 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace bfs.Repository
{
	static internal class Constants
	{
		public const int RepoFileHeaderCurrentVersion = 1;
		/// <summary>
		///		Invariant level index of leaf data folders.
		/// </summary>
		public const int DataFolderLevelLeaf = 0;
		public const int DefaultDataItemsPerFile = 50000;
	}
}
