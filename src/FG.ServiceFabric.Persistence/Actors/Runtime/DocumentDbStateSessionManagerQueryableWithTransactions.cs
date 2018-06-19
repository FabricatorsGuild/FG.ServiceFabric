using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using FG.ServiceFabric.Utils;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class DocumentDbStateSessionManagerQueryableWithTransactions : DocumentDbStateSessionManagerWithTransactions,
        IActorStateProviderQueryable
    {
        public DocumentDbStateSessionManagerQueryableWithTransactions(
            string serviceName,
            Guid partitionId,
            string partitionKey,
            ISettingsProvider settingsProvider,
            ICosmosDbClientFactory factory = null,
            ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.DirectTcp) :
            base(serviceName, partitionId, partitionKey, settingsProvider, factory, connectionPolicySetting)
        {
        }

        public async Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(string stateName, int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken)
        {
            using (var session = new DocumentDbStateSessionActorQueryable(this, new IStateSessionObject[0]))
            {
                throw new NotImplementedException();
                //var result = await session.GetActorStatesAsync<T>(stateName, numItemsToReturn, continuationToken, cancellationToken);
                //return result;
            }
        }

        protected override DocumentDbStateSession
            CreateSessionInternal(StateSessionManagerBase<DocumentDbStateSession> manager,
                IStateSessionObject[] stateSessionObjects)
        {
            return new DocumentDbStateSessionActorQueryable(this, stateSessionObjects);
        }

        protected override DocumentDbStateSession CreateSessionInternal(
            StateSessionManagerBase<DocumentDbStateSession> manager,
            IStateSessionReadOnlyObject[] stateSessionObjects)
        {
            return new DocumentDbStateSessionActorQueryable(this, stateSessionObjects);
        }

        protected class DocumentDbStateSessionActorQueryable : DocumentDbStateSession
        {
            public DocumentDbStateSessionActorQueryable(DocumentDbStateSessionManagerWithTransactions manager,
                IStateSessionReadOnlyObject[] stateSessionObjects) : base(manager, stateSessionObjects)
            {
            }

            public DocumentDbStateSessionActorQueryable(DocumentDbStateSessionManagerWithTransactions manager,
                IStateSessionObject[] stateSessionObjects) : base(manager, stateSessionObjects)
            {
            }
        }
    }
}