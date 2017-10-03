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
		protected MockFabricRuntime FabricRuntime;

		protected readonly IDictionary<string, string> State = new ConcurrentDictionary<string, string>();
		private IDictionary<string, int> _runAsyncLoopUpdates = new ConcurrentDictionary<string, int>();

		protected TestBase()
		{
		}

		protected IEnumerable<T> Services => _services;
		private IList<T> _services = new List<T>();

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

		private IStateSessionManager CreateStateManager(StatefulServiceContext context)
		{
			var stateManager = new InMemoryStateSessionManagerWithTransaction(
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
}