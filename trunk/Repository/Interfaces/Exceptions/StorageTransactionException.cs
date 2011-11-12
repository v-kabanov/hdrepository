using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces.Infrastructure;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	[Serializable]
	public class StorageTransactionException : ApplicationException
	{
		public StorageTransactionException(string message)
			: this(message: message, technicalInfo: string.Empty)
		{ }

		public StorageTransactionException(string message, string technicalInfo)
			: this(message: message, technicalInfo: technicalInfo, innerException: null)
		{ }

		public StorageTransactionException(string message, string technicalInfo, Exception innerException)
			: base(userMessage: message, technicalInfo: technicalInfo, inner: innerException)
		{ }

		protected StorageTransactionException(SerializationInfo info, StreamingContext context)
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
