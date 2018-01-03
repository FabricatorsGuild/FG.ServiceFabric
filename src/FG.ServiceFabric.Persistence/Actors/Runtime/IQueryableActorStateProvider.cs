using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IQueryableActorStateProvider
    {
        Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(string stateName, int numItemsToReturn, ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken)) where T : class;
    }
}