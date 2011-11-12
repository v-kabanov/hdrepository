using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Storage
{
	/// <summary>
	///		The class holds information about data folder reader seek outcome.
	/// </summary>
	public class FolderSeekStatus
	{
		/// <summary>
		///		Seek status enumeration.
		/// </summary>
		public enum PositionStatus
		{
			/// <summary>
			///		No problems
			/// </summary>
			Success,
			/// <summary>
			///		A problem which did not lead to complete failure: data file containing item recorded in the position as having been read
			///		was not found
			/// </summary>
			FileNotFound,
			/// <summary>
			///		A problem which did not lead to complete failure: data item recorded in the position as having been read
			///		was not found (by <see cref="IFolderReadingPosition.Time"/> and <see cref="IFolderReadingPosition.NumberOfItemsWithTheTimestampRead"/>
			/// </summary>
			DataItemNotFound,
			/// <summary>
			///		A problem which did not lead to complete failure: data item recorded in the position as having been read
			///		was found (by <see cref="IFolderReadingPosition.Time"/> and <see cref="IFolderReadingPosition.NumberOfItemsWithTheTimestampRead"/>,
			///		but its verification hash (<see cref="IFolderReadingPosition.VerificationLastReadItemHash"/>) did not match with actual
			///		<see cref="IDataItem.GetBusinessHashCode()"/>
			/// </summary>
			DataItemHashMismatch,
		}

		private string _message;

		public FolderSeekStatus(string folderKey, PositionStatus status)
			: this(folderKey, status, string.Empty)
		{
		}

		public FolderSeekStatus(string folderKey, PositionStatus status, string message)
		{
			FolderKey = folderKey;
			Status = status;
			WarningMessage = message;
		}

		/// <summary>
		///		Get repository folder key (<see cref="IRepositoryFolder.FolderKey"/>) which uniquely identifies a folder within its repository.
		/// </summary>
		public string FolderKey
		{ get; private set; }

		/// <summary>
		///		Get or set additional info if any; returns <code>string.Empty</code> if no additional info
		/// </summary>
		public string WarningMessage
		{
			get { return _message; }
			private set
			{
				if (value == null)
				{
					_message = string.Empty;
				}
				else
				{
					_message = value;
				}
			}
		}

		public PositionStatus Status
		{ get; private set; }

		public override string ToString()
		{
			return Enum.GetName(typeof(PositionStatus), this.Status);
		}
	}
}
