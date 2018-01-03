using System;
using System.Runtime.Serialization;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    [Serializable]
    public class StateSessionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public StateSessionException()
        {
        }

        public StateSessionException(string message) : base(message)
        {
        }

        public StateSessionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected StateSessionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}