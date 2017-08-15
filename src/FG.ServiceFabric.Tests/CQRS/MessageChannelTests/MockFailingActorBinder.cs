using System;
using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
{
    internal class MockFailingActorBinder : IReceiverActorBinder
    {
        public IReliableMessageReceiverActor Bind(ActorReference actorReference)
        {
            throw new Exception("Boom!");
        }
    }
}