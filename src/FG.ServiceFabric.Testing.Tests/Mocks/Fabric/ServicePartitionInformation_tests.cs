// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FluentAssertions;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Mocks.Fabric
{
	public class ServicePartitionInformation_tests
	{

		[Test]
		public async Task MockPartitionEnumerationManager_should_return_one_partition_for_Stateless_service_with_Singleton_Partotioning()
		{
			var serviceName = new Uri(@"fabric:/TestApp/TestService", UriKind.Absolute);
			var partitionEnumerationManager = new MockPartitionEnumerationManager(MockPartition.CreateStatelessPartition(MockPartition.SingletonPartitionInformation));
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
		public async Task MockPartitionEnumerationManager_should_return_one_partition_for_Stateful_service_with_Uniform_Int64_Partitioning()
		{
			var serviceName = new Uri(@"fabric:/TestApp/TestService", UriKind.Absolute);
			var partitionEnumerationManager = new MockPartitionEnumerationManager(MockPartition.CreateStatefulPartition(MockPartition.Int64RangePartitionInformation));
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

			partitionKeys.Should().HaveCount(1);
			partitionKeys.Single().Id.Should().NotBe(Guid.Empty);
		}

		[Test]
		public async Task MockPartitionEnumerationManager_should_return_one_partition_for_Stateful_service_with_Named_Partitioning()
		{
			var serviceName = new Uri(@"fabric:/TestApp/TestService", UriKind.Absolute);
			var partitionEnumerationManager = new MockPartitionEnumerationManager(MockPartition.CreateStatefulPartition(MockPartition.NamedPartitionInformation));
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

			partitionKeys.Should().HaveCount(1);
			partitionKeys.Single().Id.Should().NotBe(Guid.Empty);
		}
	}
}