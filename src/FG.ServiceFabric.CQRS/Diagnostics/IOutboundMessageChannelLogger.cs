using System;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Diagnostics
{
    public interface IOutboundMessageChannelLogger
    {
        void FailedToSendMessage(ActorId actorId, Uri serviceUri, Exception ex);
        void MessageSent(ActorId actorId, Uri serviceUri, string messageType, string messagePayload);
        void MessageMovedToDeadLetterQueue(string messageType, string messagePayload, int deadLetterQueueDepth);
    }
}