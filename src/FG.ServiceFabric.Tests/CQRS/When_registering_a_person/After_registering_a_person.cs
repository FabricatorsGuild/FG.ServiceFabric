using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.PersonActor;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.When_registering_a_person
{
    public class After_event_is_raised : TestBase
    {
        protected override void SetupRuntime()
        {
            ForTestPersonActor.Setup(FabricRuntime);
            ForTestPersonIndexActor.Setup(FabricRuntime);
            base.SetupRuntime();
        }

        private readonly Guid _aggregateRootId = Guid.NewGuid();
        [SetUp]
        public async Task RaiseEvent()
        {
            var personProxy = ActorProxyFactory.CreateActorProxy<IPersonActor>(new ActorId(_aggregateRootId));
            await personProxy.RegisterAsync(new RegisterCommand { FirstName = "Stig"});
        }

        [Test]
        public async Task Then_event_is_applied()
        {
            var personServiceProxy = ActorProxyFactory.CreateActorServiceProxy<IPersonActorService>(
                serviceUri: FabricRuntime.ApplicationUriBuilder.Build("PersonActorService").ToUri(),
                actorId: new ActorId(_aggregateRootId));

            var person = await personServiceProxy.GetAsync(_aggregateRootId);

            person.Should().NotBeNull();
            person.FirstName.Should().Be("Stig");
        }


        [Test]
        public async Task Then_event_is_stored()
        {
            var personServiceProxy = ActorProxyFactory.CreateActorServiceProxy<IPersonActorService>(
                serviceUri: FabricRuntime.ApplicationUriBuilder.Build("PersonActorService").ToUri(),
                actorId: new ActorId(_aggregateRootId));

            var events = await personServiceProxy.GetAllEventHistoryAsync(_aggregateRootId);
            events.Should().Contain(x => x.EventType == typeof(PersonRegisteredEvent).FullName);
        }

        [Test]
        [Ignore("Must fix timers in mock runtime")]
        public async Task Then_person_is_indexed()
        {
            var indexProxy = ActorProxyFactory.CreateActorProxy<IPersonIndexActor>(new ActorId("PersonIndex"));
            var index = await indexProxy.ListCommandsAsync();

            index.Should().Contain(_aggregateRootId);
        }
    }
}
