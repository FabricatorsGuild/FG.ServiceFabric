using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace FG.ServiceFabric.CQRS.Exceptions
{
    [Serializable]
    public class EventHandlerNotFoundException : Exception
    {
        public EventHandlerNotFoundException() { }
        public EventHandlerNotFoundException(string message) : base(message) { }
        
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected EventHandlerNotFoundException
        (
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}