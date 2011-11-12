using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Exceptions
{
	internal static class FileContainerNotificationExceptionHelper
	{
		public static FileContainerNotificationException GetDeletionOfUnknownFile(DateTime fileKey)
		{
			string message = string.Format(Storage.StorageResources.ContainerNotifiedOfDeletionOfUnknownFile, fileKey.Ticks, fileKey);
			return new FileContainerNotificationException(message);
		}
	}
}
