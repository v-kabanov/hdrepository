using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;

namespace bfs.Repository.Exceptions
{
	internal static class FolderNotFoundExceptionHelper
	{
		public static FolderNotFoundException GetForWriter(
			IRepositoryFolder writersTarget, string originalRelativePath)
		{
			string message = string.Format(Storage.StorageResources.DescendantFolderNotFound, originalRelativePath, writersTarget.LogicalPath);
			string technicalInfo = string.Format(
@"Target folder was not found by its relative path {0}
in forlder {1}
repository {2}"
				, originalRelativePath
				, writersTarget.LogicalPath
				, writersTarget.Repository.RepositoryRoot);

			return new FolderNotFoundException(
				message: message
				, technicalInfo: technicalInfo
				, rootRepositoryPath: writersTarget.Repository.RepositoryRoot
				, pathToTargetFolder: originalRelativePath
				, innerException: null);
		}
	}
}
