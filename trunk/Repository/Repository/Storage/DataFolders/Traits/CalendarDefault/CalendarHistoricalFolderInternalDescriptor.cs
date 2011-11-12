using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bfs.Repository.Storage.DataFolders.Traits.CalendarDefault
{
	internal class CalendarHistoricalFolderInternalDescriptor
	{
		internal DateTime Start
		{ get; set; }

		internal DateTime End
		{ get; set; }

		internal string Name
		{ get; set; }

		/*internal static RepoFileContainerDescriptor GetFullDescriptor(
			CalendarHistoricalFolderInternalDescriptor descriptor
			, RepoFileContainerDescriptor parent)
		{
			RepoFileContainerDescriptor retval = new RepoFileContainerDescriptor()
			{
				Start = descriptor.Start,
				End = descriptor.End,
			};
			string path;
			if (parent != null)
			{
				if (!string.IsNullOrEmpty(parent.RelativePath))
				{
					path = Path.Combine(parent.RelativePath, descriptor.Name);
				}
				else
				{
					path = descriptor.Name;
				}
				retval.Level = parent.Level + 1;
			}
			else
			{
				retval.Level = 
			}
			retval.RelativePath = path;
			return retval;
		}*/
	}
}
