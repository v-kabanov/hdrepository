using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;

namespace bfs.Repository.Exceptions
{
	internal static class FolderContainsSubfoldersExceptionHelper
	{
		public static FolderContainsSubfoldersException GetCannotDelete(IRepositoryFolder folder)
		{
			return new FolderContainsSubfoldersException(
				folder
				, string.Format(Storage.StorageResources.CannotDeleteFolderWithSubfolders, folder.LogicalPath)
			);
		}
	}
}
