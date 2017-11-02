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
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager
{
	public abstract class TestBase<T>
		where T : StatefulServiceDemoBase
	{
		private string ApplicationName => @"Overlord";
		protected MockFabricRuntime FabricRuntime;
		protected MockFabricApplication _fabricApplication;

		private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

		protected TestBase()
		{
		}

		protected IEnumerable<T> Services => _services;
		private IList<T> _services = new List<T>();

		[SetUp]
		public void Setup()
		{
			OnSetup();

			FabricRuntime = new MockFabricRuntime() {DisableMethodCallOutput = true};
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
				{
					Assert.Fail(@"Should have run the loop and updated the states by now");
				}
				Task.Delay(TimeSpan.FromMilliseconds(5)).GetAwaiter().GetResult();
				i++;
			} while (_runAsyncLoopUpdates.Keys.Count() < instances);

			foreach (var serviceInstance in FabricRuntime.GetInstances())
			{
				serviceInstance.CancellationTokenSource.Cancel();
			}
			foreach (var serviceInstance in FabricRuntime.GetInstances())
			{
				while (serviceInstance.RunAsyncEnded == null)
				{
					Task.Delay(TimeSpan.FromMilliseconds(5)).GetAwaiter().GetResult();
				}
			}
		}

		protected virtual void OnSetup() { }

		protected virtual void OnTearDown() { }

		private T CreateAndMonitorService(StatefulServiceContext context, IStateSessionManager stateSessionManager)
		{
			var service = CreateService(context, stateSessionManager);

			service.RunAsyncLoop += ServiceOnRunAsyncLoop;

			_services.Add(service);
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
			OnTearDown();
		}

		protected virtual Task SetupService()
		{
			return Task.FromResult(true);
		}

		protected virtual Task SetUpStates(InMemoryStateSessionManagerWithTransaction stateSessionManager)
		{
			return Task.FromResult(true);
		}

		protected T GetState<T>(string key)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(State[key]);
		}

		public abstract IDictionary<string, string> State { get; }

		public abstract IStateSessionManager CreateStateManager(MockFabricRuntime fabricRuntime, StatefulServiceContext context);		
	}
}