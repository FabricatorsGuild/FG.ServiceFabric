using Microsoft.Azure.Documents;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    public interface IDocumentDbStateSessionLogger
    {
        void StartingSession(string stateObjects);

        void DocumentClientException(string stateSessionOperation, int documentClientStatusCode, Error error,
            string message);
    }
}