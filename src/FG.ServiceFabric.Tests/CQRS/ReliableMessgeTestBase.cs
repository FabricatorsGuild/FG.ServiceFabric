using System;
using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.CQRS
{
    public class MockActorBinder : IReceiverActorBinder
    {
        private readonly MockFabricRuntime _fabricRuntime;

        public MockActorBinder(MockFabricRuntime fabricRuntime)
        {
            _fabricRuntime = fabricRuntime;
        }

        public IReliableMessageReceiverActor Bind(ActorReference actorReference)
        {
            return _fabricRuntime.ActorProxyFactory.CreateActorProxy<IPersonIndexActor>(actorReference.ServiceUri,
                actorReference.ActorId, actorReference.ListenerName);
        }
    }

    public class ReliableMessgeTestBase : TestBase
    {
        [SetUp]
        public void CreateOutboundChannel()
        {
            OutboundChannel = new OutboundReliableMessageChannel(new MockActorStateManager(),
                FabricRuntime.ActorProxyFactory, null, null, new MockActorBinder(FabricRuntime));
        }

        public OutboundReliableMessageChannel OutboundChannel { get; set; }
    }
}