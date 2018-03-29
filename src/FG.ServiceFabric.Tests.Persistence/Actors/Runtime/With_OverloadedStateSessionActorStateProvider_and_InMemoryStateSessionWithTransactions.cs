using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Actors.Runtime.ActorDocument;
using FG.ServiceFabric.Actors.Runtime.StateSession;
using FG.ServiceFabric.Actors.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.InMemory;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using NUnit.Framework;
using ActorDemo = FG.ServiceFabric.Tests.Actor.WithoutInternalErrors.ActorDemo;

namespace FG.ServiceFabric.Tests.Persistence.Actors.Runtime
{
    namespace With_OverloadedStateSessionActorStateProvider
    {
        namespace _and_InMemoryStateSessionWithTransactions
        {
            public abstract class TestBase<T>
                where T : ActorBase, IActorDemo
            {
                protected readonly IDictionary<string, string> State = new ConcurrentDictionary<string, string>();
                private Guid _appId = Guid.NewGuid();
                protected MockFabricApplication _fabricApplication;
                protected MockFabricRuntime FabricRuntime;

                private string ApplicationName => @"Overlord";

                [SetUp]
                public void Setup()
                {
                    State.Clear();
                    FabricRuntime = new MockFabricRuntime {DisableMethodCallOutput = true};
                    _fabricApplication = FabricRuntime.RegisterApplication(ApplicationName);

                    _fabricApplication.SetupActor(
                        CreateActor,
                        (context, information, provider, factory) =>
                            new ActorDemoActorService(context, information, stateProvider: provider),
                        createActorStateProvider: (context, actorInfo) =>
                            new OverloadedStateSessionActorStateProvider(new MockActorStateProvider(FabricRuntime, null), CreateStateManager(context)), 
                        serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(2, long.MinValue,
                            long.MaxValue));


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

                protected T2 GetState<T2>(string key)
                {
                    return JsonConvert.DeserializeObject<T2>(State[key]);
                }

                private IStateSessionManager CreateStateManager(StatefulServiceContext context)
                {
                    //_collectionName = $"AppTest-{_appId}";
                    //var _cosmosDbSettingsProvider = CosmosDbForTestingSettingsProvider.DefaultForCollection(_collectionName);
                    //var stateManager = new DocumentDbStateSessionManagerWithTransactions(
                    //	"StatefulServiceDemo",
                    //	_appId,
                    //	StateSessionHelper.GetPartitionInfo(context, () => FabricRuntime.PartitionEnumerationManager).GetAwaiter()
                    //		.GetResult(),
                    //	_cosmosDbSettingsProvider
                    //);
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

            public class _without_state : TestBase<ActorDemo>
            {
                protected override ActorDemo CreateActor(
                    ActorDemoActorService service, ActorId actorId)
                {
                    return new ActorDemo(service, actorId);
                }
            }

            public class _with_state : TestBase<ActorDemo>
            {
                protected override async Task SetUpStates(
                    InMemoryStateSessionManagerWithTransaction stateSessionManager)
                {
                    if (stateSessionManager
                        .GetPrivateProperty<InMemoryStateSessionManagerWithTransaction, string>("PartitionKey")
                        .Equals("range-1"))
                        using (var session = stateSessionManager.Writable.CreateSession())
                        {
                            var actorId = new ActorId("testivus");
                            var actorIdSchemaKey = new ActorIdStateKey(actorId);
                            await session.SetValueAsync(actorIdSchemaKey.Schema, actorIdSchemaKey.Key,
                                actorIdSchemaKey.Key,
                                new ActorStateValueMetadata(StateWrapperType.ActorId, actorId));

                            var id = new ActorStateKey(actorId, "count");
                            await session.SetValueAsync(id.Schema, id.Key, 5,
                                new ActorStateValueMetadata(StateWrapperType.ActorState, actorId));

                            await session.CommitAsync();
                        }
                    await base.SetUpStates(stateSessionManager);
                }


                protected override ActorDemo CreateActor(
                    ActorDemoActorService service, ActorId actorId)
                {
                    return new ActorDemo(service, actorId);
                }
            }

            public class _with_multiple_activated_actors : TestBase<ActorDemo>
            {
                [Test]
                public async Task _should_return_prior_state_on_activate()
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var actorProxy =
                            FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus-{j}"));
                        var i = await actorProxy.GetCountAsync();

                        var actorId = new ActorId($"testivus-{j}");
                        var actorIdSchemaKey = new ActorDocumentStateKey(actorId);
                        var actorIdKey = State.Keys.Single(k =>
                            k.Contains(ActorDocumentStateKey.ActorDocumentStateSchemaName) && k.Contains(actorIdSchemaKey.Key));
                        var actorIdState = GetState<ActorStateWrapper<ActorDocumentState>>(actorIdKey);
                        actorIdState.Schema.Should().Be(ActorDocumentStateKey.ActorDocumentStateSchemaName);
                        actorIdState.Key.Should().Be(actorIdSchemaKey.Key);
                        actorIdState.ServiceName.Should().Be("Overlord-ActorDemoActorService");
                        actorIdState.ActorId.Kind.Should().Be(ActorIdKind.String);
                        actorIdState.ActorId.GetStringId().Should().Be($"testivus-{j}");
                    }

                    State.Should().HaveCount(100);
                }

                [Test]
                public async Task _should_be_able_to_delete_created_actors()
                {
                    for (var j = 0; j < 10; j++)
                    {
                        var actorProxy =
                            FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus-{j}"));
                        var i = await actorProxy.GetCountAsync();
                    }

                    //State.Should().HaveCount(20);

                    var serviceUri = _fabricApplication.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();
                    for (var j = 0; j < 10; j++)
                    {
                        var actorProxy =
                            FabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorService>(serviceUri,
                                new ActorId($"testivus-{j}"));
                        await actorProxy.DeleteActorAsync(new ActorId($"testivus-{j}"), CancellationToken.None);
                    }

                    //State.Should().HaveCount(0);
                }


                protected override ActorDemo CreateActor(
                    ActorDemoActorService service, ActorId actorId)
                {
                    return new ActorDemo(service, actorId);
                }
            }

            public class _with_multiple_activated_actors_with_multiple_states : TestBase<
                ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates.ActorDemo>
            {
                private IList<long> _partitionKeys;

                protected override async Task SetupActor()
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var actorProxy =
                            FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus_{j}"));
                        var i = await actorProxy.GetCountAsync();
                    }

                    await base.SetupActor();

                    var serviceName = _fabricApplication.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

                    var partitions = new List<Guid>();
                    _partitionKeys = new List<long>();
                    for (var j = 0; j < 100; j++)
                    {
                        var partitionKey = new ActorId($"testivus_{j}").GetPartitionKey();
                        var partition =
                            await FabricRuntime.PartitionEnumerationManager.GetPartition(serviceName, partitionKey);

                        if (!partitions.Contains(partition.Id))
                        {
                            partitions.Add(partition.Id);
                            _partitionKeys.Add(partitionKey);
                        }
                    }
                }

