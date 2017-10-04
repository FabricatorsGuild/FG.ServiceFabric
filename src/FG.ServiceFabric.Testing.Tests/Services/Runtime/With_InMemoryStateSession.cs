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
	namespace With_StateSessionManager
	{
		namespace and_InMemoryStateSession
		{
			public abstract class TestBaseInMemoryStateSessionManager<TService> : TestBase<TService> where TService : StatefulServiceDemoBase
			{
				protected readonly IDictionary<string, string> _state = new ConcurrentDictionary<string, string>();

				protected override void OnSetup()
				{
					State.Clear();
					base.OnSetup();
				}				

				public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime, StatefulServiceContext context)
				{
					return new InMemoryStateSessionManager(
						context.ServiceName.ToString(), 
						context.PartitionId, 
						StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager).GetAwaiter().GetResult(), 
						_state);
				}

				public override IDictionary<string, string> State => _state;
			}

			public class Service_with_simple_counter_state : TestBaseInMemoryStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo>
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

			public class Service_with_multiple_states : TestBaseInMemoryStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo>
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


			public class Service_with_polymorphic_states : TestBaseInMemoryStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo>
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

			public class Service_with_simple_queue_enqueued : TestBaseInMemoryStateSessionManager<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo>
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
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

					// Enqueue 5 items
					await statefulServiceDemo.Enqueue(5);

					// Dequeue 3 items
					var longs = await statefulServiceDemo.Dequeue(3);
					longs.Should().BeEquivalentTo(new long[] {1, 2, 3});

					State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("_myQueue_queue-info")).Should().HaveCount(1);
					for (int i = 3; i < 5; i++)
					{
						State.Where(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Should().HaveCount(1, $"Expected _myQueue_{i} to be there");

						var key = State.Single(s => s.Key.Contains("range-0") && s.Key.Contains($"_myQueue_{i}")).Key;
						var state = GetState<StateWrapper<long>>(key);
						state.State.Should().Be(i+1);
					}
				}

				[Test]
				public async Task _should_clear_states_when_queue_is_empty()
				{
					State.Should().HaveCount(0);

					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
			}
		}
	}
}