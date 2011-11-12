//-----------------------------------------------------------------------------
// <created>2/18/2010 3:46:02 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using bfs.Repository.Interfaces;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	[Serializable]
	public class FolderContainsSubfoldersException : Exception
	{
		private const string _serializationKeyTargetFolderPath = "folderRelPath";

		public FolderContainsSubfoldersException(IRepositoryFolder folder, string message)
			: base(message: message)
		{
			Folder = folder;
			FolderRelativePath = folder.LogicalPath;
		}

		protected FolderContainsSubfoldersException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.FolderRelativePath = info.GetString(_serializationKeyTargetFolderPath);
        }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue(_serializationKeyTargetFolderPath, this.FolderRelativePath);

			// Note: if "List<T>" isn't serializable you may need to work out another
			//       method of adding your list, this is just for show...
			//info.AddValue("ValidationErrors", this.ValidationErrors, typeof(IList<string>));

			// MUST call through to the base class to let it save its own state
			base.GetObjectData(info, context);
		}

		/// <summary>
		///		Get repository folder which caused the exception.
		/// </summary>
		/// <remarks>
		///		Note that this property is not serializable and will be null if transferred through remoting etc.
		/// </remarks>
		public IRepositoryFolder Folder
		{ get; private set; }

		/// <summary>
		///		Path (relative to repository root folder) of the folder which caused the exception.
		/// </summary>
		public string FolderRelativePath
		{ get; private set; }
	}
}
