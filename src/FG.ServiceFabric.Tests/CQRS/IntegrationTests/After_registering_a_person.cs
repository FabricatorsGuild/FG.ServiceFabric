using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.EventStoredActor;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.IntegrationTests
{
    public class When_creating_an_aggregate_root : TestBase
    {
        protected override void SetupRuntime()
        {
            ForTestEventStoredActor.Setup(FabricRuntime);
            ForTestIndexActor.Setup(FabricRuntime);
            base.SetupRuntime();
        }

        private readonly Guid _aggregateRootId = Guid.NewGuid();
        [SetUp]
        public async Task RaiseEvent()
        {
            var personProxy = ActorProxyFactory.CreateActorProxy<IEventStoredActor>(new ActorId(_aggregateRootId));
            await personProxy.CreateAsync(new CreateCommand { SomeProperty = "Stig"});
        }

        [Test]
        public async Task Then_event_is_applied()
        {
            var serviceProxy = ActorProxyFactory.CreateActorServiceProxy<IEventStoredActorService>(
                serviceUri: FabricRuntime.ApplicationUriBuilder.Build("EventStoredActorService").ToUri(),
                actorId: new ActorId(_aggregateRootId));

            var result = await serviceProxy.GetAsync(_aggregateRootId);

            result.Should().NotBeNull();
            result.SomeProperty.Should().Be("Stig");
        }

        [Test]
        public async Task Then_event_is_stored()
        {
            var serviceProxy = ActorProxyFactory.CreateActorServiceProxy<IEventStoredActorService>(
                serviceUri: FabricRuntime.ApplicationUriBuilder.Build("EventStoredActorService").ToUri(),
                actorId: new ActorId(_aggregateRootId));

            var events = await serviceProxy.GetAllEventHistoryAsync(_aggregateRootId);
            events.Should().Contain(x => x.EventType == typeof(CreatedEvent).FullName);
        }
    }
}
