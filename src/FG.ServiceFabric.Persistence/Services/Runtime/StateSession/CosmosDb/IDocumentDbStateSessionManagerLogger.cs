using System;
using Microsoft.Azure.Documents;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    public interface IDocumentDbStateSessionManagerLogger
    {
        void StartingManager(string serviceName, Guid partitionId, string partitionKey, string endpointUri,
            string databaseName,
            string collection);

        void CreatingCollection(string collectionName);
        void CreatingClient();

        void CreatingSession();
        void ContainsInternalDocumentDbFailed(string id, DocumentClientException dcex);
        void ContainsInternalFailed(string id, Exception ex);
        void FindByKeyPrefixDocumenbtDbFailure(string schemaKeyPrefix, DocumentClientException dcex);
        void FindByKeyPrefixFailure(string schemaKeyPrefix, Exception ex);
        void EnumerateSchemaNamesDocumentDbFailure(string schemaKeyPrefix, DocumentClientException dcex);
        void EnumerateSchemaNamesFailure(string schemaKeyPrefix, Exception exception);
        void DisposingSession();
    }
}