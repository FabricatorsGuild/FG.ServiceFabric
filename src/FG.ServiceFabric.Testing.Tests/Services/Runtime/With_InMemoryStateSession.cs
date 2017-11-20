using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using FluentAssertions;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime
{
	// ReSharper disable InconsistentNaming
	namespace With_StateSessionManager
	{
		namespace and_InMemoryStateSession
		{
			public abstract class TestBaseInMemoryStateSessionManager<TService> : TestBase<TService>
				where TService : StatefulServiceDemoBase
			{
				protected readonly IDictionary<string, string> _state = new ConcurrentDictionary<string, string>();

				public override IDictionary<string, string> State => _state;

				protected override void OnSetup()
				{
					State.Clear();
					base.OnSetup();
				}

				public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime,
					StatefulServiceContext context)
				{
					return new InMemoryStateSessionManager(
						context.ServiceName.ToString(),
						context.PartitionId,
						StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager).GetAwaiter()
							.GetResult(),
						_state);
				}
			}

			public class Service_with_simple_counter_state : TestBaseInMemoryStateSessionManager<
				FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo>
			{
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

				protected override FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo
					CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service =
						new FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo(context,
							stateSessionManager);
					return service;
				}

				[Test]
				public void _should_persist_state_stored()
				{
					State.Should().HaveCount(2);
				}
			}

			public class Service_with_multiple_states : TestBaseInMemoryStateSessionManager<
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

				[Test]
				public void _should_persist_state_stored()
				{
					State.Should().HaveCount(4);
				}
			}


			public class Service_with_polymorphic_states : TestBaseInMemoryStateSessionManager<
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

				[Test]
				public void _should_persist_state_stored()
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


			public class Service_with_simple_dictionary : TestBaseInMemoryStateSessionManager<
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

			public class Service_with_simple_queue_enqueued : TestBaseInMemoryStateSessionManager<
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

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);
					for (int i = 0; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Should()
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
					longs.Should().BeEquivalentTo(new long[] {1, 2, 3});

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);
					for (int i = 3; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Should()
							.HaveCount(1, $"Expected _myQueue_{i} to be there");

						var key = State.Single(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Key;
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
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);

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
					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);

					var item = await statefulServiceDemo.Dequeue(1);
					item.Single().Should().Be(6L);
				}

				[Test]
				public async Task _check_stored_string()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					await statefulServiceDemo.Enqueue(1);
					await statefulServiceDemo.Dequeue(1);

					State.Single().Key.Should().Be(@"fabric:/Overlord/StatefulServiceDemo_range-0_myQueue_queue-info");
					State.Single().Value.TrimInternalWhitespace(true).Replace("\r\n", "").Should().Be(@"{
						  ""state"": {
							""HeadKey"": 0,
							""TailKey"": 1
						  },
						  ""serviceTypeName"": ""fabric:/Overlord/StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueueItem"",
						  ""id"": ""fabric:/Overlord/StatefulServiceDemo_range-0_myQueue_queue-info""
						}".TrimInternalWhitespace(true));
				}

				[Test]
				public async Task _should_be_able_to_enqueue_new_items_when_queue_info_is_loaded_from_prior_state()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					State.Add("fabric:/Overlord/StatefulServiceDemo_range-0_myQueue_queue-info", @"{
						  ""state"": {
							""HeadKey"": 4,
							""TailKey"": 5
						  },
						  ""serviceTypeName"": ""fabric:/Overlord/StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueueItem"",
						  ""id"": ""fabric:/Overlord/StatefulServiceDemo_range-0_myQueue_queue-info""
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
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					State.Add("fabric:/Overlord/StatefulServiceDemo_range-0_myQueue_queue-info", @"{
						  ""state"": {
							""HeadKey"": %%HEAD%%,
							""TailKey"": %%TAIL%%
						  },
						  ""serviceTypeName"": ""fabric:/Overlord/StatefulServiceDemo"",
						  ""partitionKey"": ""range-0"",
						  ""schema"": ""myQueue"",
						  ""key"": ""queue-info"",
						  ""type"": ""ReliableQueueItem"",
						  ""id"": ""fabric:/Overlord/StatefulServiceDemo_range-0_myQueue_queue-info""
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

					all.Should().Equal(new long[] {1, 2, 3, 4, 5});
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

					all.Should().Equal(new long[] {4, 5});
				}

				[Test]
				public async Task _should_be_able_to_enumerate_all_enqueued_items_after_multiple_enqueues_and_dequeues()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
						.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
							_fabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));


					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // 1 2 3 4 5
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] {1, 2, 3, 4, 5});

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3); // _ _ _ 4 5
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] {4, 5});

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // _ _ _ 4 5 6 7 8 9 10
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] {4, 5, 6, 7, 8, 9, 10});

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(2); // _ _ _ _ _ 6 7 8 9 10
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] {6, 7, 8, 9, 10});

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5); // _ _ _ _ _ 6 7 8 9 10 11 12 13 14 15
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] {6, 7, 8, 9, 10, 11, 12, 13, 14, 15});

					// Enqueue 5 items
					await statefulServiceDemo.Dequeue(3); // _ _ _ _ _ _ _ _ 9 10 11 12 13 14 15
					(await statefulServiceDemo.EnumerateAll()).Should().Equal(new long[] {9, 10, 11, 12, 13, 14, 15});

					// Enumerate all
					var all = await statefulServiceDemo.EnumerateAll();

					all.Should().Equal(new long[] {9, 10, 11, 12, 13, 14, 15});
				}
			}
		}
	}
	// ReSharper restore InconsistentNaming
}