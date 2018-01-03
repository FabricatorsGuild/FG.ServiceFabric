using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.CQRS.IntegrationTests
{
    public class When_sending_a_reliable_message_as_result_of_an_event : TestBase
    {
        private readonly Guid _aggregateRootId = Guid.NewGuid();

        protected override void SetupRuntime()
        {
            ForTestEventStoredActor.Setup(_fabricApplication);
            ForTestIndexActor.Setup(_fabricApplication);
            base.SetupRuntime();
        }

        [SetUp]
        public async Task RaiseEvent()
        {
            var proxy = ActorProxyFactory.CreateActorProxy<IEventStoredActor>(new ActorId(_aggregateRootId));
            await proxy.CreateAsync(new CreateCommand {SomeProperty = "Stig"});
        }

        [Test]
        [Ignore("Must fix timers in mock runtime")]
        public async Task Then_message_is_recieved()
        {
            var indexProxy = ActorProxyFactory.CreateActorProxy<IIndexActor>(new ActorId("Index"));
            var index = await indexProxy.ListCommandsAsync();

            index.Should().Contain(_aggregateRootId);
        }
    }
}