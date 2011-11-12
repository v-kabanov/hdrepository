//-----------------------------------------------------------------------------
// <copyright file="IncompatibleVersionException.cs" company="BFS">
//      Copyright © 2010 Vasily Kabanov
//      All rights reserved.
// </copyright>
// <created>2/2/2010 4:13:04 PM</created>
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
	public class IncompatibleVersionException : ApplicationException
	{
		public IncompatibleVersionException(string message, string technicalInfo, Exception inner)
			: base(userMessage: message, technicalInfo: technicalInfo, inner: null)
		{
		}

		protected IncompatibleVersionException(SerializationInfo info, StreamingContext context)
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
