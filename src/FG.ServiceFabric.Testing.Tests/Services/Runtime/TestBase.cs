namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager
{
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

    public abstract class TestBase<T>
        where T : StatefulServiceDemoBase
    {
        protected MockFabricApplication _fabricApplication;

        protected MockFabricRuntime FabricRuntime;

        private readonly IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

        private readonly IList<T> _services = new List<T>();

        public abstract IDictionary<string, string> State { get; }

        protected IEnumerable<T> Services => this._services;

        private string ApplicationName => @"Overlord";

        public abstract IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime, StatefulServiceContext context);

        [SetUp]
        public void Setup()
        {
            this.OnSetup();

            this.FabricRuntime = new MockFabricRuntime
                                     {
                                         DisableMethodCallOutput = true
                                     };
            this._fabricApplication = this.FabricRuntime.RegisterApplication(this.ApplicationName);

            this._fabricApplication.SetupService(
                (context, stateManager) => this.CreateAndMonitorService(context, this.CreateStateManager(this.FabricRuntime, context)),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(2, long.MinValue, long.MaxValue));

            this.SetupService().GetAwaiter().GetResult();

            var instances = this.FabricRuntime.GetInstances().Count();
            var i = 0;
            do
            {
                if (i > 200)
                {
                    Assert.Fail(@"Should have run the loop and updated the states by now");
                }

                Task.Delay(TimeSpan.FromMilliseconds(5)).GetAwaiter().GetResult();
                i++;
            }
            while (this._runAsyncLoopUpdates.Keys.Count() < instances);

            foreach (var serviceInstance in this.FabricRuntime.GetInstances())
            {
                serviceInstance.CancellationTokenSource.Cancel();
            }

            foreach (var serviceInstance in this.FabricRuntime.GetInstances())
            {
                while (serviceInstance.RunAsyncEnded == null)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(5)).GetAwaiter().GetResult();
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine($"States stored");
            Console.WriteLine($"______________________");
            foreach (var stateKey in this.State.Keys)
            {
                Console.WriteLine($"State: {stateKey}");
                Console.WriteLine($"{this.State[stateKey]}");
                Console.WriteLine($"______________________");
            }

            this.OnTearDown();
        }

        protected abstract T CreateService(StatefulServiceContext context, IStateSessionManager stateSessionManager);

        protected T2 GetState<T2>(string key)
        {
            return JsonConvert.DeserializeObject<T2>(this.State[key]);
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
            var service = this.CreateService(context, stateSessionManager);

            service.RunAsyncLoop += this.ServiceOnRunAsyncLoop;

            this._services.Add(service);
            return service;
        }

        private void ServiceOnRunAsyncLoop(object sender, RunAsyncLoopEventArgs e)
        {
            var partitionKey = StateSessionHelper.GetPartitionInfo(e.Context, () => this.FabricRuntime.PartitionEnumerationManager).GetAwaiter().GetResult();
            this._runAsyncLoopUpdates[partitionKey] = e.Iteration;
        }
    }
}