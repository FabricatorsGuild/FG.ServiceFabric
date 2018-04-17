using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Health;
using System.Fabric.Query;
using System.Linq;
using System.Numerics;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using Microsoft.ServiceFabric.Services.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    public class MockServiceDefinition
    {
        private readonly List<Replica> _instances;
        private readonly List<Partition> _partitions;

        private MockServiceDefinition()
        {
            _partitions = new List<Partition>();
            _instances = new List<Replica>();
        }


        public static MockServiceDefinition Default => CreateSingletonPartition();

        public ServicePartitionKind PartitionKind { get; private set; }

        public IEnumerable<Partition> Partitions => _partitions;
        public IEnumerable<Replica> Instances => _instances;

        public static MockServiceDefinition CreateStateless(int instances)
        {
            var mockServiceDefinition = new MockServiceDefinition { PartitionKind = ServicePartitionKind.Singleton };

            for (var i = 0; i < instances; i++)
            {
                var instance = ReflectionUtils.ActivateInternalCtor<StatelessServiceInstance>();
                instance.SetPrivateProperty(() => instance.Id, instance.GetHashCode());

                mockServiceDefinition._instances.Add(instance);
            }

            var partitionInformation = MockPartition.SingletonPartitionInformation;
            var statefulPartition = MockPartition.CreateStatefulPartition(partitionInformation);
            mockServiceDefinition._partitions.Add(statefulPartition);

            return mockServiceDefinition;
        }

        public static MockServiceDefinition CreateUniformInt64Partitions(int partitionCount,
            long lowKey = long.MinValue,
            long highKey = long.MaxValue)
        {
            var mockServiceDefinition = new MockServiceDefinition { PartitionKind = ServicePartitionKind.Int64Range };

            var lowKeyInt = new BigInteger(lowKey);
            var highKeyInt = new BigInteger(highKey);
            var partitionSize = (highKeyInt - lowKeyInt) / partitionCount;
            var currentLowKey = lowKeyInt;
            var currentHighKey = lowKey + partitionSize - 1;

            for (var i = 0; i < partitionCount; i++)
            {
                var partitionInformation =
                    MockPartition.CreateInt64Partiton((long)currentLowKey, (long)currentHighKey);
                var statefulPartition = MockPartition.CreateStatefulPartition(partitionInformation);
                mockServiceDefinition._partitions.Add(statefulPartition);

                currentLowKey = currentHighKey + 1;
                currentHighKey = currentHighKey + partitionSize;
            }

            mockServiceDefinition.CreateStatefulReplica();

            return mockServiceDefinition;
        }

        private void CreateStatefulReplica()
        {
            var replicaStatus = ServiceReplicaStatus.Ready;
            var healthState = HealthState.Ok;
            var replicaRole = ReplicaRole.Primary;
            var replicaAddress = "";
            var nodeName = "";
            var replicaId = (long)CRC64.ToCRC64(Guid.NewGuid().ToByteArray());
            var lastInBuildDuration = TimeSpan.FromSeconds(1);

            var instance = ReflectionUtils.ActivateInternalCtor<StatefulServiceReplica>(
                replicaStatus,
                healthState,
                replicaRole,
                replicaAddress,
                nodeName,
                replicaId,
                lastInBuildDuration);
            instance.SetPrivateProperty(() => instance.Id, instance.GetHashCode());

            _instances.Add(instance);
        }

        public static MockServiceDefinition CreateNamedPartitions(params string[] partitionNames)
        {
            var mockServiceDefinition = new MockServiceDefinition { PartitionKind = ServicePartitionKind.Named };

            foreach (var partitionName in partitionNames)
            {
                var partitionInformation = MockPartition.CreateNamedPartiton(partitionName);
                var statefulPartition = MockPartition.CreateStatefulPartition(partitionInformation);
                mockServiceDefinition._partitions.Add(statefulPartition);
            }

            mockServiceDefinition.CreateStatefulReplica();

            return mockServiceDefinition;
        }

        public static MockServiceDefinition CreateSingletonPartition()
        {
            var mockServiceDefinition = new MockServiceDefinition { PartitionKind = ServicePartitionKind.Singleton };

            var partitionInformation = MockPartition.SingletonPartitionInformation;
            var statefulPartition = MockPartition.CreateStatefulPartition(partitionInformation);
            mockServiceDefinition._partitions.Add(statefulPartition);

            mockServiceDefinition.CreateStatefulReplica();

            return mockServiceDefinition;
        }


        public Guid GetPartion(ServicePartitionKey partitionKey)
        {
            if (PartitionKind != partitionKey.Kind)
                throw new NotSupportedException(
                    $"Service has {PartitionKind} partitioning but a partitionKey of type {partitionKey.Kind} was requested");

            if (PartitionKind == ServicePartitionKind.Singleton)
                return _partitions.Single().PartitionInformation.Id;

            if (PartitionKind == ServicePartitionKind.Int64Range)
            {
                var value = (long)partitionKey.Value;
                return _partitions.Select(p => p.PartitionInformation as Int64RangePartitionInformation)
                    .First(p => p.LowKey <= value && p.HighKey >= value).Id;
            }

            if (PartitionKind == ServicePartitionKind.Named)
            {
                var value = (string)partitionKey.Value;
                return _partitions.Select(p => p.PartitionInformation as NamedPartitionInformation)
                    .First(p => p.Name == value).Id;
            }

            throw new NotSupportedException($"PartitionKind {PartitionKind} is not supported");
        }

        public override string ToString()
        {
            return
                $"ServiceDefinition {PartitionKind}, partitions {_partitions?.Count ?? 0}, instances {_instances?.Count ?? 0}";
        }
    }
}