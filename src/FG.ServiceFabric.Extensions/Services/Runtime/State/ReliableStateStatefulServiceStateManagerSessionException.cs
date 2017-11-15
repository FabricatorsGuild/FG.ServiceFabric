using System;
using System.Runtime.Serialization;

namespace FG.ServiceFabric.Services.Runtime.State
{
	[Serializable]
	public class ReliableStateStatefulServiceStateManagerSessionException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public ReliableStateStatefulServiceStateManagerSessionException()
		{
		}

		public ReliableStateStatefulServiceStateManagerSessionException(string message) : base(message)
		{
		}

		public ReliableStateStatefulServiceStateManagerSessionException(string message, Exception inner) : base(message,
			inner)
		{
		}

		protected ReliableStateStatefulServiceStateManagerSessionException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}