using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
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
			public abstract class TestBase<T>
				where T : StatefulServiceDemoBase
			{
				protected MockFabricRuntime FabricRuntime;

				protected readonly IDictionary<string, string> State = new ConcurrentDictionary<string, string>();
				private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();


				protected TestBase()
				{
				}

				[SetUp]
				public void Setup()
				{
					State.Clear();
					FabricRuntime = new MockFabricRuntime("Overlord") {DisableMethodCallOutput = true};
					FabricRuntime.SetupService(
						(context, stateManager) => CreateAndMonitorService(context, CreateStateManager(context)),
						serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(2, long.MinValue, long.MaxValue));

					SetupService().GetAwaiter().GetResult();

					var instances = FabricRuntime.GetInstances().Count();
					var i = 0;
					do
					{
						if (i > 50)
						{
							Assert.Fail(@"Should have run the loop and updated the states by now");
						}
						Task.Delay(TimeSpan.FromMilliseconds(100)).GetAwaiter().GetResult();						
						i++;
					} while (_runAsyncLoopUpdates.Count < instances);
				}

				private T CreateAndMonitorService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
				{
					var service = CreateService(context, stateSessionManager);

					service.RunAsyncLoop += ServiceOnRunAsyncLoop;
					
					return service;
				}

				private void ServiceOnRunAsyncLoop(object sender, RunAsyncLoopEventArgs e)
				{
					var partitionKey = StateSessionHelper.GetPartitionInfo(e.Context, () => FabricRuntime.PartitionEnumerationManager).GetAwaiter().GetResult();
					_runAsyncLoopUpdates[partitionKey] = e.Iteration;
				}

				protected abstract T CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager);

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

				protected virtual Task SetupService()
				{
					return Task.FromResult(true);
				}

				protected virtual Task SetUpStates(InMemoryStateSessionManager stateSessionManager)
				{
					return Task.FromResult(true);
				}

				protected T GetState<T>(string key)
				{
					return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(State[key]);
				}

				private IStateSessionManager CreateStateManager(StatefulServiceContext context)
				{
					var stateManager = new InMemoryStateSessionManager(
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

			public class Service_with_simple_counter_state : TestBase<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo>
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

			public class Service_with_multiple_states : TestBase<FG.ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo>
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

			public class Service_with_simple_queue_enqueued : TestBase<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo>
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
				public async Task _should_be_able_to_enqueue_new_items_when_queue_info_is_loaded_from_prior_state()
				{
					var statefulServiceDemo = FabricRuntime.ServiceProxyFactory.CreateServiceProxy<FG.ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.IStatefulServiceDemo>(
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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
						FabricRuntime.ApplicationUriBuilder.Build("StatefulServiceDemo"), new ServicePartitionKey(int.MinValue));

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