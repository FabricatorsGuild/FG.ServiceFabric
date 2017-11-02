using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
{
    public class ReliableMessgeTestBase : TestBase
    {
        [SetUp]
        public void CreateOutboundChannel()
        {
            OutboundChannel = new OutboundReliableMessageChannel(new MockActorStateManager(),
                FabricRuntime.ActorProxyFactory, null, null, new MockableActorBinder<IIndexActor>(FabricRuntime.ActorProxyFactory));
        }

        protected override void SetupRuntime()
        {
            ForTestIndexActor.Setup(_fabricApplication);
            base.SetupRuntime();
        }

        public OutboundReliableMessageChannel OutboundChannel { get; set; }
    }
}