                [Test]
                public void _should_store_state_for_all_actors()
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var actorId = new ActorId($"testivus_{j}");
                        var expectedActorDocumentStateKey = new ActorDocumentStateKey(actorId);
                        var actorDocumentStateId = State.Keys.SingleOrDefault(k =>
                            k.Contains(ActorDocumentStateKey.ActorDocumentStateSchemaName) && k.Contains(expectedActorDocumentStateKey.Key));
                        if (actorDocumentStateId == null)
                            throw new Exception(
                                $"Tried to find state for {ActorDocumentStateKey.ActorDocumentStateSchemaName} {expectedActorDocumentStateKey.Key} but found no keys matching that");

                        var actorIdState = GetState<ActorStateWrapper<ActorDocumentState>>(actorDocumentStateId);
                        actorIdState.Schema.Should().Be(ActorDocumentStateKey.ActorDocumentStateSchemaName);
                        actorIdState.Key.Should().Be(expectedActorDocumentStateKey.Key);
                        actorIdState.ServiceName.Should().Be("Overlord-ActorDemoActorService");
                        actorIdState.ActorId.Kind.Should().Be(ActorIdKind.String);
                        actorIdState.ActorId.GetStringId().Should().Be($"testivus_{j}");

                        CheckActorState<int>($"testivus_{j}", "state1", value => value.Should().Be(1));
                        CheckActorState<string>($"testivus_{j}", "state2",
                            v => v.Should().Be($"the value for testivus_{j}"));
                        CheckActorState<ComplexState>($"testivus_{j}", "state3", v =>
                        {
                            v.Value.Should().Be($"Complex state for testivus_{j}");
                            (DateTime.Now - v.Time).TotalMinutes.Should().BeLessThan(30);
                        });
                        CheckActorState<int>($"testivus_{j}", "state4", v => v.Should().BeInRange(0, 100));
                    }

                    State.Should().HaveCount(100);
                }

