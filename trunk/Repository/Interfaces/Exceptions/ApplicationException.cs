using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace bfs.Repository.Exceptions
{
	/// <summary>
	///		Base class for exceptions providing user-friendly and more detailed technical problem descriptions.
	/// </summary>
	/// <remarks>
	///		User-friendly message will be available through <see cref="Exception.Message"/>.
	/// </remarks>
	[Serializable]
	public class ApplicationException : Exception
	{
		private const string _serializationKeyTechnicalInfo = "tinfo";

		public ApplicationException(string userMessage, string technicalInfo, Exception inner)
			: base(userMessage, inner)
		{
			TechnicalInfo = technicalInfo;
		}

		protected ApplicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.TechnicalInfo = info.GetString(_serializationKeyTechnicalInfo);
        }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}

			info.AddValue(_serializationKeyTechnicalInfo, this.TechnicalInfo);

			// Note: if "List<T>" isn't serializable you may need to work out another
			//       method of adding your list, this is just for show...
			//info.AddValue("ValidationErrors", this.ValidationErrors, typeof(IList<string>));

			// MUST call through to the base class to let it save its own state
			base.GetObjectData(info, context);
		}

		/// <summary>
		///		Get more detailed technical error description
		/// </summary>
		public string TechnicalInfo
		{ get; protected set; }

		/// <summary>
		///		Get user-friendly problem summary.
		/// </summary>
		public string UserMessage
		{ get { return base.Message; } }
	}
}
