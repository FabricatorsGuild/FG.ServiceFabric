using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime
{
	namespace With_StateSessionManager
	{
	}


	namespace With_StateSessionManager
	{
		namespace and_FileSystemStateSessionManagerWithTransaction
		{
			public abstract class TestBaseFileSystemStateSessionManager<TService> : TestBase<TService> where TService : StatefulServiceDemoBase
			{
				private string _path;

				protected override void OnSetup()
				{
					_path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{this.GetType().Assembly.GetName().Name}-{Guid.NewGuid().ToString()}");
					System.IO.Directory.CreateDirectory(_path);
					base.OnSetup();
				}

				protected override void OnTearDown()
				{
					try
					{
						System.IO.Directory.Delete(_path, true);

					}
					catch (IOException ex)
					{
						// Just ignore it, it's a temp file anyway
					}
					base.OnTearDown();
				}

				public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime, StatefulServiceContext context)
				{
					return new FileSystemStateSessionManager(
						context.ServiceName.ToString(),
						context.PartitionId,
						StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager).GetAwaiter().GetResult(),
						_path);
				}

				public override IDictionary<string, string> State => GetState();


				private IDictionary<string, string> GetState()
				{
					var state = new Dictionary<string, string>();
					var files = System.IO.Directory.GetFiles(_path);

					foreach (var file in files)
					{
						var name = System.IO.Path.GetFileNameWithoutExtension(file);
						var content = System.IO.File.ReadAllText(file);

						state.Add(name, content);
					}
					return state;
				}
			}
			public class StateSession_transacted_scope
			{
				private string _path;

				[SetUp]
				public void Setup()
				{
					_path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{this.GetType().Assembly.GetName().Name}-{Guid.NewGuid().ToString()}");
					System.IO.Directory.CreateDirectory(_path);
				}

				[TearDown]
				public void Teardown()
				{
					try
					{
						System.IO.Directory.Delete(_path, true);
					}
					catch (IOException ex)
					{
						// Just ignore it, it's a temp file anyway
					}
				}

				private IDictionary<string, string> GetState()
				{
					var state = new Dictionary<string, string>();
					var files = System.IO.Directory.GetFiles(_path);

					foreach (var file in files)
					{
						var name = System.IO.Path.GetFileNameWithoutExtension(file);
						var content = System.IO.File.ReadAllText(file);

						state.Add(name, content);
					}
					return state;
				}

				[Test]
				public async Task _should_be_able_to_page_findbykey_results()
				{
					var manager = new FileSystemStateSessionManager("testservice", Guid.NewGuid(), "range-0", _path);

					var session1 = manager.CreateSession();

					var keys = new List<string>();
					for (int i = (int)'a'; i <= (int)'z'; i++)
					{
						var key = ((char)i).ToString();
						keys.Add(key);
						await session1.SetValueAsync<string>("values", key, $"Value from session1 schema values key {key}", null, CancellationToken.None);
					}
					await session1.CommitAsync();

					var state = GetState();
					state.Should().HaveCount(keys.Count);

					var results = new List<string>();
					var pages = 0;
					ContinuationToken token = null;
					do
					{
						var foundKeys = await session1.FindByKeyPrefixAsync("values", null, 10, token, CancellationToken.None);
						token = foundKeys.ContinuationToken;
						results.AddRange(foundKeys.Items);
						Console.WriteLine($"Found {foundKeys.Items.Count()} {foundKeys.Items.First()}-{foundKeys.Items.Last()} with next token {token?.Marker}");
						pages++;
					} while (token != null);

					results.Should().BeEquivalentTo(keys);
					pages.Should().Be(3);
				}

				[Test]
				public async Task should_not_be_available_from_another_session()
				{
					var manager = new FileSystemStateSessionManager("testservice", Guid.NewGuid(), "range-0", _path);

					var session1 = manager.CreateSession();
					var session2 = manager.CreateSession();

					await session1.SetValueAsync<string>("values", "a", "Value from session1 schema values key a", null, CancellationToken.None);

					await session2.SetValueAsync<string>("values", "b", "Value from session2 schema values key b", null, CancellationToken.None);

					var session1ValueBPreCommit = await session1.TryGetValueAsync<string>("values", "b", CancellationToken.None);
					var session2ValueAPreCommit = await session2.TryGetValueAsync<string>("values", "a", CancellationToken.None);

					var session1ValueAPreCommit = await session1.TryGetValueAsync<string>("values", "a", CancellationToken.None);
					var session2ValueBPreCommit = await session2.TryGetValueAsync<string>("values", "b", CancellationToken.None);

					session1ValueBPreCommit.HasValue.Should().Be(false);
					session2ValueAPreCommit.HasValue.Should().Be(false);

					session1ValueAPreCommit.Value.Should().Be("Value from session1 schema values key a");
					session2ValueBPreCommit.Value.Should().Be("Value from session2 schema values key b");

					await session1.CommitAsync();
					await session2.CommitAsync();

					var session1ValueBPostCommit = await session1.GetValueAsync<string>("values", "b", CancellationToken.None);
					var session2ValueAPostCommit = await session1.GetValueAsync<string>("values", "a", CancellationToken.None);

					session1ValueBPostCommit.Should().Be("Value from session2 schema values key b");
					session2ValueAPostCommit.Should().Be("Value from session1 schema values key a");
				}

				[Test]
				public async Task should_not_be_included_in_FindBykey()
				{
					var manager = new FileSystemStateSessionManager("testservice", Guid.NewGuid(), "range-0", _path);

					var session1 = manager.CreateSession();

					await session1.SetValueAsync<string>("values", "a", "Value from session1 schema values key a", null, CancellationToken.None);
					await session1.SetValueAsync<string>("values", "b", "Value from session1 schema values key b", null, CancellationToken.None);
					await session1.SetValueAsync<string>("values", "c", "Value from session1 schema values key c", null, CancellationToken.None);
					await session1.SetValueAsync<string>("values", "d", "Value from session1 schema values key d", null, CancellationToken.None);

					await session1.CommitAsync();

					var committedResults = await session1.FindByKeyPrefixAsync<string>("values", null, 10000, null, CancellationToken.None);

					committedResults.Items.ShouldBeEquivalentTo(new[] { "a", "b", "c", "d" });

					await session1.SetValueAsync<string>("values", "e", "Value from session1 schema values key e", null, CancellationToken.None);
					await session1.SetValueAsync<string>("values", "f", "Value from session1 schema values key f", null, CancellationToken.None);
					await session1.SetValueAsync<string>("values", "g", "Value from session1 schema values key g", null, CancellationToken.None);
					await session1.RemoveAsync<string>("values", "a", CancellationToken.None);
					await session1.RemoveAsync<string>("values", "b", CancellationToken.None);
					await session1.RemoveAsync<string>("values", "c", CancellationToken.None);
					await session1.RemoveAsync<string>("values", "d", CancellationToken.None);

					var uncommittedResults = await session1.FindByKeyPrefixAsync<string>("values", null, 10000, null, CancellationToken.None);
					uncommittedResults.Items.ShouldBeEquivalentTo(new[] { "a", "b", "c", "d" });

					committedResults.ShouldBeEquivalentTo(uncommittedResults);
				}

				[Test]
				public async Task should_not_be_included_in_enumerateSchemaNames()
				{
					var manager = new FileSystemStateSessionManager("testservice", Guid.NewGuid(), "range-0", _path);

					var session1 = manager.CreateSession();

					var schemas = new[] { "a-series", "b-series", "c-series" };
					foreach (var schema in schemas)
					{
						await session1.SetValueAsync<string>(schema, "a", $"Value from session1 schema {schema} key a", null, CancellationToken.None);
						await session1.SetValueAsync<string>(schema, "b", $"Value from session1 schema {schema} key b", null, CancellationToken.None);
						await session1.SetValueAsync<string>(schema, "c", $"Value from session1 schema {schema} key c", null, CancellationToken.None);
						await session1.SetValueAsync<string>(schema, "d", $"Value from session1 schema {schema} key d", null, CancellationToken.None);

					}

					var schemaKeysPreCommit = await session1.EnumerateSchemaNamesAsync("a", CancellationToken.None);

					schemaKeysPreCommit.Should().HaveCount(0);

					await session1.CommitAsync();

					var schemaKeysPostCommit = await session1.EnumerateSchemaNamesAsync("a", CancellationToken.None);

					schemaKeysPostCommit.ShouldBeEquivalentTo(schemas);

				}
			}

			public class Service_with_simple_counter_state : TestBaseFileSystemStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo>
			{
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service = new FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo(context, stateSessionManager);
					return service;
				}

				[Test]
				public async Task _should_persist_state_stored()
				{
					State.Should().HaveCount(2);
				}

			}

			public class Service_with_multiple_states : TestBaseFileSystemStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo>
			{

				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service = new FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo(context, stateSessionManager);
					return service;
				}

				[Test]
				public async Task _should_persist_state_stored()
				{
					State.Should().HaveCount(4);
				}
			}


			public class Service_with_polymorphic_states : TestBaseFileSystemStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo>
			{

				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service = new FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo(context, stateSessionManager);
					return service;
				}

				[Test]
				public async Task _should_persist_state_stored()
				{
					State.Should().HaveCount(2);
				}

				[Test]
				public async Task _should_be_able_to_read_persisted_state()
				{
					foreach (var service in this.Services)
					{
						var serviceState = await service.GetStateAsync(CancellationToken.None);
						serviceState.Items.Should().HaveCount(2);
						var innerStateItemA = (FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeA)serviceState.Items.Single(i => i is FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeA);
						var innerStateItemB = (FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeB)serviceState.Items.Single(i => i is FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.InnerStateItemTypeB);

						innerStateItemA.Name.Should().Be("I am a");
						innerStateItemA.PropertyInA.Should().Be("Prop in A");
						innerStateItemB.Name.Should().Be("I am b");
						innerStateItemB.PropertyInB.Should().Be("Prop in B");

					}
				}
			}


			public class Service_with_simple_queue_enqueued : TestBaseFileSystemStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo>
			{

				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service = new FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo(context, stateSessionManager);
					return service;
				}

				[Test]
				public async Task _should_persist_state_stored_after_enqueued()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// items and queue info state
					State.Should().HaveCount(6);

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);
					for (int i = 0; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Should().HaveCount(1, $"Expected _myQueue_{i} to be there");
					}
				}

				[Test]
				public async Task _should_persist_state_stored_after_dequeued()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(3);
					longs.Should().BeEquivalentTo(new long[] { 1, 2, 3 });

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);
					for (int i = 3; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Should().HaveCount(1, $"Expected _myQueue_{i} to be there");

						var key = State.Single(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Key;
						var state = GetState<StateWrapper<long>>(key);
						state.State.Should().Be(i + 1);
					}
				}

				[Test]
				public async Task _should_clear_states_when_queue_is_empty()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					State.Should().HaveCount(1);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);

					// Peek should show empty
					var hasMore = await statefulServiceDemo.Peek();
					hasMore.Should().Be(false);

				}

				[Test]
				public async Task _should_persist_enqueued_state_after_dequeuing()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(3);

					State.Should().HaveCount(4);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}

				[Test]
				public async Task _should_be_able_to_enqueue_new_items_when_queue_info_is_loaded_from_prior_state()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					State.Add("Overlord-StatefulServiceDemo_range-0_myQueue_queue-info", @"{
						  ""state"": {
							""HeadKey"": 4,
							""TailKey"": 5
						  },
						  ""serviceTypeName"": ""Overlord-StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueuItem"",
						  ""id"": ""Overlord-StatefulServiceDemo_range-0_myQueue_queue-info""
						}");

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(3);

					State.Should().HaveCount(4);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}

				[Test]
				public async Task _should_be_able_to_store_beyond_int_maxvalue()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					State.Add("Overlord-StatefulServiceDemo_range-0_myQueue_queue-info", @"{
						  ""state"": {
							""HeadKey"": %%HEAD%%,
							""TailKey"": %%TAIL%%
						  },
						  ""serviceTypeName"": ""Overlord-StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueuItem"",
						  ""id"": ""Overlord-StatefulServiceDemo_range-0_myQueue_queue-info""
						}".Replace("%%HEAD%%", (long.MaxValue - 1).ToString()).Replace("%%TAIL%%", (long.MaxValue).ToString()));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(5);

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(3);

					State.Should().HaveCount(4);
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}
			}


		}
	}
}