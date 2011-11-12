using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Exceptions
{
	internal static class FoderAlreadyExistsExceptionHelper
	{
		/// <summary>
		///		Get exception to throw when asked to create a new folder or rename existing giving it a name which is equal to the name of an existing folder under
		///		the same parent.
		/// </summary>
		/// <param name="paramName">
		///		Name of method parameter which is to blame for the conflict.
		/// </param>
		/// <returns>
		///		ArgumentException
		/// </returns>
		public static ArgumentException CreateForNewOrRename(string paramName)
		{
			return new ArgumentException(message: Storage.StorageResources.FolderAlreadyExistsException, paramName: paramName);
		}

		/// <summary>
		///		Get exception to throw when asked to move a folder which already contains a child folder with the name equal to the name of the folder being moved.
		/// </summary>
		/// <param name="paramName">
		///		Name of method parameter which is to blame for the conflict.
		/// </param>
		/// <returns>
		///		ArgumentException
		/// </returns>
		public static ArgumentException CreateForMove(string paramName)
		{
			return new ArgumentException(
				message: Storage.StorageResources.FolderAlreadyContainsChildWithTheSameName
				, paramName: paramName);
		}
	}
}
