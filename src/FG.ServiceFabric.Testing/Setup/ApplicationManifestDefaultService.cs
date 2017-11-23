using System.Fabric;

namespace FG.ServiceFabric.Testing.Setup
{
	internal class ApplicationManifestDefaultService
	{
		public string Name { get; set; }
		public string TypeName { get; set; }
		public bool IsStateless { get; set; }

		public int InstanceCount { get; set; }
		public int TargetReplicaSetSize { get; set; }
		public int MinReplicaSetSize { get; set; }

		public ServicePartitionKind ServicePartitionKind { get; set; }

		public int? PartitionCount { get; set; }
		public long? PartitioningLowLey { get; set; }
		public long? PartitioningHighLey { get; set; }

		public string[] NamedPartitions { get; set; }
	}
}