using System;
using System.Collections.Generic;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    public interface IReliableMessageChannelLogger
    {
        void FailedToSendMessage(ActorId actorId, Uri serviceUri, Exception ex);
        void MovedToDeadLetters(int depth);
    }
}