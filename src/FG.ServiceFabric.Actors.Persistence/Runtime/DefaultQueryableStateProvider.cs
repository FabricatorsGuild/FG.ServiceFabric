using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class DefaultQueryableStateProvider : IQueryableActorStateProvider
    {
        private readonly IActorStateProvider _actorStateProvider;

        public DefaultQueryableStateProvider(IActorStateProvider actorStateProvider)
        {
            _actorStateProvider = actorStateProvider;
        }

        public async Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(string stateName, int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            var page = await _actorStateProvider.GetActorsAsync(numItemsToReturn, continuationToken, cancellationToken);
            var result = new List<KeyValuePair<ActorId, T>>();
            foreach (var actorId in page.Items)
            {
                var actorState = await _actorStateProvider.LoadStateAsync<T>(actorId, stateName, cancellationToken);
                result.Add(new KeyValuePair<ActorId, T>(actorId, actorState));
            }

            return new PagedLookupResult<ActorId, T> {Items = result, ContinuationToken = page.ContinuationToken};
        }
    }
}