using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Actors.Runtime
{
    public static class DocumentDbStateSessionExtensions
    {
        public static Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(
            this IStateSession session,
            string stateName,
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken)
        {
            if (session is IDocumentDbSession documentDbSession)
                return GetActorStatesAsync<T>(documentDbSession, stateName, numItemsToReturn, continuationToken,
                    cancellationToken);
            throw new NotImplementedException();
        }

        public static async Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(
            this IDocumentDbSession session,
            string stateName,
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken)
        {
            var results = new List<KeyValuePair<ActorId, T>>();
            var resultCount = 0;
            try
            {
                var nextToken = continuationToken?.Marker as string;

                var documentClient = await session.GetDocumentClientAsync();

                var documentCollectionQuery = documentClient.CreateDocumentQuery<ActorSingleStateDocument<T>>(
                    UriFactory.CreateDocumentCollectionUri(session.DatabaseName, session.DatabaseCollection),
                    new SqlQuerySpec
                    {
                        QueryText =
                            "SELECT c.actorId AS ActorId, c.state.states[@state] AS Content FROM c WHERE c.state.states[@state] != null",
                        Parameters = new SqlParameterCollection
                        {
                            new SqlParameter("@state", stateName)
                        }
                    },
                    new FeedOptions
                    {
                        PartitionKey = session.PartitionKey,
                        MaxItemCount = numItemsToReturn,
                        RequestContinuation = nextToken
                    }).AsDocumentQuery();

                while (documentCollectionQuery.HasMoreResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var response =
                        await documentCollectionQuery.ExecuteNextAsync<ActorSingleStateDocument<T>>(CancellationToken
                            .None);

                    foreach (var actorSingleStateDocument in response)
                    {
                        resultCount++;

                        var actorId = ActorSchemaKey.TryGetActorIdFromSchemaKey(actorSingleStateDocument.ActorID);
                        results.Add(new KeyValuePair<ActorId, T>(actorId, actorSingleStateDocument.Content));

                        if (resultCount >= numItemsToReturn)
                        {
                            var nextContinuationToken = response.ResponseContinuation == null
                                ? null
                                : new ContinuationToken(response.ResponseContinuation);
                            return new PagedLookupResult<ActorId, T>
                            {
                                ContinuationToken = nextContinuationToken,
                                Items = results
                            };
                        }
                    }
                }
                return new PagedLookupResult<ActorId, T> {ContinuationToken = null, Items = results};
            }
            catch (DocumentClientException dcex)
            {
                throw new StateSessionException($"GetActorStatesAsync for {stateName} failed, {dcex.Message}", dcex);
            }
            catch (Exception ex)
            {
                throw new StateSessionException($"GetActorStatesAsync for {stateName} failed, {ex.Message}", ex);
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local - Used in deserializing DocDb result
        private class ActorSingleStateDocument<T>
        {
            public string ActorID { get; set; }
            public T Content { get; set; }
        }
    }

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