﻿using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Mocks.Fabric
{
	public class MockFabricRuntime_service_instance_activation
	{
		public interface IRunningService : IService
		{
			Task<int> GetCount();
		}

		public interface ITestActor : IActor
		{
			
		}

		public class TestStatelessService : StatelessService, IRunningService
		{
			private int _count = 0;

			public TestStatelessService(StatelessServiceContext serviceContext) : base(serviceContext)
			{
			}

			protected override async Task RunAsync(CancellationToken cancellationToken)
			{
				while (true)
				{
					_count++;

					await Task.Delay(100, cancellationToken);
				}
			}

			public Task<int> GetCount()
			{
				return Task.FromResult(_count);
			}


		}

		public class TestStatefulService : StatefulService, IRunningService
		{
			private int _count = 0;

			public TestStatefulService(StatefulServiceContext serviceContext) : base(serviceContext)
			{
			}

			public TestStatefulService(
				StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
			{
			}

			protected override async Task RunAsync(CancellationToken cancellationToken)
			{
				while (true)
				{
					_count++;

					await Task.Delay(100, cancellationToken);
				}
			}

			public Task<int> GetCount()
			{
				return Task.FromResult(_count);
			}
		}

		public class TestActorService : ActorService, IRunningService
		{
			private int _count = 0;

			public TestActorService(
				StatefulServiceContext context,
				ActorTypeInformation actorTypeInfo,
				Func<ActorService, ActorId, ActorBase> actorFactory = null,
				Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
				IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) :
				base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
			{
			}

			protected override async Task RunAsync(CancellationToken cancellationToken)
			{
				while (true)
				{
					Console.WriteLine($"RunAsync loop {_count} for {this.GetHashCode()}");
					_count++;

					await Task.Delay(100, cancellationToken);
				}
			}

			public Task<int> GetCount()
			{
				return Task.FromResult(_count);
			}
		}

		public class TestActor : Actor, ITestActor
		{
			public TestActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
			{
			}
		}

		[Test]
		public async Task MockServivceInstance_should_activate_RunAsync_for_Actor_service()
		{
			var fabricRuntime = new MockFabricRuntime("Overlord");

			fabricRuntime.SetupActor<TestActor, TestActorService>(
				(service, actorId) => new TestActor(service, actorId),
				(context, actorTypeInformation, stateProvider, stateManagerFactory) => new TestActorService(context, actorTypeInformation,
				stateProvider: stateProvider, stateManagerFactory: stateManagerFactory), serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue));

			await Task.Delay(500);

			var serviceUri = new Uri("fabric:/overlord/testactorservice");

			var mockServiceInstances = fabricRuntime.GetInstances();
			foreach (var mockServiceInstance in mockServiceInstances)
			{
				mockServiceInstance.RunAsyncStarted.Should().NotBeNull();
				mockServiceInstance.RunAsyncStarted.Value.Should().BeBefore(DateTime.Now);
				mockServiceInstance.RunAsyncEnded.Should().BeNull();
			}

			foreach (var partition in await fabricRuntime.PartitionEnumerationManager.GetPartitionListAsync(serviceUri))
			{
				var actorServiceProxy = fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IRunningService>(serviceUri,
					(partition.PartitionInformation as Int64RangePartitionInformation).LowKey);

				var count = await actorServiceProxy.GetCount();
				count.Should().BeGreaterThan(0);
			}
		}

		[Test]
		public async Task MockServivceInstance_should_activate_RunAsync_for_Stateless_service()
		{
			var fabricRuntime = new MockFabricRuntime("Overlord");

			fabricRuntime.SetupService(
				(context) => new TestStatelessService(context),
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue));

			await Task.Delay(500);

			var serviceUri = new Uri("fabric:/overlord/TestStatelessService");

			var mockServiceInstances = fabricRuntime.GetInstances();
			foreach (var mockServiceInstance in mockServiceInstances)
			{
				mockServiceInstance.RunAsyncStarted.Should().NotBeNull();
				mockServiceInstance.RunAsyncStarted.Value.Should().BeBefore(DateTime.Now);
				mockServiceInstance.RunAsyncEnded.Should().BeNull();
			}

			foreach (var partition in await fabricRuntime.PartitionEnumerationManager.GetPartitionListAsync(serviceUri))
			{
				var actorServiceProxy = fabricRuntime.ServiceProxyFactory.CreateServiceProxy<IRunningService>(serviceUri,
					new ServicePartitionKey((partition.PartitionInformation as Int64RangePartitionInformation).LowKey));

				var count = await actorServiceProxy.GetCount();
				count.Should().BeGreaterThan(0);
			}
		}

		[Test]
		public async Task MockServivceInstance_should_activate_RunAsync_for_Stateful_service()
		{
			var fabricRuntime = new MockFabricRuntime("Overlord");

			fabricRuntime.SetupService(
				(context, stateManager) => new TestStatefulService(context, stateManager),
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10, long.MinValue, long.MaxValue));

			await Task.Delay(500);

			var serviceUri = new Uri("fabric:/overlord/TestStatefulService");

			var mockServiceInstances = fabricRuntime.GetInstances();
			foreach (var mockServiceInstance in mockServiceInstances)
			{
				mockServiceInstance.RunAsyncStarted.Should().NotBeNull();
				mockServiceInstance.RunAsyncStarted.Value.Should().BeBefore(DateTime.Now);
				mockServiceInstance.RunAsyncEnded.Should().BeNull();
			}

			foreach (var partition in await fabricRuntime.PartitionEnumerationManager.GetPartitionListAsync(serviceUri))
			{
				var actorServiceProxy = fabricRuntime.ServiceProxyFactory.CreateServiceProxy<IRunningService>(serviceUri,
					new ServicePartitionKey((partition.PartitionInformation as Int64RangePartitionInformation).LowKey));

				var count = await actorServiceProxy.GetCount();
				count.Should().BeGreaterThan(0);
			}
		}

	}
}