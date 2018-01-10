using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.InMemory;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.StatefulServiceDemo;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_InMemoryStateSessionWithTransactions
{
    public class With_StateSession_InMemoryStateSessionWithTransactions : With_StateSession_All_Tests
    {
        private class TestRunnerBase<T> : TestRunnerWithFunc<T> where T : StatefulServiceDemoBase
        {
            private readonly IDictionary<string, string> _state = new ConcurrentDictionary<string, string>();

            public TestRunnerBase(Func<StatefulServiceContext, IStateSessionManager, T> createServiceFunc) : base(createServiceFunc)
            {
            }

            public override IDictionary<string, string> State => _state;

            protected override void OnSetup()
            {
                State.Clear();
                base.OnSetup();
            }

            public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime,
                StatefulServiceContext context)
            {
                return new InMemoryStateSessionManagerWithTransaction(
                    StateSessionHelper.GetServiceName(context.ServiceName),
                    context.PartitionId,
                    StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager)
                        .GetAwaiter()
                        .GetResult(),
                    _state);
            }
        }

        public abstract class StateSession_transacted_scope : With_StateSessionManager.StateSession_transacted_scope
        {
            Dictionary<string, string> _state = new Dictionary<string, string>();

            protected override IStateSessionManager GetStateSessionManager()
            {
                return new InMemoryStateSessionManagerWithTransaction("testservice", Guid.NewGuid(), "range-0", _state);
            }
        }

        public class Service_with_simple_queue_enqueued : With_StateSession_All_Tests.
            Service_with_simple_queue_enqueued
        {
            public Service_with_simple_queue_enqueued()
                : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                    With_simple_queue_enqueued.StatefulServiceDemo>(CreateService))
            {
            }
        }


        public class Service_with_simple_dictionary : With_StateSession_All_Tests.
            Service_with_simple_dictionary
        {
            public Service_with_simple_dictionary()
                : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                    With_simple_dictionary.StatefulServiceDemo>(CreateService))
            {
            }
        }

        public class Service_with_multiple_states : With_StateSession_All_Tests.
            Service_with_multiple_states
        {
            public Service_with_multiple_states()
                : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                    With_multiple_states.StatefulServiceDemo>(CreateService))
            {
            }
        }

        public class Service_with_simple_counter_state : With_StateSession_All_Tests.
            Service_with_simple_counter_state
        {
            public Service_with_simple_counter_state()
                : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                    With_simple_counter_state.StatefulServiceDemo>(CreateService))
            {
            }
        }

        public class Service_with_polymorphic_states : With_StateSession_All_Tests.
            Service_with_polymorphic_states
        {
            public Service_with_polymorphic_states()
                : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                    With_polymorphic_array_state.StatefulServiceDemo>(CreateService))
            {
            }
        }
    }
}