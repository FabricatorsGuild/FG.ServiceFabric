using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace FG.CQRS.Exceptions
{
    [Serializable]
    public class AggregateRootNotFoundException : Exception
    {
        public AggregateRootNotFoundException() { }
        public AggregateRootNotFoundException(Guid aggregatRootId) : base($"Trying to read nonexisting aggregate with id: {aggregatRootId}") { }
        
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected AggregateRootNotFoundException
        (
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }
}