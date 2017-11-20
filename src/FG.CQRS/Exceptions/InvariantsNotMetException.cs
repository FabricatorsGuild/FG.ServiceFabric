using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace FG.CQRS.Exceptions
{
	[Serializable]
	public sealed class InvariantsNotMetException : AggregateRootException
	{
		public InvariantsNotMetException(string message) : base(message)
		{
		}

		public InvariantsNotMetException(IEnumerable<string> messages) : base(string.Join(", ", messages))
		{
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		private InvariantsNotMetException
		(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}