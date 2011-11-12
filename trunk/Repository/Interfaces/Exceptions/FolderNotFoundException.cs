//-----------------------------------------------------------------------------
// <created>2/23/2010 4:10:01 PM</created>
// <author>Vasily.Kabanov</author>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	[Serializable]
	public class FolderNotFoundException : ApplicationException
	{
		private const string _serializationKeyRepoRootPath = "repoRootPath";
		private const string _serializationKeyTargetFolderPath = "folderRelPath";

		#region constructors --------------------------------------------------

		/// <summary>
		///		Create new instance
		/// </summary>
		/// <param name="message">
		/// 	User-friendly message.
		/// </param>
		/// <param name="technicalInfo">
		/// 	Detailed technical information.
		/// </param>
		/// <param name="rootRepositoryPath">
		///		Path in repository to the folder which failed to find subfolder (is relative to repository root).
		/// </param>
		/// <param name="pathToTargetFolder">
		///		Path relative to <paramref name="rootRepositoryPath"/> to the folder which was not found.
		/// </param>
		/// <param name="innerException">
		///		The inner exception
		/// </param>
		public FolderNotFoundException(
			string message
			,string technicalInfo
			, string rootRepositoryPath
			, string pathToTargetFolder
			, Exception innerException)
			//"The folder with the specified name was not found in the collection"
			: base(userMessage: message, technicalInfo: technicalInfo, inner: innerException)
		{
			RootRepositoryPath = rootRepositoryPath;
			PathToTargetFolder = pathToTargetFolder;
		}

		/// <summary>
		/// 	Constructor for serialization
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected FolderNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
			PathToTargetFolder = info.GetString(_serializationKeyTargetFolderPath);
			RootRepositoryPath = info.GetString(_serializationKeyRepoRootPath);
        }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue(_serializationKeyTargetFolderPath, PathToTargetFolder);
			info.AddValue(_serializationKeyRepoRootPath, RootRepositoryPath);

			// MUST call through to the base class to let it save its own state
			base.GetObjectData(info, context);
		}

		#endregion constructors -----------------------------------------------

		/// <summary>
		///		Get path in repository to the folder which failed to find subfolder (is relative to repository root).
		/// </summary>
		public string RootRepositoryPath
		{ get; private set; }

		/// <summary>
		///		Get path relative to <see cref="RootRepositoryPath"/> to the folder which was not found.
		/// </summary>
		public string PathToTargetFolder
		{ get; private set; }

	}
}
