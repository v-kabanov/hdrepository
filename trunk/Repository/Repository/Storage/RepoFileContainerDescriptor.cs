//-----------------------------------------------------------------------------
// <copyright file="RepoFileContainerDescriptor.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/16/2010 9:47:56 AM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;

namespace bfs.Repository.Storage
{
	internal class RepoFileContainerDescriptor : IRepoFileContainerDescriptor
	{
		#region IRepoFileContainerDescriptor Members

		public DateTime Start
		{
			get;
			set;
		}

		public DateTime End
		{
			get;
			set;
		}

		public string RelativePath
		{
			get;
			set;
		}

		public int Level
		{
			get;
			set;
		}

		#endregion

		public override string ToString()
		{
			return string.Format("FileContainerDescriptor: relPath = {0}, level = {1}, start = {2}, end = {3}"
				, this.RelativePath, this.Level, this.Start, this.End);
		}

		public static int CompareFileContainers(
			IRepoFileContainerDescriptor c1, IRepoFileContainerDescriptor c2)
		{
			return DateTime.Compare(c1.Start, c1.Start);
		}

		internal static void SortDataFolders(List<IRepoFileContainerDescriptor> folders)
		{
			folders.Sort(new Comparison<IRepoFileContainerDescriptor>(CompareFileContainers));
		}
	}
}
