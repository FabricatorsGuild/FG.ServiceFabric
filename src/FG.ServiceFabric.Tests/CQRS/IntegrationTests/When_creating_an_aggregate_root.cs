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
	    protected string ApplicationName => @"Overlord";
        protected override void SetupRuntime()
        {
            ForTestEventStoredActor.Setup(FabricRuntime, this.ApplicationName);
            ForTestIndexActor.Setup(FabricRuntime, this.ApplicationName);
            base.SetupRuntime();
        }

        private readonly Guid _aggregateRootId = Guid.NewGuid();
        [SetUp]
        public async Task RaiseEvent()
        {
            var proxy = ActorProxyFactory.CreateActorProxy<IEventStoredActor>(new ActorId(_aggregateRootId));
            await proxy.CreateAsync(new CreateCommand { SomeProperty = "Stig"});
        }

        [Test]
        public async Task Then_event_is_applied()
        {
            var serviceProxy = ActorProxyFactory.CreateActorServiceProxy<IEventStoredActorService>(
                serviceUri: FabricRuntime.GetApplicationUriBuilder("Overlord").Build("EventStoredActorService").ToUri(),
                actorId: new ActorId(_aggregateRootId));

            var result = await serviceProxy.GetAsync(_aggregateRootId);

            result.Should().NotBeNull();
            result.SomeProperty.Should().Be("Stig");
        }

        [Test]
        public async Task Then_event_is_stored()
        {
            var serviceProxy = ActorProxyFactory.CreateActorServiceProxy<IEventStoredActorService>(
                serviceUri: FabricRuntime.GetApplicationUriBuilder("Overlord").Build("EventStoredActorService").ToUri(),
                actorId: new ActorId(_aggregateRootId));

            var events = await serviceProxy.GetAllEventHistoryAsync(_aggregateRootId);
            events.Should().Contain(x => x.EventType == typeof(CreatedEvent).FullName);
        }
    }
}
