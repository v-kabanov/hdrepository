using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	[Serializable]
	public class FileContainerNotificationException : Exception
	{
		public FileContainerNotificationException(string message)
			: base(message)
		{
		}

		protected FileContainerNotificationException(SerializationInfo info, StreamingContext context)
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
