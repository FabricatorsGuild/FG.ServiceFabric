using System.Collections.Generic;
using FG.ServiceFabric.CQRS;

namespace FG.ServiceFabric.Actors.Runtime
{
    // TODO: Must be immutable and all that...
    public class ReliableMessageQueue
    {
        public ReliableMessageQueue()
        {
            Queue = new Queue<ReliableActorMessage>();
        }

        public Queue<ReliableActorMessage> Queue { get; set; }
    }
}