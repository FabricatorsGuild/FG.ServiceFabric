using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.InMemory;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.StatefulServiceDemo;
using Newtonsoft.Json;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager
{
    public abstract class TestBase<T>
        where T : StatefulServiceDemoBase
    {
        private readonly IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

        private readonly IList<T> _services = new List<T>();
        protected MockFabricApplication _fabricApplication;

        protected MockFabricRuntime FabricRuntime;

        public abstract IDictionary<string, string> State { get; }

        protected IEnumerable<T> Services => _services;

        private string ApplicationName => @"Overlord";

        public abstract IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime,
            StatefulServiceContext context);

        [SetUp]
        public void Setup()
        {
            OnSetup();

            FabricRuntime = new MockFabricRuntime
            {
                DisableMethodCallOutput = true
            };
            _fabricApplication = FabricRuntime.RegisterApplication(ApplicationName);

            _fabricApplication.SetupService(
                (context, stateManager) => CreateAndMonitorService(context, CreateStateManager(FabricRuntime, context)),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(2, long.MinValue, long.MaxValue));

            SetupService().GetAwaiter().GetResult();

            var instances = FabricRuntime.GetInstances().Count();
            var i = 0;
            do
            {
                if (i > 200)
                    Assert.Fail(@"Should have run the loop and updated the states by now");

                Task.Delay(TimeSpan.FromMilliseconds(5)).GetAwaiter().GetResult();
                i++;
            } while (_runAsyncLoopUpdates.Keys.Count() < instances);

            foreach (var serviceInstance in FabricRuntime.GetInstances())
                serviceInstance.CancellationTokenSource.Cancel();

            foreach (var serviceInstance in FabricRuntime.GetInstances())
                while (serviceInstance.RunAsyncEnded == null)
                    Task.Delay(TimeSpan.FromMilliseconds(5)).GetAwaiter().GetResult();
        }

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

            OnTearDown();
        }

        protected abstract T CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager);

        protected T2 GetState<T2>(string key)
        {
            return JsonConvert.DeserializeObject<T2>(State[key]);
        }

        protected virtual void OnSetup()
        {
        }

        protected virtual void OnTearDown()
        {
        }

        protected virtual Task SetupService()
        {
            return Task.FromResult(true);
        }

        protected virtual Task SetUpStates(InMemoryStateSessionManagerWithTransaction stateSessionManager)
        {
            return Task.FromResult(true);
        }

        private T CreateAndMonitorService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
        {
            var service = CreateService(context, stateSessionManager);

            service.RunAsyncLoop += ServiceOnRunAsyncLoop;

            _services.Add(service);
            return service;
        }

        private void ServiceOnRunAsyncLoop(object sender, RunAsyncLoopEventArgs e)
        {
            var partitionKey = StateSessionHelper
                .GetPartitionInfo(e.Context, () => FabricRuntime.PartitionEnumerationManager).GetAwaiter().GetResult();
            _runAsyncLoopUpdates[partitionKey] = e.Iteration;
        }
    }
}