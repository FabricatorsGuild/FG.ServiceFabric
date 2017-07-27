using System;
using FG.ServiceFabric.Tests.Actor;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors.Runtime;
using NUnit.Framework;

namespace FG.ServiceFabric.Actors.Runtime.Tests
{
	public class ActorStateProviderHelperTests
    {
        [Test]
        public void CreateDefaultStateProvider_should_create_instance_of_IActorStateProvider()
        {
            var actorName = typeof(ActorDemoActorService).FullName.Replace("ActorDemoActorService", "ActorDemo");
            var assembly = typeof(ActorDemoActorService).Assembly;
            var actorType = assembly.GetType(actorName);
            var actorTypeInformation = ActorTypeInformation.Get(actorType);

            actorTypeInformation.Should().NotBeNull();

            var defaultStateProvider = ActorStateProviderHelper.CreateDefaultStateProvider(actorTypeInformation);

            defaultStateProvider.Should().NotBeNull();
        }
    }
}
