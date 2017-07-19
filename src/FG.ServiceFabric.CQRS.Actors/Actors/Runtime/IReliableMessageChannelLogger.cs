using System;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IOutboundMessageChannelLogger
    {
        void FailedToSendMessage(ActorId actorId, Uri serviceUri, Exception ex);
        void MovedToDeadLetters(int depth);
    }

    public class NullOpOutboundMessageChannelLogger : IOutboundMessageChannelLogger
    {
        public void FailedToSendMessage(ActorId actorId, Uri serviceUri, Exception ex)
        {
        }

        public void MovedToDeadLetters(int depth)
        {
        }
    }
}