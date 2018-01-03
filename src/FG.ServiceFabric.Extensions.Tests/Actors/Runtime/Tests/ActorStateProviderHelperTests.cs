using FG.ServiceFabric.Tests.Actor.WithoutInternalErrors;
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
            var actorName = typeof(ActorDemo).FullName;
            var assembly = typeof(ActorDemo).Assembly;
            var actorType = assembly.GetType(actorName);
            var actorTypeInformation = ActorTypeInformation.Get(actorType);

            actorTypeInformation.Should().NotBeNull();

            var defaultStateProvider = ActorStateProviderHelper.CreateDefaultStateProvider(actorTypeInformation);

            defaultStateProvider.Should().NotBeNull();
        }
    }
}