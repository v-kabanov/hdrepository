using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Storage;
using bfs.Repository.Interfaces;

namespace RepositoryTests.Mock
{
	public class MultiLevelDataRouter : IDataRouter
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subfolderCount">
		/// </param>
		/// <param name="levelCount">
		///		Number of levels below writer's root, must be 1 or greater
		/// </param>
		public MultiLevelDataRouter(int subfolderCount, int levelCount)
		{
			if (subfolderCount <= 0 || levelCount <= 1)
			{
				throw new ArgumentException();
			}
			this.SubfolderCount = subfolderCount;
			this.LevelCount = levelCount;
		}

		public int SubfolderCount
		{ get; private set; }

		// 1 means one level under the root node
		public int LevelCount
		{ get; private set; }

		/// <summary>
		///		Total folders count including root
		/// </summary>
		public int SubtreeFolderCount
		{ get { return GetTotalFolderCountInTree(LevelCount); } }

		public string GetRelativePath(IDataItem dataItem)
		{
			int val = ((Mock.TestDataItem)dataItem).ValInt;
			int ord = val % SubtreeFolderCount; // ordinal of target node in the whole subtree

			int targetLevel = GetLevelOrdinalInSubtree(ord);

			int[] pathOrdinals = new int[targetLevel];

			if (targetLevel > 0)
			{
				int ordinalOnTargetLevel = ord - GetTotalFolderCountInTree(targetLevel - 1);
				//pathOrdinals[targetLevel - 1] = ordinalOnTargetLevel;

				for (int n = targetLevel - 1; n >= 0; --n)
				{
					pathOrdinals[n] = ordinalOnTargetLevel % SubfolderCount;
					ordinalOnTargetLevel = ordinalOnTargetLevel / SubfolderCount;
				}
			}
			StringBuilder bld = new StringBuilder(10);
			for (int n = 0; n < targetLevel; ++n)
			{
				bld.Append(pathOrdinals[n]).Append(RepositoryFolder.logicalPathSeparator);
			}

			if (targetLevel > 0)
			{
				--bld.Length;
			}
			return bld.ToString();
		}

		private int GetFirstNodeOrdinal(int level)
		{
			if (level == 0)
				return 0;
			return GetTotalFolderCountInTree(level - 1);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="level">
		///		Level ordinal; 0 - root (single root folder)
		/// </param>
		/// <returns></returns>
		private int GetTotalFolderCountInTree(int level)
		{
			// level = 1, subfolders = 3, return 4
			// level = 0, subfolders = 3, return 1
			return ((int)Math.Pow(SubfolderCount, level + 1) - 1) / (SubfolderCount - 1);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nodeOrdinalInSubtree">
		///		Integer from 0 to {SubtreeFolderCount - 1} inclusive
		/// </param>
		/// <returns>
		///		when SubfolderCount == 2:
		///			F(2) == 1
		///			F(6) == 2
		///		when SubfolderCount == 3:
		///			F(3) == 1
		///			F(4) == 2
		/// </returns>
		private int GetLevelOrdinalInSubtree(int nodeOrdinalInSubtree)
		{
			// Subfolders = 2, levels = 1, ordinal = 2; target level = 1	- log(2 * 1 + 1, 2) = 1
			// Subfolders = 2, levels = 1, ordinal = 0; target level = 0	- log(0 + 1, 2) = 0
			// Subfolders = 2, levels = 2, ordinal = 6; target level = 2	- log(6 * 1 + 1, 2) = 2
			// Subfolders = 2, levels = 2, ordinal = 1; target level = 1
			// Subfolders = 2, levels = 2, ordinal = 0; target level = 0
			//return (int)Math.Floor(Math.Log(nodeOrdinalInSubtree + 1, SubfolderCount));
			return (int)Math.Floor(Math.Log(nodeOrdinalInSubtree * (SubfolderCount - 1) + 1, SubfolderCount));
		}
	}
}
