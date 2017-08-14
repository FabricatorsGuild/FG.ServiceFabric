using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Testing.Tests.Mocks.Fabric;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Actors.Client
{
    // ReSharper disable once InconsistentNaming
    public class MockActorProxyFactory_tests
    {
        [Test]
        public async Task MockActorProxy_should_SaveState_after_Actor_method()
        {

            var fabricRuntime = new MockFabricRuntime("Overlord");
            var stateActions = new List<string>();
            var mockActorStateProvider = new MockActorStateProvider(fabricRuntime, stateActions);
            fabricRuntime.SetupActor(
				(service, actorId) => new ActorDemo(service, actorId), 
				createActorStateProvider: () => mockActorStateProvider, 
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue));

            // Only to get around the kinda stupid introduced 1/20 msec 'bug'
            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                var actor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return actor.SetCountAsync(5);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

	        stateActions.Should().BeEquivalentTo(new string[]
	        {
				"ContainsStateAsync - testivus - count",
				"ActorActivatedAsync - testivus",
				"SaveStateAsync - testivus - [{\"StateName\":\"count\",\"Type\":\"System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\",\"Value\":5,\"ChangeKind\":1}]"
			});
        }
        
        [Test]
        public async Task MockActorProxy_should_should_be_able_to_create_proxy_for_Actor_with_specific_ActorService()
        {
            var fabricRuntime = new MockFabricRuntime("Overlord");
            
            fabricRuntime.SetupActor<ActorDemo, ActorDemoActorService>(
                (service, actorId) => new ActorDemo(service, actorId),
                (context, actorTypeInformation, stateProvider, stateManagerFactory) => new ActorDemoActorService(context, actorTypeInformation,
                stateProvider: stateProvider, stateManagerFactory: stateManagerFactory), serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue));

            IActorDemo proxy = null;
           
            // Only to get around the kinda stupid introduced 1/20 msec 'bug'
            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                proxy = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return Task.FromResult(true);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

            proxy.Should().NotBeNull();
        }

        [Test]
        public async Task MockActorProxy_should_should_be_able_to_create_proxy_for_specific_ActorService()
        {
            var fabricRuntime = new MockFabricRuntime("Overlord");
            fabricRuntime.SetupActor<ActorDemo, ActorDemoActorService>(
                (service, actorId) => new ActorDemo(service, actorId),
                (context, actorTypeInformation, stateProvider, stateManagerFactory) => new ActorDemoActorService(context, actorTypeInformation),
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue));

            IActorDemoActorService proxy = null;
            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                proxy = fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
                  fabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri(), new ActorId("testivus"));
                return Task.FromResult(true);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

            proxy.Should().NotBeNull();
        }

        [Test]
        public async Task MockActorProxy_should_should_persist_state_across_multiple_proxies()
        {

            var fabricRuntime = new MockFabricRuntime("Overlord");
            var stateActions = new List<string>();
            var mockActorStateProvider = new MockActorStateProvider(fabricRuntime, stateActions);
            fabricRuntime.SetupActor(
                (service, actorId) => new ActorDemo(service, actorId),                
                createActorStateProvider: () => mockActorStateProvider,
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue)
			);

            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                var actor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return actor.SetCountAsync(5);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);


            var count = await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                var sameActor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return sameActor.GetCountAsync();
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);
            
            count.Should().Be(5);
        }



		[Test]
		public async Task MockActorProxy_should_activate_actor_with_custom_constructor()
		{
			var fabricRuntime = new MockFabricRuntime("Overlord");

			fabricRuntime.SetupActor<TestActor, TestActorService>(
				(service, actorId) => new TestActor(service, actorId, "Heimlich"),
				(context, actorTypeInformation, stateProvider, stateManagerFactory) => new TestActorService(context, actorTypeInformation,
				stateProvider: stateProvider, stateManagerFactory: stateManagerFactory), serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));

			var proxy = fabricRuntime.ActorProxyFactory.CreateActorProxy<ITestActor>(new ActorId("Testivus"));

			proxy.Should().NotBeNull();
		}



		public interface ITestActor : IActor
		{

		}

		public class TestActorService : ActorService
		{
			public TestActorService(
				StatefulServiceContext context,
				ActorTypeInformation actorTypeInfo,
				Func<ActorService, ActorId, ActorBase> actorFactory = null,
				Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
				IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) :
				base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
			{
			}
		}

		public class TestActor : Actor, ITestActor
		{
			public TestActor(ActorService actorService, ActorId actorId, string secretKey) : base(actorService, actorId)
			{
			}
		}

	}
}
