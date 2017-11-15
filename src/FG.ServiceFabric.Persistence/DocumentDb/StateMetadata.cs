using System;

namespace FG.ServiceFabric.DocumentDb
{
	internal class StateMetadata : IStateMetadata
	{
		public StateMetadata(string stateName, Guid partitionId, string partitionKey)
		{
			StateName = stateName;
			PartitionId = partitionId;
			PartitionKey = partitionKey;
		}

		public string StateName { get; set; }
		public Guid PartitionId { get; set; }
		public string PartitionKey { get; set; }
	}
}