using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.FileSystem;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.Persistence.Services.Runtime
{
    namespace With_StateSession_FileSystemStateSession
    {


        public class With_StateSession_All_Tests : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
            With_StateSession_All_Tests
        {
            private class TestRunnerBase<T> : With_StateSession_All_Tests.TestRunnerWithFunc<T>
                where T : StatefulServiceDemoBase
            {
                private string _path;

                public override IDictionary<string, string> State => GetState();

                private FileSystemStateSessionManager _emptyManager =
                    new FileSystemStateSessionManager(null, Guid.Empty, null, null);

                protected override void OnSetup()
                {
                    _path = Path.Combine(Path.GetTempPath(),
                        $"{GetType().Assembly.GetName().Name}-{Guid.NewGuid().ToString()}");
                    Directory.CreateDirectory(_path);
                    base.OnSetup();
                }

                protected override void OnTearDown()
                {
                    try
                    {
                        Directory.Delete(_path, true);
                    }
                    catch (IOException)
                    {
                        // Just ignore it, it's a temp file anyway
                    }
                    base.OnTearDown();
                }

                public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime,
                    StatefulServiceContext context)
                {
                    return new FileSystemStateSessionManager(
                        StateSessionHelper.GetServiceName(context.ServiceName),
                        context.PartitionId,
                        StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager)
                            .GetAwaiter()
                            .GetResult(),
                        _path);
                }


                private IDictionary<string, string> GetState()
                {

                    var state = new Dictionary<string, string>();
                    var files = Directory.GetFiles(_path);

                    foreach (var file in files)
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        var id = SchemaStateKey.Parse(fileName).GetId();
                        var content = File.ReadAllText(file);

                        state.Add(id, content);
                    }
                    return state;
                }

                public TestRunnerBase(Func<StatefulServiceContext, IStateSessionManager, T> createService) : base(
                    createService)
                {
                }
            }

            public class StateSession_transacted_scope : Runtime.StateSession_transacted_scope
            {
                private string _path;

                [SetUp]
                public override void Setup()
                {
                    _path = Path.Combine(Path.GetTempPath(),
                        $"{GetType().Assembly.GetName().Name}-{Guid.NewGuid().ToString()}");
                    Directory.CreateDirectory(_path);
                }

                [TearDown]
                public override void Teardown()
                {
                    try
                    {
                        Directory.Delete(_path, true);
                    }
                    catch (IOException)
                    {
                        // Just ignore it, it's a temp file anyway
                    }
                }

                protected override IStateSessionManager GetStateSessionManager()
                {
                    return new FileSystemStateSessionManager("testservice", Guid.NewGuid(), "range-0", _path);
                }
            }

            public class Service_with_simple_queue_enqueued : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
                With_StateSession_All_Tests.
                Service_with_simple_queue_enqueued
            {
                public Service_with_simple_queue_enqueued()
                    : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                        With_simple_queue_enqueued.StatefulServiceDemo>(CreateService))
                {
                }
            }


            public class Service_with_simple_dictionary : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
                With_StateSession_All_Tests.
                Service_with_simple_dictionary
            {
                public Service_with_simple_dictionary()
                    : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                        With_simple_dictionary.StatefulServiceDemo>(CreateService))
                {
                }
            }

            public class Service_with_multiple_states : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
                With_StateSession_All_Tests.
                Service_with_multiple_states
            {
                public Service_with_multiple_states()
                    : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                        With_multiple_states.StatefulServiceDemo>(CreateService))
                {
                }
            }

            public class Service_with_simple_counter_state : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
                With_StateSession_All_Tests.
                Service_with_simple_counter_state
            {
                public Service_with_simple_counter_state()
                    : base(new TestRunnerBase<ServiceFabric.Tests.StatefulServiceDemo.
                        With_simple_counter_state.StatefulServiceDemo>(CreateService))
                {
                }
            }

            public class Service_with_polymorphic_states : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
                With_StateSession_All_Tests.
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
}