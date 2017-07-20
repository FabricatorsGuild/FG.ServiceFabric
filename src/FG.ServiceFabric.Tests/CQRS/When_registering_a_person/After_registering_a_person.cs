using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.When_registering_a_person
{
    public class After_registering_a_person : TestBase
    {
        private readonly Guid _aggregateRootId = Guid.NewGuid();
        [SetUp]
        public async Task RegisterPerson()
        {
            var personProxy = ActorProxyFactory.CreateActorProxy<IPersonActor>(new ActorId(_aggregateRootId));
            await personProxy.RegisterAsync(new RegisterCommand { FirstName = "Stig"});
        }

        [Test]
        public async Task Then_person_is_registeredAsync()
        {
            var personServiceProxy = ActorProxyFactory.CreateActorServiceProxy<IPersonActorService>(
                serviceUri: new Uri("fabric:/FG.ServiceFabric.Tests.Application/PersonActorService"),
                actorId: new ActorId(_aggregateRootId));

            var person = await personServiceProxy.GetAsync(_aggregateRootId);

            person.Should().NotBeNull();
            person.Name.Should().Be("Stig");
        }

        [Test]
        [Ignore("Must fix timers in mock runtime")]
        public async Task Then_person_is_indexed()
        {
            var indexProxy = ActorProxyFactory.CreateActorProxy<IPersonIndexActor>(new ActorId("PersonIndex"));
            var index = await indexProxy.ListReceivedCommands();

            index.Should().Contain(_aggregateRootId);
        }
    }
}
