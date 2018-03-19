using System;
using Microsoft.Azure.Documents;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    public class DocumentDbStateSessionManagerLogger : IDocumentDbStateSessionManagerLogger
    {
        public DocumentDbStateSessionManagerLogger(string instanceName)
        {
            throw new NotImplementedException();
        }

        public void StartingManager(string serviceName, Guid partitionId, string partitionKey, string endpointUri, string databaseName,
            string collection)
        {
            throw new NotImplementedException();
        }

        public void CreatingCollection(string collectionName)
        {
            throw new NotImplementedException();
        }

        public void CreatingClient()
        {
            throw new NotImplementedException();
        }

        public void CreatingSession()
        {
            throw new NotImplementedException();
        }

        public void ContainsInternalDocumentDbFailed(string id, DocumentClientException dcex)
        {
            throw new NotImplementedException();
        }

        public void ContainsInternalFailed(string id, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void FindByKeyPrefixDocumenbtDbFailure(string schemaKeyPrefix, DocumentClientException dcex)
        {
            throw new NotImplementedException();
        }

        public void FindByKeyPrefixFailure(string schemaKeyPrefix, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void EnumerateSchemaNamesDocumentDbFailure(string schemaKeyPrefix, DocumentClientException dcex)
        {
            throw new NotImplementedException();
        }

        public void EnumerateSchemaNamesFailure(string schemaKeyPrefix, Exception exception)
        {
            throw new NotImplementedException();
        }

        public void DisposingSession()
        {
            throw new NotImplementedException();
        }
    }
}