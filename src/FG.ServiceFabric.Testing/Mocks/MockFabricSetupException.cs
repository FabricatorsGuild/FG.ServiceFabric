using System;
using System.Runtime.Serialization;

namespace FG.ServiceFabric.Testing.Mocks
{
    [Serializable]
    public class MockFabricSetupException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public MockFabricSetupException()
        {
        }

        public MockFabricSetupException(string message) : base(message)
        {
        }

        public MockFabricSetupException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MockFabricSetupException
        (
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}