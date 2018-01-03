using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    public interface IDocumentDbSession
    {
        DocumentClient Client { get; }
        string DatabaseName { get; }
        string DatabaseCollection { get; }
        PartitionKey PartitionKey { get; }
    }
}