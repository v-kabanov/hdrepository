using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	/// <summary>
	///		Enumeration of storage transaction notification methods, <see cref="ITransactionNotification"/>.
	/// </summary>
	[Serializable]
	public enum NotificationType
	{
		/// <summary>
		///		Corresponds to <see cref="ITransactionNotification.Prepare(void)"/>
		/// </summary>
		Preparation,
		/// <summary>
		///		Corresponds to <see cref="ITransactionNotification.TransactionCompleted(IFileSystemTransaction, bool)"/>
		/// </summary>
		Completion
	}

	/// <summary>
	///		The exception is thrown from <see cref="ITransactionNotification"/> methods.
	/// </summary>
	[Serializable]
	public class TransactionNotificationException : StorageTransactionException
	{
		private const string _serializationKeyNotificationType = "notificationType";

		public TransactionNotificationException(string message, NotificationType notificationType)
			: this(message, null, notificationType)
		{ }

		public TransactionNotificationException(string message, Exception innerException, NotificationType notificationType)
			: base(message: message, technicalInfo: string.Empty, innerException: innerException)
		{
			NotificationType = notificationType;
		}

		protected TransactionNotificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
			this.NotificationType = (NotificationType)info.GetValue(_serializationKeyNotificationType, typeof(NotificationType));
        }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue(_serializationKeyNotificationType, NotificationType, typeof(NotificationType));

			// MUST call through to the base class to let it save its own state
			base.GetObjectData(info, context);
		}

		/// <summary>
		///		What kind of notification threw the exception.
		/// </summary>
		public NotificationType NotificationType
		{ get; private set; }

	}
}
