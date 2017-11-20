using System;
using System.Fabric;
using System.Fabric.Health;
using System.Fabric.Query;
using FG.Common.Utils;

namespace FG.ServiceFabric.Testing.Mocks.Fabric
{
	public class MockPartition
	{
		public static ServicePartitionInformation Int64RangePartitionInformation
		{
			get
			{
				var type = typeof(System.Fabric.Int64RangePartitionInformation);
				var int64RangePartitionInformation =
					ReflectionUtils.CreateInstanceOfInternal(type) as Int64RangePartitionInformation;
				int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.Id, Guid.NewGuid());
				return (ServicePartitionInformation) int64RangePartitionInformation;
			}
		}

		public static ServicePartitionInformation NamedPartitionInformation
		{
			get
			{
				var type = typeof(System.Fabric.NamedPartitionInformation);
				var namedPartitionInformation = ReflectionUtils.CreateInstanceOfInternal(type) as NamedPartitionInformation;
				namedPartitionInformation.SetPrivateProperty(() => namedPartitionInformation.Id, Guid.NewGuid());
				return (ServicePartitionInformation) namedPartitionInformation;
			}
		}

		public static ServicePartitionInformation SingletonPartitionInformation
		{
			get
			{
				var singletonPartitionInformation = new SingletonPartitionInformation();
				singletonPartitionInformation.SetPrivateProperty(() => singletonPartitionInformation.Id, Guid.NewGuid());
				return (ServicePartitionInformation) singletonPartitionInformation;
			}
		}

		public static ServicePartitionInformation CreateInt64Partiton(long lowKey, long highKey)
		{
			var type = typeof(System.Fabric.Int64RangePartitionInformation);
			var int64RangePartitionInformation =
				ReflectionUtils.CreateInstanceOfInternal(type) as Int64RangePartitionInformation;
			int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.Id, Guid.NewGuid());
			int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.LowKey, lowKey);
			int64RangePartitionInformation.SetPrivateProperty(() => int64RangePartitionInformation.HighKey, highKey);
			return (ServicePartitionInformation) int64RangePartitionInformation;
		}

		public static ServicePartitionInformation CreateNamedPartiton(string name)
		{
			var type = typeof(System.Fabric.NamedPartitionInformation);
			var namedPartitionInformation = ReflectionUtils.CreateInstanceOfInternal(type) as NamedPartitionInformation;
			namedPartitionInformation.SetPrivateProperty(() => namedPartitionInformation.Id, Guid.NewGuid());
			namedPartitionInformation.SetPrivateProperty(() => namedPartitionInformation.Name, name);
			return (ServicePartitionInformation) namedPartitionInformation;
		}

		public static Partition CreateStatefulPartition(ServicePartitionInformation servicePartitionInformation)
		{
			var statefulServicePartitionType = typeof(StatefulServicePartition);
			var targetReplicaSetSize = 1L;
			var minReplicaSetSize = 1L;
			var lastQuorumLossDuration = TimeSpan.Zero;
			var primaryEpoch = new Epoch();
			var statefulServicePartition = ReflectionUtils.CreateInstanceOfInternal(statefulServicePartitionType,
				servicePartitionInformation, targetReplicaSetSize, minReplicaSetSize, HealthState.Ok, ServicePartitionStatus.Ready,
				lastQuorumLossDuration, primaryEpoch);

			return (Partition) statefulServicePartition;
		}

		public static Partition CreateStatelessPartition(ServicePartitionInformation servicePartitionInformation)
		{
			var statelessServicePartitionType = typeof(StatelessServicePartition);
			var instanceCount = 1L;
			var statelessServicePartition = ReflectionUtils.CreateInstanceOfInternal(statelessServicePartitionType,
				(ServicePartitionInformation) servicePartitionInformation, instanceCount,
				HealthState.Ok, ServicePartitionStatus.Ready) as Partition;

			return (Partition) statelessServicePartition;
		}
	}
}