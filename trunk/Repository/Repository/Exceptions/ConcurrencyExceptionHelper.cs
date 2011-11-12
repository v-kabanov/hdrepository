using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;

namespace bfs.Repository.Exceptions
{
	/// <summary>
	///		The exception thrown when an error results from data being changed concurrently.
	/// </summary>
	/// <remarks>
	///		Examples of concurrent access errors are folder being renamed while data is being read from one of its descendant
	///		folders or data file not being found when reading.
	///		The reason for the convenience static methods constructing instances for different kinds of condition returning the instance rather than
	///		throwing the exception is to make stack trace
	/// </remarks>
	internal static class ConcurrencyExceptionHelper
	{
		/// <summary>
		///		Create new instance signalling that an attempt is made to modify a folder while data in its subtree is being accessed.
		/// </summary>
		/// <param name="folder">
		///		The folder being modified
		/// </param>
		public static ConcurrencyException GetLockedFolderModificationAttempted(IRepositoryFolder folder)
		{
			string message = string.Format(Storage.StorageResources.CannotModifyFolderWhileDataIsAccessed, folder.LogicalPath);
			return new ConcurrencyException(folder, message);
		}

		/// <summary>
		///		Create new instance signalling that a data file previously known to exist was not found on disk.
		/// </summary>
		/// <param name="folder">
		///		The repo folder in which the accident occurred.
		/// </param>
		/// <param name="innerException">
		///		The <see cref="System.IO.FileNotFoundException"/> which was thrown in response to the error.
		///		Its <see cref="System.IO.FileNotFoundException.FileName"/> will be used to format the error message.
		/// </param>
		/// <returns>
		///		New <see cref="ConcurrencyException"/> instance
		/// </returns>
		public static ConcurrencyException GetFileNotFound(IRepositoryFolder folder, System.IO.FileNotFoundException innerException)
		{
			string message = string.Format(Storage.StorageResources.DataFileNotFound, innerException.FileName);
			return new ConcurrencyException(folder, Storage.StorageResources.PotentialConcurrencyIssueMessage, message, innerException);
		}

		/// <summary>
		///		Create exception instance for when timestamp of the last data item read from a data file does not match the border timestamp
		///		recorded in the file name instance.
		/// </summary>
		/// <param name="folder">
		///		The target folder containig the data file.
		/// </param>
		/// <param name="fileName">
		///		The file name as presented by the file name instance contained in the repository object model
		/// </param>
		/// <param name="expectedTime">
		///		The border timestamp as recorded in the file name instance.
		/// </param>
		/// <param name="actualTime">
		///		The actual timestamp of the last data item in the file.
		/// </param>
		/// <returns>
		///		New <see cref="ConcurrencyException"/> instance.
		/// </returns>
		public static ConcurrencyException GetLastReadItemTimestampMismatch(IRepositoryFolder folder, string fileName, DateTime expectedTime, DateTime actualTime)
		{
			string message = string.Format(
				Storage.StorageResources.ReaderLastItemTimeInFileMismatch
				, folder.LogicalPath
				, fileName
				, expectedTime
				, actualTime);
			return new ConcurrencyException(folder, Storage.StorageResources.PotentialConcurrencyIssueMessage, message, null);
		}

		public static ConcurrencyException GetDataFileDeletionNotificationFailed(
			IRepositoryFolder folder, string fileName, FileContainerNotificationException inner)
		{
			return new ConcurrencyException(folder, Storage.StorageResources.PotentialConcurrencyIssueMessage, inner.Message, inner);
		}
	}
}
