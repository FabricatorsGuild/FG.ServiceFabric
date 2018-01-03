using System;

namespace FG.ServiceFabric.DocumentDb
{
    public interface IStateMetadata
    {
        string StateName { get; set; }
        Guid PartitionId { get; set; }
        string PartitionKey { get; set; }
    }
}