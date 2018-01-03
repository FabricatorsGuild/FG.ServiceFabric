using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Fabric;

namespace FG.ServiceFabric.Testing.Mocks.Fabric
{
    public class MockPartitionEnumerationManager : IPartitionEnumerationManager
    {
        private readonly MockFabricRuntime _fabricRuntime;

        public MockPartitionEnumerationManager(MockFabricRuntime fabricRuntime)
        {
            _fabricRuntime = fabricRuntime;
        }

        public Task<ServicePartitionList> GetPartitionListAsync(Uri serviceUri)
        {
            var instances = _fabricRuntime.Instances.Where(i =>
                i.ServiceUri.ToString().Equals(serviceUri.ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (!instances.Any())
                throw new NotSupportedException(
                    $"Cannot enumerate partitions for {serviceUri}, call SetupService on MockFabricRuntime first");
            var partitions = _fabricRuntime.Instances
                .Where(i => i.ServiceUri.ToString()
                    .Equals(serviceUri.ToString(), StringComparison.InvariantCultureIgnoreCase))
                .Select(i => i.Partition);

            var servicePartitionListType = typeof(ServicePartitionList);
            var servicePartitionList =
                ReflectionUtils.CreateInstanceOfInternal(servicePartitionListType, new List<Partition>(partitions)) as
                    ServicePartitionList;

            return Task.FromResult(servicePartitionList);
        }

        public Task<ServicePartitionInformation> GetPartition(Uri serviceUri, long partitionKey)
        {
            var instances = _fabricRuntime.Instances.Where(i =>
                i.ServiceUri.ToString().Equals(serviceUri.ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (!instances.Any())
                throw new NotSupportedException(
                    $"Cannot enumerate partitions for {serviceUri}, call SetupService on MockFabricRuntime first");

            var partition = instances
                .Where(i => i.Partition.PartitionInformation.Kind == ServicePartitionKind.Int64Range)
                .Select(i => i.Partition.PartitionInformation as Int64RangePartitionInformation)
                .FirstOrDefault(i =>
                    partitionKey >= i.LowKey &&
                    partitionKey <= i.HighKey);

            if (partition == null) return null;

            var int64RangePartitionInformation =
                ReflectionUtils.ActivateInternalCtor<Int64RangePartitionInformation>();
            int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.LowKey,
                partition.LowKey);
            int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.LowKey,
                partition.HighKey);
            int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.Id, partition.Id);

            return Task.FromResult((ServicePartitionInformation) int64RangePartitionInformation);
        }

        public Task<ServicePartitionInformation> GetPartition(Uri serviceUri, string partitionKey)
        {
            var instances = _fabricRuntime.Instances.Where(i =>
                i.ServiceUri.ToString().Equals(serviceUri.ToString(), StringComparison.InvariantCultureIgnoreCase));
            if (!instances.Any())
                throw new NotSupportedException(
                    $"Cannot enumerate partitions for {serviceUri}, call SetupService on MockFabricRuntime first");

            var partition = instances
                .Where(i => i.Partition.PartitionInformation.Kind == ServicePartitionKind.Int64Range)
                .Select(i => i.Partition.PartitionInformation as NamedPartitionInformation)
                .FirstOrDefault(i => i.Name.Equals(partitionKey));

            if (partition == null) return null;

            var namedPartitionInformation = ReflectionUtils.ActivateInternalCtor<NamedPartitionInformation>();
            namedPartitionInformation.SetPrivateProperty(() => namedPartitionInformation.Name, partition.Name);
            namedPartitionInformation.SetPrivateProperty(() => namedPartitionInformation.Id, partition.Id);

            return Task.FromResult((ServicePartitionInformation) namedPartitionInformation);
        }
    }
}