                [Test]
                public async Task _should_enumerate_all_actors()
                {
                    var serviceName = _fabricApplication.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

                    var actors = new List<string>();
                    foreach (var partitionKey in _partitionKeys)
                    {
                        var actorDemoActorService =
                            FabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
                                serviceName, partitionKey);

                        var actorsAsync = await actorDemoActorService.GetActorsAsync(CancellationToken.None);
                        actors.AddRange(actorsAsync);
                    }

                    actors.Should().HaveCount(100);
                }

                [Test]
                public async Task _should_enumerate_all_state_for_all_actors()
                {
                    var serviceName = _fabricApplication.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

                    var actors = new List<string>();
                    foreach (var partitionKey in _partitionKeys)
                    {
                        var actorDemoActorService =
                            FabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
                                serviceName, partitionKey);

                        var actorsAsync = await actorDemoActorService.GetActorsAsync(CancellationToken.None);

                        foreach (var actor in actors)
                        {
                            var actorStates =
                                await actorDemoActorService.GetStoredStates(new ActorId(actor), CancellationToken.None);

                            actorStates.Should().BeEquivalentTo("state1", "state2", "state3", "state4");
                        }
                    }
                }

                private void CheckActorState<T>(string actorName, string stateName, Action<T> checkValue)
                {
                    var actorId = new ActorId(actorName);
                    var actorIdSchemaKey = new ActorDocumentStateKey(actorId);

                    var actorStateId = State.Keys.SingleOrDefault(k =>
                        k.Contains(ActorDocumentStateKey.ActorDocumentStateSchemaName) && k.Contains(actorIdSchemaKey.Key));
                    if (actorStateId == null)
                        throw new Exception(
                            $"Tried to find state for {ActorDocumentStateKey.ActorDocumentStateSchemaName} | {actorIdSchemaKey.Key} | {stateName} but found no keys matching that");

                    var actorDocumentState = GetState<ActorStateWrapper<ActorDocumentState>>(actorStateId);
                    if (!actorDocumentState.State.States.ContainsKey(stateName))
                    throw new Exception(
                            $"Tried to find actor state for {ActorDocumentStateKey.ActorDocumentStateSchemaName} | {actorIdSchemaKey.Key} | {stateName} found the ACTORDOC, but not a state with key matching that");
                    var stateData =
                        JsonConvert.DeserializeObject<T>(
                            JsonConvert.SerializeObject(actorDocumentState.State.States[stateName]));

                    actorDocumentState.Schema.Should().StartWith(ActorDocumentStateKey.ActorDocumentStateSchemaName);
                    actorDocumentState.ServiceName.Should().Be("Overlord-ActorDemoActorService");
                    actorDocumentState.ActorId.Kind.Should().Be(ActorIdKind.String);
                    actorDocumentState.ActorId.GetStringId().Should().Be(actorName);
                    checkValue(stateData);
                }

                protected override ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates.ActorDemo
                    CreateActor(
                        ActorDemoActorService service, ActorId actorId)
                {
                    return new ServiceFabric.Tests.Actor.WithoutInternalErrors.WithMultipleStates.ActorDemo(service,
                        actorId);
                }
            }

            public class
                _with_actors_with_reminders : TestBase<
                    ServiceFabric.Tests.Actor.WithoutInternalErrors.WithReminders.ActorDemo>
            {
                private IList<long> _partitionKeys;

                protected override async Task SetupActor()
                {
                    for (var j = 0; j < 10; j++)
                    {
                        var actorProxy =
                            FabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId($"testivus-{j}"));
                        var i = await actorProxy.GetCountAsync();
                    }

                    await base.SetupActor();

                    var serviceName = _fabricApplication.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri();

                    var partitions = new List<Guid>();
                    _partitionKeys = new List<long>();
                    for (var j = 0; j < 100; j++)
                    {
                        var partitionKey = new ActorId($"testivus-{j}").GetPartitionKey();
                        var partition =
                            await FabricRuntime.PartitionEnumerationManager.GetPartition(serviceName, partitionKey);

                        if (!partitions.Contains(partition.Id))
                        {
                            partitions.Add(partition.Id);
                            _partitionKeys.Add(partitionKey);
                        }
                    }
                }

                [Test]
                public void _should_store_reminder_state()
                {
                    State.Should().HaveCount(10);

                    State.Where(i => i.Key.Contains(ActorDocumentStateKey.ActorDocumentStateSchemaName)).Should().HaveCount(10);
                }

                protected override ServiceFabric.Tests.Actor.WithoutInternalErrors.WithReminders.ActorDemo CreateActor(
                    ActorDemoActorService service, ActorId actorId)
                {
                    return new ServiceFabric.Tests.Actor.WithoutInternalErrors.WithReminders.ActorDemo(service,
                        actorId);
                }
            }
        }
    }
}