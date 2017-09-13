using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Testing.Tests.Actors.Runtime
{
	public class When_starting_actor
	{
		protected MockFabricRuntime _fabricRuntime;
		protected MockServiceDefinition _actorDemoServiceDefinition;

		protected IDictionary<string, string> _state = new ConcurrentDictionary<string, string>();

		[SetUp]
		public void CreateActorsWithActorService()
		{
			_fabricRuntime = new MockFabricRuntime("Overlord");

			_actorDemoServiceDefinition = MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue);
			_fabricRuntime.SetupActor<ActorWithReminderDemo, ActorService>(
				(service, actorId) => new ActorWithReminderDemo(service, actorId),
				createActorService: (context, information, provider, factory) => new FG.ServiceFabric.Actors.Runtime.ActorService(context, information, stateProvider: provider, stateManagerFactory: factory),
				createActorStateProvider: (context, actorInfo) => new StateSessionActorStateProvider(context, CreateStateManager(context), actorInfo),
				serviceDefinition: _actorDemoServiceDefinition);
		}

		protected virtual void Setup()
		{
			
		}

		private IStateSessionManager CreateStateManager(StatefulServiceContext context)
		{
			var stateManager = new InMemoryStateSessionManager(
					StateSessionHelper.GetServiceName(context.ServiceName),
					context.PartitionId,
					StateSessionHelper.GetPartitionInfo(context,
						() => new MockPartitionEnumerationManager(_fabricRuntime)).GetAwaiter().GetResult(),
					_state
				);
			return stateManager;
		}

	}

	// ReSharper disable InconsistentNaming
	public class When_actor_has_not_been_activated_before : When_starting_actor
	{
		[Test]
		public void reminders_should_be_loaded_on_startup()
		{
			var actorWithReminderDemo = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorWithReminderDemo>(new ActorId("testivus"));

			var result = actorWithReminderDemo.GetCountAsync().GetAwaiter().GetResult();

			//result.Should().BeGreaterThan(5);
			Console.WriteLine(result);
		}

	}

	public class When_actor_has_not_been_activated_before_with_existing_state : When_starting_actor
	{
		protected override void Setup()
		{
			_state.Add(@"Overlord-ActorService_range-6_ACTORID_S%3Atestivus.json", @"{
				  ""id"": ""S:testivus"",
				  ""serviceTypeName"": ""Overlord-ActorService"",
				  ""partitionKey"": ""range-6"",
				  ""stateName"": ""Overlord-ActorService_range-6_ACTORID_S:testivus"",
				  ""state"": {
					""Value"": ""testivus"",
					""Kind"": 2
				  }
				}");
			_state.Add(@"Overlord-ActorService_range-6_ACTORREMINDER_S%3Atestivus-myReminder.json", @"{
				  ""id"": ""S:testivus-myReminder"",
				  ""serviceTypeName"": ""Overlord-ActorService"",
				  ""partitionKey"": ""range-6"",
				  ""stateName"": ""Overlord-ActorService_range-6_ACTORREMINDER_S:testivus-myReminder"",
				  ""state"": {
					""ActorId"": {
					  ""Kind"": 2,
					  ""StringId"": ""testivus""
					},
					""Name"": ""myReminder"",
					""DueTime"": ""00:00:03"",
					""Period"": ""00:00:01"",
					""State"": ""PFRpY2tVcFJlbWluZGVyU3RhdGUgeG1sbnM9Imh0dHA6Ly9zY2hlbWFzLmRhdGFjb250cmFjdC5vcmcvMjAwNC8wNy9GRy5TZXJ2aWNlRmFicmljLlRlc3RzLkFjdG9yIiB4bWxuczppPSJodHRwOi8vd3d3LnczLm9yZy8yMDAxL1hNTFNjaGVtYS1pbnN0YW5jZSI+PFN0YXJ0ZWRUaW1lPjIwMTctMDktMTFUMDk6NDQ6MDkuNzQxNTg5Nlo8L1N0YXJ0ZWRUaW1lPjxWYWx1ZVRvVGlja0Rvd24+OTwvVmFsdWVUb1RpY2tEb3duPjwvVGlja1VwUmVtaW5kZXJTdGF0ZT4="",
					""UtcCreationTime"": ""2017-09-11T09:44:09.9372314Z""
				  }
				}");
		}

		[Test]
		public void reminders_should_be_loaded_on_startup()
		{
			var actorWithReminderDemo = _fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorWithReminderDemo>(new ActorId("testivus"));

			var result = actorWithReminderDemo.GetCountAsync().GetAwaiter().GetResult();

			//result.Should().BeGreaterThan(5);
			Console.WriteLine(result);
		}

	}

	// ReSharper restore InconsistentNaming
}