using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
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
            return _fabricRuntime.ActorProxyFactory.CreateActorProxy<IIndexActor>(actorReference.ServiceUri,
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

        protected override void SetupRuntime()
        {
            ForTestIndexActor.Setup(FabricRuntime);
            base.SetupRuntime();
        }

        public OutboundReliableMessageChannel OutboundChannel { get; set; }
    }
}