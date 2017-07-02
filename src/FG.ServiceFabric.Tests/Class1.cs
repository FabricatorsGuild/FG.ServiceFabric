using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public async Task Can_has_test()
        {
            var fabricRuntime = new MockFabricRuntime("Overlord");
            var mockActorStateProvider = new MockActorStateProvider(fabricRuntime);
            fabricRuntime.SetupActor((service, actorId) => new FG.ServiceFabric.Tests.Actor.EventStoredActor(service, actorId), createStateProvider: () => mockActorStateProvider);

            var actor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IEventStoredActor>(new ActorId("testivus"));
            await actor.CreateAsync(new MyCommand { AggretateRootId = Guid.NewGuid(), Value = "festivus" });
        }
    }
}
