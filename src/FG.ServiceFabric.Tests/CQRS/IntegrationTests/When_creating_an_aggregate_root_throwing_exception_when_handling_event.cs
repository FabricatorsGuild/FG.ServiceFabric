using System;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.IntegrationTests
{
    public class When_creating_an_aggregate_root_throwing_exception_when_handling_event : TestBase
    {
        private readonly Guid _aggregateRootId = Guid.NewGuid();

        protected override void SetupRuntime()
        {
            ForTestEventStoredActor.Setup(_fabricApplication);
            ForTestIndexActor.Setup(_fabricApplication);
            base.SetupRuntime();
        }

        [Test]
        public void Then_exception_is_thrown_back_to_client()
        {
            var proxy = ActorProxyFactory.CreateActorProxy<IEventStoredActor>(new ActorId(_aggregateRootId));
            proxy.Awaiting(p => p.CreateInvalidAsync(new CreateInvalidCommand {SomeProperty = "Olof"}))
                .ShouldThrow<Exception>();
        }
    }
}