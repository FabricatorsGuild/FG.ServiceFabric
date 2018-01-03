namespace FG.ServiceFabric.Services.Runtime.StateSession.Metadata
{
    public interface IServiceMetadata
    {
        string ServiceName { get; set; }
        string ServicePartitionKey { get; set; }
        string StoragePartitionKey { get; set; }
    }

    public class ServiceMetadata : IServiceMetadata
    {
        public string ServiceName { get; set; }
        public string ServicePartitionKey { get; set; }
        public string StoragePartitionKey { get; set; }
    }
}