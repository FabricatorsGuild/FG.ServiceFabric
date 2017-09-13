using System;
using System.Runtime.Serialization;

namespace FG.ServiceFabric.Actors.Runtime
{
	[Serializable]
	public class SessionStateActorStateProviderException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public SessionStateActorStateProviderException()
		{
		}

		public SessionStateActorStateProviderException(string message) : base(message)
		{
		}

		public SessionStateActorStateProviderException(string message, Exception inner) : base(message, inner)
		{
		}

		protected SessionStateActorStateProviderException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}