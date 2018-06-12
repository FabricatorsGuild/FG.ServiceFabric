using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    using System.Threading.Tasks;

    public interface IDocumentDbSession
    {
        Task<DocumentClient> GetDocumentClientAsync();

        string DatabaseName { get; }
        string DatabaseCollection { get; }
        PartitionKey PartitionKey { get; }
    }
}