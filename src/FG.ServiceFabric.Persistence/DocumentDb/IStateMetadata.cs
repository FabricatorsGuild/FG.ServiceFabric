using System;

namespace FG.ServiceFabric.DocumentDb
{
    public interface IStateMetadata
    {
        string StateName { get; set; }
        Guid PartitionKey { get; set; }
    }
}