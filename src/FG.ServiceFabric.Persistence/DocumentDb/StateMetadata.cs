using System;

namespace FG.ServiceFabric.DocumentDb
{
    internal class StateMetadata : IStateMetadata
    {
        public string StateName { get; set; }
        public Guid PartitionKey { get; set; }
    }
}