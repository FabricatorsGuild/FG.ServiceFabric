using System;

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
    }
}