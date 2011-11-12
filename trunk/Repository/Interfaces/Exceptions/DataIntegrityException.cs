using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	/// <summary>
	///		Thrown when encountering inconsistent/corrupt data in the repository.
	/// </summary>
	[Serializable]
	public class DataIntegrityException : ApplicationException
	{
		public DataIntegrityException(string userMessage, string technicalInfo)
			: base(userMessage, technicalInfo, null)
		{
		}

		protected DataIntegrityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			// MUST call through to the base class to let it save its own state
			base.GetObjectData(info, context);
		}
	}
}
