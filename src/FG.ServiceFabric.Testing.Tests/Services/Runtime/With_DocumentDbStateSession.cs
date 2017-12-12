﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using FluentAssertions;
using Microsoft.ServiceFabric.Services.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime
{

	// ReSharper disable InconsistentNaming
	namespace With_StateSessionManager
	{
		namespace and_DocumentDbStateSessionManagerWithTransaction
		{
			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public abstract class TestBaseForDocumentDbStateSessionManager<TService> : TestBase<TService>
				where TService : StatefulServiceDemoBase
			{
				private DocumentDbStateSessionManagerWithTransactions _stateSessionManager;
				private CosmosDbForTestingSettingsProvider _cosmosDbSettingsProvider;
				private string _collectionName;
				private Guid _appId = Guid.NewGuid();

				public override IDictionary<string, string> State => GetState();

				public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime,
					StatefulServiceContext context)
				{
					_collectionName = $"App-tests-{_appId}";

					_cosmosDbSettingsProvider = CosmosDbForTestingSettingsProvider.DefaultForCollection(_collectionName);
					_stateSessionManager = new DocumentDbStateSessionManagerWithTransactions(
						"StatefulServiceDemo",
						_appId,
						StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager).GetAwaiter()
							.GetResult(),
						_cosmosDbSettingsProvider
					);

					return _stateSessionManager;
				}

				private IDictionary<string, string> GetState()
				{
					if (_stateSessionManager is IDocumentDbDataManager dataManager)
					{
						return dataManager.GetCollectionDataAsync(_collectionName).GetAwaiter().GetResult();
					}
					return new Dictionary<string, string>();
				}

				protected void DestroyCollection()
				{
					if (_stateSessionManager is IDocumentDbDataManager dataManager)
					{
						dataManager.DestroyCollecton(_collectionName).GetAwaiter().GetResult();
					}
				}

				protected override void OnTearDown()
				{
					base.OnTearDown();

					DestroyCollection();
				}
			}

			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public class StateSession_transacted_scope
			{
				private string _sessionId;
				private CosmosDbForTestingSettingsProvider _settingsProvider;
				private DocumentDbStateSessionManagerWithTransactions _stateSessionManager;

				[SetUp]
				public void Setup()
				{
					_sessionId = Guid.NewGuid().ToString();
					var settingsProvider = CosmosDbForTestingSettingsProvider.DefaultForCollection(_sessionId);
					_settingsProvider = settingsProvider;
				}

				[TearDown]
				public void Teardown()
				{
					if (_stateSessionManager is IDocumentDbDataManager dataManager)
					{
						dataManager.DestroyCollecton(_settingsProvider.CollectionName).GetAwaiter().GetResult();
					}
				}

				public IStateSessionManager CreateStateManager()
				{
					_stateSessionManager =  new DocumentDbStateSessionManagerWithTransactions(
						"StatefulServiceDemo",
						Guid.NewGuid(),
						"range-0",
						_settingsProvider
					);

					return _stateSessionManager;
				}

				[Test]
				public async Task should_not_be_available_from_another_session()
				{
					var manager = CreateStateManager();

					var tasks = new List<Task>();
					var task = new Task(async () => await SessionAWorker(manager));
					task.Start();
					tasks.Add(task);

					task = new Task(async () => await SessionBWorker(manager));
					task.Start();
					tasks.Add(task);

					await Task.WhenAll(tasks);
				}


				private async Task SessionBWorker(IStateSessionManager manager)
				{
					using (var session2 = manager.Writable.CreateSession())
					{

						await session2.SetValueAsync<string>("values", "b", "Value from session2 schema values key b", null,
						CancellationToken.None);

						var session2ValueAPreCommit = await session2.TryGetValueAsync<string>("values", "a", CancellationToken.None);
						var session2ValueBPreCommit = await session2.TryGetValueAsync<string>("values", "b", CancellationToken.None);

						session2ValueAPreCommit.HasValue.Should().Be(false);

						session2ValueBPreCommit.Value.Should().Be("Value from session2 schema values key b");

						await session2.CommitAsync();
					}
					using (var session2 = manager.CreateSession())
					{
						var session2ValueBPostCommit = await session2.GetValueAsync<string>("values", "b", CancellationToken.None);

						session2ValueBPostCommit.Should().Be("Value from session2 schema values key b");
					}
				}



				private async Task SessionAWorker(IStateSessionManager manager)
				{
					using (var session1 = manager.Writable.CreateSession())
					{

						await session1.SetValueAsync<string>("values", "a", "Value from session1 schema values key a", null,
							CancellationToken.None);

						var session1ValueBPreCommit = await session1.TryGetValueAsync<string>("values", "b", CancellationToken.None);
						var session1ValueAPreCommit = await session1.TryGetValueAsync<string>("values", "a", CancellationToken.None);

						session1ValueBPreCommit.HasValue.Should().Be(false);

						session1ValueAPreCommit.Value.Should().Be("Value from session1 schema values key a");

						await session1.CommitAsync();
					}
					using (var session1 = manager.Writable.CreateSession())
					{
						var session1ValueAPostCommit = await session1.GetValueAsync<string>("values", "a", CancellationToken.None);

						session1ValueAPostCommit.Should().Be("Value from session1 schema values key a");
					}
				}


				[Test]
				public async Task should_not_be_included_in_FindBykey()
				{
					var manager = CreateStateManager();

					var session1 = manager.Writable.CreateSession();

					await session1.SetValueAsync<string>("values", "a", "Value from session1 schema values key a", null,
						CancellationToken.None);
					await session1.SetValueAsync<string>("values", "b", "Value from session1 schema values key b", null,
						CancellationToken.None);
					await session1.SetValueAsync<string>("values", "c", "Value from session1 schema values key c", null,
						CancellationToken.None);
					await session1.SetValueAsync<string>("values", "d", "Value from session1 schema values key d", null,
						CancellationToken.None);

					await session1.CommitAsync();

					var committedResults =
						await session1.FindByKeyPrefixAsync<string>("values", null, 10000, null, CancellationToken.None);

					committedResults.Items.ShouldBeEquivalentTo(new[] {"a", "b", "c", "d"});

					await session1.SetValueAsync<string>("values", "e", "Value from session1 schema values key e", null,
						CancellationToken.None);
					await session1.SetValueAsync<string>("values", "f", "Value from session1 schema values key f", null,
						CancellationToken.None);
					await session1.SetValueAsync<string>("values", "g", "Value from session1 schema values key g", null,
						CancellationToken.None);
					await session1.RemoveAsync<string>("values", "a", CancellationToken.None);
					await session1.RemoveAsync<string>("values", "b", CancellationToken.None);
					await session1.RemoveAsync<string>("values", "c", CancellationToken.None);
					await session1.RemoveAsync<string>("values", "d", CancellationToken.None);

					var uncommittedResults =
						await session1.FindByKeyPrefixAsync<string>("values", null, 10000, null, CancellationToken.None);
					uncommittedResults.Items.ShouldBeEquivalentTo(new[] {"a", "b", "c", "d"});

					committedResults.ShouldBeEquivalentTo(uncommittedResults);
				}

				[Test]
				public async Task should_not_be_included_in_enumerateSchemaNames()
				{
					var manager = CreateStateManager();

					var session1 = manager.Writable.CreateSession();

					var schemas = new[] {"a-series", "b-series", "c-series"};
					foreach (var schema in schemas)
					{
						await session1.SetValueAsync<string>(schema, "a", $"Value from session1 schema {schema} key a", null,
							CancellationToken.None);
						await session1.SetValueAsync<string>(schema, "b", $"Value from session1 schema {schema} key b", null,
							CancellationToken.None);
						await session1.SetValueAsync<string>(schema, "c", $"Value from session1 schema {schema} key c", null,
							CancellationToken.None);
						await session1.SetValueAsync<string>(schema, "d", $"Value from session1 schema {schema} key d", null,
							CancellationToken.None);
					}

					var schemaKeysPreCommit = await session1.EnumerateSchemaNamesAsync("a", CancellationToken.None);

					schemaKeysPreCommit.Should().HaveCount(0);

					await session1.CommitAsync();

					var schemaKeysPostCommit = await session1.EnumerateSchemaNamesAsync("a", CancellationToken.None);

					schemaKeysPostCommit.ShouldBeEquivalentTo(schemas);
				}
			}

			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public class Service_with_simple_counter_state : TestBaseForDocumentDbStateSessionManager<
				FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo>
			{
				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo
					CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service = new FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo(context, stateSessionManager);
					return service;
				}

				private async Task RunWork()
				{
					foreach (var partitionKey in new[] { long.MinValue, long.MaxValue - 10 })
					{
						var statefulServiceDemo = _fabricApplication.FabricRuntime.ServiceProxyFactory
							.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.IStatefulServiceDemo>(
								new Uri("fabric:/overlord/StatefulServiceDemo"), new ServicePartitionKey(partitionKey));

						await statefulServiceDemo.RunWork();
					}
				}

				[Test]
				public async Task _should_persist_state_stored()
				{
					await RunWork();
					State.Should().HaveCount(2);
				}
			}

			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public class Service_with_multiple_states : TestBaseForDocumentDbStateSessionManager<
				FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo>
			{
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo
					CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service =
						new FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo(context,
							stateSessionManager);
					return service;
				}

				private async Task RunWork()
				{
					foreach (var partitionKey in new[] { long.MinValue, long.MaxValue - 10 })
					{
						var statefulServiceDemo = _fabricApplication.FabricRuntime.ServiceProxyFactory
							.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.IStatefulServiceDemo>(
								new Uri("fabric:/overlord/StatefulServiceDemo"), new ServicePartitionKey(partitionKey));

						await statefulServiceDemo.RunWork();
					}
				}

				[Test]
				public async Task _should_persist_state_stored()
				{
					await RunWork();
					State.Should().HaveCount(4);
				}
			}


			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public class Service_with_polymorphic_states : TestBaseForDocumentDbStateSessionManager<
				FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo>
			{
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo
					CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service =
						new FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo(context,
							stateSessionManager);
					return service;
				}

				private async Task RunWork()
				{
					foreach (var partitionKey in new[] { long.MinValue, long.MaxValue - 10 })
					{
						var statefulServiceDemo = _fabricApplication.FabricRuntime.ServiceProxyFactory
							.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.IStatefulServiceDemo>(
								new Uri("fabric:/overlord/StatefulServiceDemo"), new ServicePartitionKey(partitionKey));

						await statefulServiceDemo.RunWork();
					}
				}

				[Test]
				public async Task _should_persist_state_stored()
				{
					await RunWork();
					State.Should().HaveCount(2);
				}

				[Test]
				public async Task _should_be_able_to_read_persisted_state()
				{
					await RunWork();
					foreach (var service in this.Services)
					{
						var serviceState = await service.GetStateAsync(CancellationToken.None);
						serviceState.Items.Should().HaveCount(2);
						var innerStateItemA =
							(FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeA)
							serviceState.Items.Single(i =>
								i is FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeA);
						var innerStateItemB =
							(FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeB)
							serviceState.Items.Single(i =>
								i is FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeB);

						innerStateItemA.Name.Should().Be("I am a");
						innerStateItemA.PropertyInA.Should().Be("Prop in A");
						innerStateItemB.Name.Should().Be("I am b");
						innerStateItemB.PropertyInB.Should().Be("Prop in B");
					}
				}
			}

			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public class Service_with_simple_dictionary : TestBaseForDocumentDbStateSessionManager<
				FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo>
			{
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo
					CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service =
						new FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo(context,
							stateSessionManager);
					return service;
				}

				[Test]
				public async Task _should_persist_added_item()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Add("a", "A");

					// items and queue info state
					State.Should().HaveCount(1);
				}

				[Test]
				public async Task _should_persist_added_items()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Add("a", "A");
					await statefulServiceDemo.Add("b", "B");
					await statefulServiceDemo.Add("c", "C");
					await statefulServiceDemo.Add("d", "D");
					await statefulServiceDemo.Add("e", "E");

					// items and queue info state
					State.Should().HaveCount(5);
				}

				[Test]
				public async Task _should_persist_added_item_and_enumerate_all()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Add("a", "A");

					// items and queue info state
					var keyValuePairs = await statefulServiceDemo.EnumerateAll();
					keyValuePairs.Should().BeEquivalentTo(new KeyValuePair<string, string>[]
						{new KeyValuePair<string, string>("a", "A")});
				}

				[Test]
				public async Task _should_persist_added_items_and_enumerate_all()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Add("a", "A");
					await statefulServiceDemo.Add("b", "B");
					await statefulServiceDemo.Add("c", "C");
					await statefulServiceDemo.Add("d", "D");
					await statefulServiceDemo.Add("e", "E");

					// items and queue info state
					var keyValuePairs = await statefulServiceDemo.EnumerateAll();
					keyValuePairs.Should().BeEquivalentTo(new KeyValuePair<string, string>[]
					{
						new KeyValuePair<string, string>("a", "A"),
						new KeyValuePair<string, string>("b", "B"),
						new KeyValuePair<string, string>("c", "C"),
						new KeyValuePair<string, string>("d", "D"),
						new KeyValuePair<string, string>("e", "E"),
					});
				}

				[Test]
				public async Task _should_persist_added_and_removed_items_and_enumerate_all()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Add("a", "A");
					await statefulServiceDemo.Add("b", "B");
					await statefulServiceDemo.Add("c", "C");
					await statefulServiceDemo.Add("d", "D");
					await statefulServiceDemo.Add("e", "E");

					await statefulServiceDemo.Remove("b");
					await statefulServiceDemo.Remove("d");

					await statefulServiceDemo.Add("f", "F");
					await statefulServiceDemo.Add("g", "G");

					// items and queue info state
					var keyValuePairs = await statefulServiceDemo.EnumerateAll();
					keyValuePairs.Should().BeEquivalentTo(new KeyValuePair<string, string>[]
					{
						new KeyValuePair<string, string>("a", "A"),
						new KeyValuePair<string, string>("c", "C"),
						new KeyValuePair<string, string>("e", "E"),
						new KeyValuePair<string, string>("f", "F"),
						new KeyValuePair<string, string>("g", "G"),
					});
				}
			}

			//[Ignore("Only run these tests with a live Cosmos Db or a Cosmos Db emulator, change the settings to connect to the instance")]
			public class Service_with_simple_queue_enqueued : TestBaseForDocumentDbStateSessionManager<
				FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo>
			{
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo
					CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service =
						new FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo(context,
							stateSessionManager);
					return service;
				}

				[Test]
				public async Task _should_persist_state_stored_after_enqueued()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// items and queue info state
					State.Should().HaveCount(6);

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_QUEUEINFO")).Should().HaveCount(1);
					for (int i = 0; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_QUEUE-{i}")).Should()
							.HaveCount(1, $"Expected _myQueue_{i} to be there");
					}
				}

				[Test]
				public async Task _should_persist_state_stored_after_dequeued()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(3);
					longs.Should().BeEquivalentTo(new long[] { 1, 2, 3 });

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_QUEUEINFO")).Should().HaveCount(1);
					for (int i = 3; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_QUEUE-{i}")).Should()
							.HaveCount(1, $"Expected _myQueue_QUEUE{i} to be there");

						var key = State.Single(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_QUEUE-{i}")).Key;
						var state = GetState<StateWrapper<long>>(key);
						state.State.Should().Be(i + 1);
					}
				}

				[Test]
				public async Task _should_clear_states_when_queue_is_empty()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					State.Should().HaveCount(1);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_QUEUEINFO")).Should().HaveCount(1);

					// Peek should show empty
					var hasMore = await statefulServiceDemo.Peek();
					hasMore.Should().Be(false);
				}

				[Test]
				public async Task _should_persist_enqueued_state_after_dequeuing()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(3);

					State.Should().HaveCount(4);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_QUEUEINFO")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}

				[Test]
				public async Task _should_be_able_to_enqueue_new_items_when_queue_info_is_loaded_from_prior_state()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					State.Add("Overlord-StatefulServiceDemo_range-0_myQueue_QUEUEINFO", @"{
						  ""state"": {
							""HeadKey"": 4,
							""TailKey"": 5
						  },
						  ""serviceTypeName"": ""Overlord-StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueuItem"",
						  ""id"": ""Overlord-StatefulServiceDemo_range-0_myQueue_QUEUEINFO""
						}");

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(3);

					State.Should().HaveCount(4);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_QUEUEINFO")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}

				[Test]
				public async Task _should_be_able_to_store_beyond_int_maxvalue()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					State.Add("Overlord-StatefulServiceDemo_range-0_myQueue_QUEUEINFO", @"{
						  ""state"": {
							""HeadKey"": %%HEAD%%,
							""TailKey"": %%TAIL%%
						  },
						  ""serviceTypeName"": ""Overlord-StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueuItem"",
						  ""id"": ""Overlord-StatefulServiceDemo_range-0_myQueue_QUEUEINFO""
						}".Replace("%%HEAD%%", (long.MaxValue - 1).ToString()).Replace("%%TAIL%%", (long.MaxValue).ToString()));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(3);

					State.Should().HaveCount(4);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_QUEUEINFO")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}

				[Test]
				public async Task _should_return_queue_length_0_for_initial_queue()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					var length = await statefulServiceDemo.GetQueueLength();
					length.Should().Be(0);
				}

				[Test]
				public async Task _should_return_queue_length_0_for_emptied_queue()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));


					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Empty
					while (await statefulServiceDemo.Peek())
					{
						await statefulServiceDemo.Dequeue(1);
					}

					var length = await statefulServiceDemo.GetQueueLength();
					length.Should().Be(0);
				}

				[Test]
				public async Task _should_return_queue_length_0_for_queue_with_multiple_enqueues_and_dequeues_until_drained()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));


					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Empty
					while (await statefulServiceDemo.Peek())
					{
						await statefulServiceDemo.Dequeue(1);
					}

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(50);

					await statefulServiceDemo.Dequeue(20);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(30);

					// Empty
					while (await statefulServiceDemo.Peek())
					{
						await statefulServiceDemo.Dequeue(1);
					}


					var length = await statefulServiceDemo.GetQueueLength();
					length.Should().Be(0);
				}


				[Test]
				public async Task _should_return_queue_length_for_enqueued_items()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					var length = await statefulServiceDemo.GetQueueLength();
					length.Should().Be(5);
				}


				[Test]
				public async Task _should_return_queue_length_for_reamaining_enqueued_items_with_multiple_enqueues_and_dequeues()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3);


					var length = await statefulServiceDemo.GetQueueLength();
					length.Should().Be(6);
				}

				[Test]
				public async Task _should_be_able_to_enumerate_all_enqueued_items()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Enumerate all
					var all = await statefulServiceDemo.EnumerateAll();

					all.Should().Equal(new long[] { 1, 2, 3, 4, 5 });
				}

				[Test]
				public async Task _should_not_enumerate_anything_after_enqueued_items_have_been_dequeued()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(5);

					// Enumerate all
					var all = await statefulServiceDemo.EnumerateAll();

					all.Should().Equal(new long[] { });
				}

				[Test]
				public async Task _should_be_able_to_enumerate_all_enqueued_items_after_last_items_have_been_dequeued()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));


					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // 1 2 3 4 5

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3); // _ _ _ 4 5				

					// Enumerate all
					var all = await statefulServiceDemo.EnumerateAll();

					all.Should().Equal(new long[] { 4, 5 });
				}

				[Test]
				public async Task _should_be_able_to_enumerate_all_enqueued_items_after_multiple_enqueues_and_dequeues()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));


					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // 1 2 3 4 5
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] { 1, 2, 3, 4, 5 });

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3); // _ _ _ 4 5
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] { 4, 5 });

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // _ _ _ 4 5 6 7 8 9 10
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] { 4, 5, 6, 7, 8, 9, 10 });

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(2); // _ _ _ _ _ 6 7 8 9 10
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] { 6, 7, 8, 9, 10 });

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // _ _ _ _ _ 6 7 8 9 10 11 12 13 14 15
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] { 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3); // _ _ _ _ _ _ _ _ 9 10 11 12 13 14 15
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] { 9, 10, 11, 12, 13, 14, 15 });

					// Enumerate all
					var all = await statefulServiceDemo.EnumerateAll();

					all.Should().Equal(new long[] { 9, 10, 11, 12, 13, 14, 15 });
				}
			}
		}
	}
	// ReSharper restore InconsistentNaming
}