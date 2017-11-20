using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace FG.CQRS.Exceptions
{
	[Serializable]
	public class HandlerNotFoundException : Exception
	{
		public HandlerNotFoundException()
		{
		}

		public HandlerNotFoundException(string message) : base(message)
		{
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		protected HandlerNotFoundException
		(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}