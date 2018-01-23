using System;
using System.Collections.Generic;
using System.Fabric;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.Persistence.Services.Runtime
{
    namespace With_StateSession_DocumentDbStateSession
    {
        public class
            With_StateSession_All_Tests : FG.ServiceFabric.Tests.Persistence.Services.Runtime.
                With_StateSession_All_Tests
        {
            private class TestRunnerBase<T> : With_StateSession_All_Tests.TestRunnerWithFunc<T>
                where T : StatefulServiceDemoBase
            {
                private readonly Guid _appId = Guid.NewGuid();
                private string _collectionName;
                private CosmosDbForTestingSettingsProvider _cosmosDbSettingsProvider;
                private DocumentDbStateSessionManagerWithTransactions _stateSessionManager;

                public override IDictionary<string, string> State => GetState();

                public override IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime,
                    StatefulServiceContext context)
                {
                    _collectionName = $"App-tests-{_appId}";

                    _cosmosDbSettingsProvider =
                        CosmosDbForTestingSettingsProvider.DefaultForCollection(_collectionName);
                    _stateSessionManager = new DocumentDbStateSessionManagerWithTransactions(
                        StateSessionHelper.GetServiceName(context.ServiceName),
                        _appId,
                        StateSessionHelper.GetPartitionInfo(context, () => fabricRuntime.PartitionEnumerationManager)
                            .GetAwaiter()
                            .GetResult(),
                        _cosmosDbSettingsProvider
                    );

                    return _stateSessionManager;
                }

                private IDictionary<string, string> GetState()
                {
                    if (_stateSessionManager is IDocumentDbDataManager dataManager)
                        return dataManager.GetCollectionDataAsync(_collectionName).GetAwaiter().GetResult();
                    return new Dictionary<string, string>();
                }

                private void DestroyCollection()
                {
                    if (_stateSessionManager is IDocumentDbDataManager dataManager)
                        dataManager.DestroyCollecton(_collectionName).GetAwaiter().GetResult();
                }

                protected override void OnTearDown()
                {
                    base.OnTearDown();

                    DestroyCollection();
                }

                public TestRunnerBase(Func<StatefulServiceContext, IStateSessionManager, T> createService) : base(
                    createService)
                {
                }
            }

            public class StateSession_transacted_scope : Runtime.StateSession_transacted_scope
            {
                private string _sessionId;
                private CosmosDbForTestingSettingsProvider _settingsProvider;
                private DocumentDbStateSessionManagerWithTransactions _stateSessionManager;

                protected override IStateSessionManager GetStateSessionManager()
                {
                    return new DocumentDbStateSessionManagerWithTransactions(
                        "StatefulServiceDemo",
                        Guid.NewGuid(),
                        "range-0",
                        _settingsProvider
                    );
                }

                [SetUp]
                public override void Setup()
                {
                    _sessionId = Guid.NewGuid().ToString();
                    var settingsProvider = CosmosDbForTestingSettingsProvider.DefaultForCollection(_sessionId);
                    _settingsProvider = settingsProvider;
                }

                [TearDown]
                public override void Teardown()
                {
                    if (_stateSessionManager is IDocumentDbDataManager dataManager)
                        dataManager.DestroyCollecton(_settingsProvider.CollectionName).GetAwaiter().GetResult();
                }

                public IStateSessionManager CreateStateManager()
                {
                    _stateSessionManager = new DocumentDbStateSessionManagerWithTransactions(
                        "StatefulServiceDemo",
                        Guid.NewGuid(),
                        "range-0",
                        _settingsProvider
                    );

                    return _stateSessionManager;
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