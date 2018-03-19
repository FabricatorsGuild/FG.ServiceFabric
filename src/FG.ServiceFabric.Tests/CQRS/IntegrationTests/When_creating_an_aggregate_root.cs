using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
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
        public async Task Then_event_is_applied()
        {
            var serviceProxy = ActorProxyFactory.CreateActorServiceProxy<FG.ServiceFabric.Tests.EventStoredActor.Interfaces.IEventStoredActorService>(
                _fabricApplication.ApplicationUriBuilder.Build("EventStoredActorService").ToUri(),
                new ActorId(_aggregateRootId));

            var result = await serviceProxy.GetAsync(_aggregateRootId);

            result.Should().NotBeNull();
            result.SomeProperty.Should().Be("Stig");
        }

        [Test]
        public async Task Then_event_is_stored()
        {
            var serviceProxy = ActorProxyFactory.CreateActorServiceProxy<FG.ServiceFabric.Tests.EventStoredActor.Interfaces.IEventStoredActorService>(
                _fabricApplication.ApplicationUriBuilder.Build("EventStoredActorService").ToUri(),
                new ActorId(_aggregateRootId));

            var events = await serviceProxy.GetAllEventHistoryAsync(_aggregateRootId);
            events.Should().Contain(x => x.EventType == typeof(CreatedEvent).FullName);
        }
    }
}