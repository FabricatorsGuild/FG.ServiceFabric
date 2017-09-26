using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Actors.Runtime.StateSession;
using FG.ServiceFabric.Actors.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Actors.Runtime
{
	namespace With_StateSessionActorStateProvider
	{

		namespace _and_InMemoryStateSession2
		{
			public abstract class TestBase<T>
				where T: ActorBase, IActorDemo
			{
				protected MockFabricRuntime FabricRuntime;

				protected readonly IDictionary<string, string> State = new ConcurrentDictionary<string, string>();

				protected TestBase()
				{
				}

				[SetUp]
				public void Setup()
				{
					State.Clear();
					FabricRuntime = new MockFabricRuntime("Overlord") { DisableMethodCallOutput = true };
					FabricRuntime.SetupActor(
						activator: CreateActor,
						createActorService: (context, information, provider, factory) => new ActorDemoActorService(context, information, stateProvider: provider),
						createActorStateProvider: (context, actorInfo) => new StateSessionActorStateProvider(context, CreateStateManager(context), actorInfo),
						serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(2, long.MinValue, long.MaxValue));

					SetupActor().GetAwaiter().GetResult();					
				}

				protected abstract T CreateActor(ActorDemoActorService service, ActorId actorId);

				[TearDown]
				public void TearDown()
				{
					Console.WriteLine($"States stored");
					Console.WriteLine($"______________________");
					foreach (var stateKey in State.Keys)
					{
						Console.WriteLine($"State: {stateKey}");
						Console.WriteLine($"{State[stateKey]}");
						Console.WriteLine($"______________________");
					}
				}

				protected virtual Task SetupActor()
				{
					return Task.FromResult(true);
				}

				protected virtual Task SetUpStates(InMemoryStateSessionManagerWithTransaction stateSessionManager)
				{
					return Task.FromResult(true);
				}

				protected T GetState<T>(string key)
				{
					return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(State[key]);
				}

				private IStateSessionManager CreateStateManager(StatefulServiceContext context)
				{
					var stateManager = new InMemoryStateSessionManagerWithTransaction(
							StateSessionHelper.GetServiceName(context.ServiceName),
							context.PartitionId,
							StateSessionHelper.GetPartitionInfo(context,
								() => new MockPartitionEnumerationManager(FabricRuntime)).GetAwaiter().GetResult(),
							State
						);
					SetUpStates(stateManager);
					return stateManager;
				}
			}

			public class _without_state : TestBase<FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo>
			{
				[Test]
				public async Task _should_not_return_any_state_on_activate()
				{
					var actorProxy = FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
					var i = await actorProxy.GetCountAsync();

					i.Should().Be(1);

					State.Keys.Should().HaveCount(2);

					var actorIdSchemaKey = StateSessionHelper.GetActorIdSchemaKey(new ActorId("testivus"));
					var actorIdKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorIdStateSchemaName));
					var actorIdState = GetState<ActorStateWrapper<string>>(actorIdKey);
					actorIdState.Schema.Should().Be(StateSessionHelper.ActorIdStateSchemaName);
					actorIdState.Key.Should().Be(actorIdSchemaKey);
					actorIdState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
					actorIdState.PartitionKey.Should().Be("range-1");
					actorIdState.ActorId.Kind.Should().Be(ActorIdKind.String);
					actorIdState.ActorId.GetStringId().Should().Be("testivus");
					actorIdState.State.Should().Be(actorIdSchemaKey);

					var actorStateKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorStateSchemaName));
					var actorState = GetState<ActorStateWrapper<int>>(actorStateKey);
					actorState.Schema.Should().StartWith(StateSessionHelper.ActorStateSchemaName);
					actorState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
					actorState.PartitionKey.Should().Be("range-1");
					actorState.ActorId.Kind.Should().Be(ActorIdKind.String);
					actorState.ActorId.GetStringId().Should().Be("testivus");
					actorState.State.Should().Be(1);

				}
				protected override FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo CreateActor(ActorDemoActorService service, ActorId actorId)
				{
					return new FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo(service, actorId);
				}
			}

			public class _with_state : TestBase<FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo>
			{
				protected override async Task SetUpStates(InMemoryStateSessionManagerWithTransaction stateSessionManager)
				{
					if (stateSessionManager.GetPrivateProperty<InMemoryStateSessionManagerWithTransaction, string>("PartitionKey").Equals("range-1"))
					{
						using (var session = stateSessionManager.CreateSession())
						{
							var actorIdSchemaKey = StateSessionHelper.GetActorIdSchemaKey(new ActorId("testivus"));
							await session.SetValueAsync(StateSessionHelper.ActorIdStateSchemaName, actorIdSchemaKey, actorIdSchemaKey,
								new ActorStateValueMetadata(StateWrapperType.ActorId, new ActorId("testivus")));

							await session.SetValueAsync(StateSessionHelper.GetActorStateSchemaName("count"), actorIdSchemaKey, 5,
								new ActorStateValueMetadata(StateWrapperType.ActorState, new ActorId("testivus")));
						}
					}
					await base.SetUpStates(stateSessionManager);
				}

				[Test]
				public async Task _should_return_prior_state_on_activate()
				{
					var actorProxy = FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
					var i = await actorProxy.GetCountAsync();

					i.Should().Be(5);

					var actorIdSchemaKey = StateSessionHelper.GetActorIdSchemaKey(new ActorId("testivus"));
					var actorIdKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorIdStateSchemaName));
					var actorIdState = GetState<ActorStateWrapper<string>>(actorIdKey);
					actorIdState.Schema.Should().Be(StateSessionHelper.ActorIdStateSchemaName);
					actorIdState.Key.Should().Be(actorIdSchemaKey);
					actorIdState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
					actorIdState.PartitionKey.Should().Be("range-1");
					actorIdState.ActorId.Kind.Should().Be(ActorIdKind.String);
					actorIdState.ActorId.GetStringId().Should().Be("testivus");
					actorIdState.State.Should().Be(actorIdSchemaKey);

					var actorStateKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorStateSchemaName));
					var actorState = GetState<ActorStateWrapper<int>>(actorStateKey);
					actorState.Schema.Should().StartWith(StateSessionHelper.ActorStateSchemaName);
					actorState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
					actorState.PartitionKey.Should().Be("range-1");
					actorState.ActorId.Kind.Should().Be(ActorIdKind.String);
					actorState.ActorId.GetStringId().Should().Be("testivus");
					actorState.State.Should().Be(5);
				}

				protected override FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo CreateActor(ActorDemoActorService service, ActorId actorId)
				{
					return new FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo(service, actorId);
				}
			}

			public class _with_multiple_activated_actors : TestBase<FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo>
			{

				[Test]
				public async Task _should_return_prior_state_on_activate()
				{
					for (int j = 0; j < 100; j++)
					{
						var actorProxy = FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus-{j}"));
						var i = await actorProxy.GetCountAsync();

						var actorIdSchemaKey = StateSessionHelper.GetActorIdSchemaKey(new ActorId($"testivus-{j}"));
						var actorIdKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorIdStateSchemaName) && k.Contains(actorIdSchemaKey));
						var actorIdState = GetState<ActorStateWrapper<string>>(actorIdKey);
						actorIdState.Schema.Should().Be(StateSessionHelper.ActorIdStateSchemaName);
						actorIdState.Key.Should().Be(actorIdSchemaKey);
						actorIdState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
						actorIdState.ActorId.Kind.Should().Be(ActorIdKind.String);
						actorIdState.ActorId.GetStringId().Should().Be($"testivus-{j}");
						actorIdState.State.Should().Be(actorIdSchemaKey);

						var actorStateKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorStateSchemaName) && k.Contains(actorIdSchemaKey));
						var actorState = GetState<ActorStateWrapper<int>>(actorStateKey);
						actorState.Schema.Should().StartWith(StateSessionHelper.ActorStateSchemaName);
						actorState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
						actorState.ActorId.Kind.Should().Be(ActorIdKind.String);
						actorState.ActorId.GetStringId().Should().Be($"testivus-{j}");
						actorState.State.Should().Be(1);

					}

					State.Should().HaveCount(200);
				}
				protected override FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo CreateActor(ActorDemoActorService service, ActorId actorId)
				{
					return new FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo(service, actorId);
				}
			}

			public class _with_multiple_activated_actors_with_multiple_states : TestBase<FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates.ActorDemo>
			{
				private IList<long> _partitionKeys;

				protected override async Task SetupActor()
				{
					for (int j = 0; j < 100; j++)
					{
						var actorProxy = FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus-{j}"));
						var i = await actorProxy.GetCountAsync();						
					}

					await base.SetupActor();

					var serviceName = FabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

					var partitions = new List<Guid>();
					_partitionKeys = new List<long>();
					for (int j = 0; j < 100; j++)
					{
						var partitionKey = new ActorId($"testivus-{j}").GetPartitionKey();
						var partition = await FabricRuntime.PartitionEnumerationManager.GetPartition(serviceName, partitionKey);

						if (!partitions.Contains(partition.Id))
						{
							partitions.Add(partition.Id);
							_partitionKeys.Add(partitionKey);
						}
					}
				}

				[Test]
				public async Task _should_store_state_for_all_actors()
				{
					for (int j = 0; j < 100; j++)
					{
						var actorIdSchemaKey = StateSessionHelper.GetActorIdSchemaKey(new ActorId($"testivus-{j}"));
						var actorIdKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorIdStateSchemaName) && k.Contains(actorIdSchemaKey));
						var actorIdState = GetState<ActorStateWrapper<string>>(actorIdKey);
						actorIdState.Schema.Should().Be(StateSessionHelper.ActorIdStateSchemaName);
						actorIdState.Key.Should().Be(actorIdSchemaKey);
						actorIdState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
						actorIdState.ActorId.Kind.Should().Be(ActorIdKind.String);
						actorIdState.ActorId.GetStringId().Should().Be($"testivus-{j}");
						actorIdState.State.Should().Be(actorIdSchemaKey);


						CheckActorState<int>($"testivus-{j}", "state1", (value) => value.Should().Be(1));
						CheckActorState<string>($"testivus-{j}", "state2", v => v.Should().Be($"the value for testivus-{j}"));
						CheckActorState<ComplexState>($"testivus-{j}", "state3", v =>
						{
							v.Value.Should().Be($"Complex state for testivus-{j}");
							(DateTime.Now - v.Time).TotalMinutes.Should().BeLessThan(30);
						});
						CheckActorState<int>($"testivus-{j}", "state4", v => v.Should().BeInRange(0, 100));
					}

					State.Should().HaveCount(500);
				}

				[Test]
				public async Task _should_enumerate_all_actors()
				{
					var serviceName = FabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();					

					var actors = new List<string>();
					foreach (var partitionKey in _partitionKeys)
					{
						var actorDemoActorService = FabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
							serviceName, partitionKey);

						var actorsAsync = await actorDemoActorService.GetActorsAsync(CancellationToken.None);
						actors.AddRange(actorsAsync);
					}

					actors.Should().HaveCount(100);
				}

				[Test]
				public async Task _should_enumerate_all_state_for_all_actors()
				{
					var serviceName = FabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

					var actors = new List<string>();
					foreach (var partitionKey in _partitionKeys)
					{
						var actorDemoActorService = FabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
							serviceName, partitionKey);

						var actorsAsync = await actorDemoActorService.GetActorsAsync(CancellationToken.None);

						foreach (var actor in actors)
						{
							var actorStates = await actorDemoActorService.GetStoredStates(new ActorId(actor), CancellationToken.None);

							actorStates.Should().BeEquivalentTo(new[] {"state1", "state2", "state3", "state4"});
						}
					}
				}

				private void CheckActorState<T>(string actorName, string stateName, Action<T> checkValue)
				{
					var actorIdSchemaKey = StateSessionHelper.GetActorIdSchemaKey(new ActorId(actorName));

					var actorStateKey = State.Keys.Single(k => k.Contains(StateSessionHelper.ActorStateSchemaName) && k.Contains(actorIdSchemaKey) && k.Contains(stateName));
					var actorState = GetState<ActorStateWrapper<T>>(actorStateKey);
					actorState.Schema.Should().StartWith(StateSessionHelper.ActorStateSchemaName);
					actorState.ServiceTypeName.Should().Be("Overlord-ActorDemoActorService");
					actorState.ActorId.Kind.Should().Be(ActorIdKind.String);
					actorState.ActorId.GetStringId().Should().Be(actorName);
					checkValue(actorState.State);
				}

				protected override FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates.ActorDemo CreateActor(ActorDemoActorService service, ActorId actorId)
				{
					return new FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates.ActorDemo(service, actorId);
				}
			}

			public class _with_actors_with_reminders : TestBase<FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithReminders.ActorDemo>
			{
				private IList<long> _partitionKeys;

				protected override async Task SetupActor()
				{
					for (int j = 0; j < 10; j++)
					{
						var actorProxy = FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus-{j}"));
						var i = await actorProxy.GetCountAsync();
					}

					await base.SetupActor();

					var serviceName = FabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

					var partitions = new List<Guid>();
					_partitionKeys = new List<long>();
					for (int j = 0; j < 100; j++)
					{
						var partitionKey = new ActorId($"testivus-{j}").GetPartitionKey();
						var partition = await FabricRuntime.PartitionEnumerationManager.GetPartition(serviceName, partitionKey);

						if (!partitions.Contains(partition.Id))
						{
							partitions.Add(partition.Id);
							_partitionKeys.Add(partitionKey);
						}
					}
				}

				[Test]
				public async Task _should_store_reminder_state()
				{					
					State.Should().HaveCount(30);

					State.Where(i => i.Key.Contains(StateSessionHelper.ActorIdStateSchemaName)).Should().HaveCount(10);
					State.Where(i => i.Key.Contains(StateSessionHelper.ActorReminderSchemaName)).Should().HaveCount(10);
					State.Where(i => i.Key.Contains(StateSessionHelper.ActorStateSchemaName)).Should().HaveCount(10);
				}
				protected override FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithReminders.ActorDemo CreateActor(ActorDemoActorService service, ActorId actorId)
				{
					return new FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithReminders.ActorDemo(service, actorId);
				}
			}

		}

	}
}