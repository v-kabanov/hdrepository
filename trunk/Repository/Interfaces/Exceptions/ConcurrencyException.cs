using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.Runtime.Serialization;
using System.Security.Permissions;

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
	[Serializable]
	public class ConcurrencyException : ApplicationException
	{
		private const string _serializationKeyTargetFolderPath = "folderRelPath";

		public ConcurrencyException(IRepositoryFolder folder, string message, string technicalInfo, Exception innerException)
			: base(message, technicalInfo, innerException)
		{
			TargetFolder = folder;
			TargetFolderRelativePath = folder.LogicalPath;
		}

		public ConcurrencyException(IRepositoryFolder folder, string message)
			: this(folder, message, string.Empty, null)
		{
		}

		protected ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.TargetFolderRelativePath = info.GetString(_serializationKeyTargetFolderPath);
        }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue(_serializationKeyTargetFolderPath, this.TargetFolderRelativePath);

			// Note: if "List<T>" isn't serializable you may need to work out another
			//       method of adding your list, this is just for show...
			//info.AddValue("ValidationErrors", this.ValidationErrors, typeof(IList<string>));

			// MUST call through to the base class to let it save its own state
			base.GetObjectData(info, context);
		}

		/// <summary>
		///		Get target of the concurrent access attempt.
		/// </summary>
		/// <remarks>
		///		Note that this property is not serializable and will be null if transferred through remoting etc.
		/// </remarks>
		public IRepositoryFolder TargetFolder
		{ get; private set; }


		/// <summary>
		///		Path (relative to repository root folder) of the folder which caused the exception.
		/// </summary>
		public string TargetFolderRelativePath
		{ get; private set; }
	}
}
