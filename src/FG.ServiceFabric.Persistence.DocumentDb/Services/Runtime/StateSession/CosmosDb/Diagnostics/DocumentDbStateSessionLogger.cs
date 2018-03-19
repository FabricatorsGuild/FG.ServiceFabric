using System;
using Microsoft.Azure.Documents;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    public class DocumentDbStateSessionLogger : IDocumentDbStateSessionLogger
    {
        public DocumentDbStateSessionLogger(string managerInstanceName, string sessionInstance)
        {
            throw new NotImplementedException();
        }

        public void StartingSession(string stateObjects)
        {
            throw new NotImplementedException();
        }

        public void DocumentClientException(string stateSessionOperation, int documentClientStatusCode, Error error, string message)
        {
            throw new NotImplementedException();
        }
    }
}