// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FluentAssertions;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Mocks.Fabric
{
	public class ServicePartitionInformation_tests
	{
		protected string ApplicationName => @"Overlord";

		[Test]
		public async Task
			MockPartitionEnumerationManager_should_return_one_partition_for_Stateless_service_with_Singleton_Partitioning()
		{
			var fabricRuntime = new MockFabricRuntime();
			var fabricApplication = fabricRuntime.RegisterApplication(ApplicationName);

			fabricApplication.SetupService(
				(context, stateManager) => new TestService(context, stateManager),
				serviceDefinition: MockServiceDefinition.CreateSingletonPartition());

			var serviceName = new Uri(@"fabric:/Overlord/TestService", UriKind.Absolute);
			var partitionEnumerationManager = new MockPartitionEnumerationManager(fabricRuntime);
			var partitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceName);

			// For each partition, build a service partition client used to resolve the low key served by the partition.
			var partitionKeys = new List<SingletonPartitionInformation>(partitionList.Count);
			foreach (var partition in partitionList)
			{
				var partitionInfo = partition.PartitionInformation as SingletonPartitionInformation;
				if (partitionInfo == null)
				{
					throw new InvalidOperationException(
						$"The service {serviceName} should have a Singleton partition. Instead: {partition.PartitionInformation.Kind}");
				}

				partitionKeys.Add(partitionInfo);
			}

			partitionKeys.Should().HaveCount(1);
			partitionKeys.Single().Id.Should().NotBe(Guid.Empty);
		}

		[Test]
		public async Task
			MockPartitionEnumerationManager_should_return_one_partition_for_Stateful_service_with_Uniform_Int64_Partitioning()
		{
			var fabricRuntime = new MockFabricRuntime();
			var fabricApplication = fabricRuntime.RegisterApplication(ApplicationName);

			fabricApplication.SetupService(
				(context, stateManager) => new TestService(context, stateManager),
				serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(10));

			var serviceName = new Uri(@"fabric:/Overlord/TestService", UriKind.Absolute);
			var partitionEnumerationManager = new MockPartitionEnumerationManager(fabricRuntime);
			var partitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceName);

			// For each partition, build a service partition client used to resolve the low key served by the partition.
			var partitionKeys = new List<Int64RangePartitionInformation>(partitionList.Count);
			foreach (var partition in partitionList)
			{
				var partitionInfo = partition.PartitionInformation as Int64RangePartitionInformation;
				if (partitionInfo == null)
				{
					throw new InvalidOperationException(
						$"The service {serviceName} should have a Singleton partition. Instead: {partition.PartitionInformation.Kind}");
				}

				partitionKeys.Add(partitionInfo);
			}

			partitionKeys.Should().HaveCount(10);
			partitionKeys.All(p => p.Id != Guid.Empty).Should().BeTrue();
		}

		[Test]
		public async Task
			MockPartitionEnumerationManager_should_return_one_partition_for_Stateful_service_with_Named_Partitioning()
		{
			var fabricRuntime = new MockFabricRuntime();
			var fabricApplication = fabricRuntime.RegisterApplication(ApplicationName);

			fabricApplication.SetupService(
				(context, stateManager) => new TestService(context, stateManager),
				serviceDefinition: MockServiceDefinition.CreateNamedPartitions("one", "two", "three"));

			var serviceName = new Uri(@"fabric:/Overlord/TestService", UriKind.Absolute);
			var partitionEnumerationManager = new MockPartitionEnumerationManager(fabricRuntime);
			var partitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceName);

			// For each partition, build a service partition client used to resolve the low key served by the partition.
			var partitionKeys = new List<NamedPartitionInformation>(partitionList.Count);
			foreach (var partition in partitionList)
			{
				var partitionInfo = partition.PartitionInformation as NamedPartitionInformation;
				if (partitionInfo == null)
				{
					throw new InvalidOperationException(
						$"The service {serviceName} should have a Singleton partition. Instead: {partition.PartitionInformation.Kind}");
				}

				partitionKeys.Add(partitionInfo);
			}

			partitionKeys.Should().HaveCount(3);
			partitionKeys.All(p => p.Id != Guid.Empty).Should().BeTrue();
		}

		public class TestService : StatefulService
		{
			public TestService(StatefulServiceContext serviceContext) : base(serviceContext)
			{
			}

			public TestService(StatefulServiceContext serviceContext, IReliableStateManagerReplica2 reliableStateManagerReplica)
				: base(serviceContext, reliableStateManagerReplica)
			{
			}
		}
	}
}