using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using FluentAssertions;
using Microsoft.ServiceFabric.Services.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.Persistence.Services.Runtime
{
    public class With_StateSession_All_Tests
    {
        public abstract class TestRunnerWithFunc<T> : TestRunner<T> where T : StatefulServiceDemoBase
        {
            private readonly Func<StatefulServiceContext, IStateSessionManager, T> _createService;

            public TestRunnerWithFunc(Func<StatefulServiceContext, IStateSessionManager, T> createService)
            {
                _createService = createService;
            }

            protected override T CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
            {
                return _createService(context, stateSessionManager);
            }
        }

        public abstract class ServiceTestBase<T> where T : StatefulServiceDemoBase
        {
            private readonly TestRunner<T> _testBase;

            protected ServiceTestBase(TestRunner<T> testBase)
            {
                _testBase = testBase;
            }

            protected IEnumerable<T> Services => _testBase.Services;
            protected IDictionary<string, string> State => _testBase.State;
            protected MockFabricRuntime FabricRuntime => _testBase.FabricRuntime;
            protected MockFabricApplication FabricApplication => _testBase.FabricApplication;

            protected virtual T GetState<T>(string key)
            {
                if (State.ContainsKey(key))
                {
                    var json = State[key];
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                }
                return default(T);
            }

            [SetUp]
            public void Setup()
            {
                _testBase.Setup();
            }

            [TearDown]
            public void TearDown()
            {
                _testBase.TearDown();
            }
        }

        public abstract class Service_with_simple_queue_enqueued : ServiceTestBase<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
            StatefulServiceDemo>
        {
            protected static ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo
                CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
            {
                return new ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo(context, stateSessionManager);
            }

            protected Service_with_simple_queue_enqueued(TestRunner<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.StatefulServiceDemo> testBase) : 
                base(testBase)
            {
            }

            [Test]
            public async Task _should_persist_state_stored_after_enqueued()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // items and queue info state
                State.Should().HaveCount(6);

                State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains("QUEUEINFO")).Should()
                    .HaveCount(1);
                for (var i = 0; i < 5; i++)
                    State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains($"QUEUE-{i}")).Should()
                        .HaveCount(1, $"Expected _myQueue_QUEUE-{i} to be there");
            }

            [Test]
            public async Task _should_persist_state_stored_after_dequeued()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Dequeue 3 items
                var longs = await statefulServiceDemo.Dequeue(3);
                longs.Should().BeEquivalentTo(new long[] {1, 2, 3});

                State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains("QUEUEINFO")).Should()
                    .HaveCount(1);
                for (var i = 3; i < 5; i++)
                {
                    State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains($"QUEUE-{i}")).Should()
                        .HaveCount(1, $"Expected _myQueue_QUEUE-{i} to be there");

                    var key = State.Single(s => s.Key.Contains("range-0") && s.Key.Contains($"myQueue") && s.Key.Contains($"QUEUE-{i}"))
                        .Key;
                    var state = GetState<StateWrapper<long>>(key);
                    state.State.Should().Be(i + 1);
                }
            }

            [Test]
            public async Task _should_clear_states_when_queue_is_empty()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Dequeue 3 items
                var longs = await statefulServiceDemo.Dequeue(5);

                State.Should().HaveCount(1);
                State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains("QUEUEINFO"))
                    .Should()
                    .HaveCount(1);

                // Peek should show empty
                var hasMore = await statefulServiceDemo.Peek();
                hasMore.Should().Be(false);
            }

            [Test]
            public async Task _should_persist_enqueued_state_after_dequeuing()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Dequeue 3 items
                var longs = await statefulServiceDemo.Dequeue(5);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(3);

                State.Should().HaveCount(4);
                State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains("QUEUEINFO")).Should()
                    .HaveCount(1);

                var item = await statefulServiceDemo.Dequeue(1);
                item.Single().Should().Be(6L);
            }

            [Test]
            public async Task _check_stored_string()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                await statefulServiceDemo.Enqueue(1);
                await statefulServiceDemo.Dequeue(1);

                var expectedId = $@"Overlord-StatefulServiceDemo{SchemaStateKey.Delimiter}range-0{SchemaStateKey.Delimiter}myQueue{SchemaStateKey.Delimiter}QUEUEINFO";
                State.Single().Key.Should().Be(expectedId);

                var stateValue = JsonUtility.NormalizeJsonString(JsonUtility.CullProperties(State.Single().Value, p => !p.StartsWith("_")));
                var expectedObject = JsonUtility.GetNormalizedJson(
                    new
                    {
                        state = new {HeadKey = 0, TailKey = 1},
                        serviceName = "Overlord-StatefulServiceDemo",
                        servicePartitionKey = "range-0",
                        partitionKey = "sA26BQ==",
                        schema = "myQueue",
                        key = "QUEUEINFO",
                        type = "ReliableQueueItem",
                        id = expectedId
                    });

                stateValue.Should().Be(expectedObject);
            }

            [Test]
            public async Task _should_be_able_to_enqueue_new_items_when_queue_info_is_loaded_from_prior_state()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                var key = new SchemaStateKey("Overlord-StatefulServiceDemo", "range-0", "myQueue", "QUEUEINFO");
                var stateObject =
                    new
                    {
                        state = new {HeadKey = 4, TailKey = 5},
                        serviceName = key.ServiceName,
                        servicePartitionKey = key.ServicePartitionKey,
                        partitionKey = "ghAHeQ==",
                        schema = key.Schema,
                        key = key.Key,
                        type = "ReliableQueueItem",
                        id = key.GetId()
                    };
                State.Add(stateObject.id, JsonUtility.GetNormalizedJson(stateObject));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Dequeue 3 items
                var longs = await statefulServiceDemo.Dequeue(5);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(3);

                State.Should().HaveCount(4);
                State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains("QUEUEINFO")).Should()
                    .HaveCount(1);

                var item = await statefulServiceDemo.Dequeue(1);
                item.Single().Should().Be(6L);
            }

            [Test]
            public async Task _should_be_able_to_store_beyond_int_maxvalue()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                var key = new SchemaStateKey("Overlord-StatefulServiceDemo", "range-0", "myQueue", "QUEUEINFO");
                var stateObject =
                    new
                    {
                        state = new {HeadKey = (long.MaxValue - 1), TailKey = long.MaxValue},
                        serviceName = key.ServiceName,
                        servicePartitionKey = key.ServicePartitionKey,
                        partitionKey = "ghAHeQ==",
                        schema = key.Schema,
                        key = key.Key,
                        type = "ReliableQueueItem",
                        id = key.GetId()
                    };
                State.Add(stateObject.id, JsonUtility.GetNormalizedJson(stateObject));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Dequeue 3 items
                var longs = await statefulServiceDemo.Dequeue(5);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(3);

                State.Should().HaveCount(4);
                State.Where(s => s.Key.Contains("range-0") && s.Key.Contains("myQueue") && s.Key.Contains("QUEUEINFO")).Should()
                    .HaveCount(1);

                var item = await statefulServiceDemo.Dequeue(1);
                item.Single().Should().Be(6L);
            }

            [Test]
            public async Task _should_return_queue_length_0_for_initial_queue()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                var length = await statefulServiceDemo.GetQueueLength();
                length.Should().Be(0);
            }

            [Test]
            public async Task _should_return_queue_length_0_for_emptied_queue()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));


                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Empty
                while (await statefulServiceDemo.Peek())
                    await statefulServiceDemo.Dequeue(1);

                var length = await statefulServiceDemo.GetQueueLength();
                length.Should().Be(0);
            }

            [Test]
            public async Task
                _should_return_queue_length_0_for_queue_with_multiple_enqueues_and_dequeues_until_drained()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));


                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Empty
                while (await statefulServiceDemo.Peek())
                    await statefulServiceDemo.Dequeue(1);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(50);

                await statefulServiceDemo.Dequeue(20);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(30);

                // Empty
                while (await statefulServiceDemo.Peek())
                    await statefulServiceDemo.Dequeue(1);


                var length = await statefulServiceDemo.GetQueueLength();
                length.Should().Be(0);
            }


            [Test]
            public async Task _should_return_queue_length_for_enqueued_items()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                var length = await statefulServiceDemo.GetQueueLength();
                length.Should().Be(5);
            }


            [Test]
            public async Task
                _should_return_queue_length_for_reamaining_enqueued_items_with_multiple_enqueues_and_dequeues()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

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
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Enumerate all
                var all = await statefulServiceDemo.EnumerateAll();

                all.Should().Equal(1, 2, 3, 4, 5);
            }

            [Test]
            public async Task _should_not_enumerate_anything_after_enqueued_items_have_been_dequeued()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5);

                // Enqueue 5 items
                await statefulServiceDemo.Dequeue(5);

                // Enumerate all
                var all = await statefulServiceDemo.EnumerateAll();

                all.Should().Equal();
            }

            [Test]
            public async Task _should_be_able_to_enumerate_all_enqueued_items_after_last_items_have_been_dequeued()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));


                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5); // 1 2 3 4 5

                // Enqueue 5 items
                await statefulServiceDemo.Dequeue(3); // _ _ _ 4 5				

                // Enumerate all
                var all = await statefulServiceDemo.EnumerateAll();

                all.Should().Equal(4, 5);
            }

            [Test]
            public async Task _should_be_able_to_enumerate_all_enqueued_items_after_multiple_enqueues_and_dequeues()
            {
                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_queue_enqueued.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));


                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5); // 1 2 3 4 5
                (await statefulServiceDemo.EnumerateAll()).Should().Equal(1, 2, 3, 4, 5);

                // Enqueue 5 items
                await statefulServiceDemo.Dequeue(3); // _ _ _ 4 5
                (await statefulServiceDemo.EnumerateAll()).Should().Equal(4, 5);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5); // _ _ _ 4 5 6 7 8 9 10
                (await statefulServiceDemo.EnumerateAll()).Should().Equal(4, 5, 6, 7, 8, 9, 10);

                // Enqueue 5 items
                await statefulServiceDemo.Dequeue(2); // _ _ _ _ _ 6 7 8 9 10
                (await statefulServiceDemo.EnumerateAll()).Should().Equal(6, 7, 8, 9, 10);

                // Enqueue 5 items
                await statefulServiceDemo.Enqueue(5); // _ _ _ _ _ 6 7 8 9 10 11 12 13 14 15
                (await statefulServiceDemo.EnumerateAll()).Should().Equal(6, 7, 8, 9, 10, 11, 12, 13, 14, 15);

                // Enqueue 5 items
                await statefulServiceDemo.Dequeue(3); // _ _ _ _ _ _ _ _ 9 10 11 12 13 14 15
                (await statefulServiceDemo.EnumerateAll()).Should().Equal(9, 10, 11, 12, 13, 14, 15);

                // Enumerate all
                var all = await statefulServiceDemo.EnumerateAll();

                all.Should().Equal(9, 10, 11, 12, 13, 14, 15);
            }
        }

        public abstract class Service_with_simple_dictionary : ServiceTestBase<
            ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo>
        {
            protected static ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo
                CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
            {
                return new ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo(context, stateSessionManager);
            }

            protected Service_with_simple_dictionary(TestRunner<ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.StatefulServiceDemo> testBase) :
                base(testBase)
            {
            }

            [Test]
            public async Task _should_persist_added_item()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

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
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

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
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Add("a", "A");

                // items and queue info state
                var keyValuePairs = await statefulServiceDemo.EnumerateAll();
                keyValuePairs.Should().BeEquivalentTo(new[]
                    {new KeyValuePair<string, string>("a", "A")});
            }

            [Test]
            public async Task _should_persist_added_items_and_enumerate_all()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

                // Enqueue 5 items
                await statefulServiceDemo.Add("a", "A");
                await statefulServiceDemo.Add("b", "B");
                await statefulServiceDemo.Add("c", "C");
                await statefulServiceDemo.Add("d", "D");
                await statefulServiceDemo.Add("e", "E");

                // items and queue info state
                var keyValuePairs = await statefulServiceDemo.EnumerateAll();
                keyValuePairs.Should().BeEquivalentTo(new[]
                {
                        new KeyValuePair<string, string>("a", "A"),
                        new KeyValuePair<string, string>("b", "B"),
                        new KeyValuePair<string, string>("c", "C"),
                        new KeyValuePair<string, string>("d", "D"),
                        new KeyValuePair<string, string>("e", "E")
                    });
            }

            [Test]
            public async Task _should_persist_added_and_removed_items_and_enumerate_all()
            {
                State.Should().HaveCount(0);

                var statefulServiceDemo = FabricRuntime.ServiceProxyFactory
                    .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_dictionary.
                        IStatefulServiceDemo>(
                        FabricApplication.ApplicationUriBuilder.Build("StatefulServiceDemo"),
                        new ServicePartitionKey(int.MinValue));

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
                keyValuePairs.Should().BeEquivalentTo(new[]
                {
                        new KeyValuePair<string, string>("a", "A"),
                        new KeyValuePair<string, string>("c", "C"),
                        new KeyValuePair<string, string>("e", "E"),
                        new KeyValuePair<string, string>("f", "F"),
                        new KeyValuePair<string, string>("g", "G")
                    });
            }
        }


        public abstract class Service_with_multiple_states : ServiceTestBase<
            ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo>
        {
            protected Service_with_multiple_states(TestRunner<ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo> testBase) : base(testBase)
            {
            }

            protected static ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo
                CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
            {
                return new ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.StatefulServiceDemo(context,
                        stateSessionManager);
            }

            private async Task RunWork()
            {
                foreach (var partitionKey in new[] { long.MinValue, long.MaxValue - 10 })
                {
                    var statefulServiceDemo = FabricApplication.FabricRuntime.ServiceProxyFactory
                        .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_multiple_states.
                            IStatefulServiceDemo>(
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

        public abstract class Service_with_simple_counter_state : ServiceTestBase<
            ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo>
        {
            protected Service_with_simple_counter_state(TestRunner<ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo> testBase) : base(testBase)
            {
            }

            protected static ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo
                CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
            {
                var service =
                    new ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.StatefulServiceDemo(context,
                        stateSessionManager);
                return service;
            }

            private async Task RunWork()
            {
                foreach (var partitionKey in new[] { long.MinValue, long.MaxValue - 10 })
                {
                    var statefulServiceDemo = FabricApplication.FabricRuntime.ServiceProxyFactory
                        .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_simple_counter_state.IStatefulServiceDemo>(
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


        public abstract class Service_with_polymorphic_states : ServiceTestBase<
            ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo>
        {

            protected static ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.
                StatefulServiceDemo
                CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
            {
                var service =
                    new ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo(
                        context,
                        stateSessionManager);
                return service;
            }

            private async Task RunWork()
            {
                foreach (var partitionKey in new[] { long.MinValue, long.MaxValue - 10 })
                {
                    var statefulServiceDemo = FabricApplication.FabricRuntime.ServiceProxyFactory
                        .CreateServiceProxy<ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.
                            IStatefulServiceDemo>(
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
                foreach (var service in Services)
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

            protected Service_with_polymorphic_states(TestRunner<FG.ServiceFabric.Tests.StatefulServiceDemo.With_polymorphic_array_state.StatefulServiceDemo> testBase) : base(testBase)
            {
            }
        }
    }

}
