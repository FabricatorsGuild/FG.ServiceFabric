using System;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    internal class NullOpOutboundMessageChannelLogger : IOutboundMessageChannelLogger
    {
        public void FailedToSendMessage(ActorId actorId, Uri serviceUri, Exception ex)
        {
        }

        public void MovedToDeadLetters(int depth)
        {
        }

        public void SentMessage(ActorId actorId, Uri serviceUri, string messageType)
        {
        }
    }
}