using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IActorStateProviderQueryable
    {
        //
        // Summary:
        //     Gets the requested number of Actor states from the state provider.
        //
        // Parameters:
        //   numItemsToReturn:
        //     Number of items requested to be returned.
        //
        //   continuationToken:
        //     A continuation token to start querying the results from. A null value of continuation
        //     token means start returning values form the beginning.
        //
        //   cancellationToken:
        //     The token to monitor for cancellation requests.
        //
        // Returns:
        //     A task that represents the asynchronous operation of call to server.
        //
        // Exceptions:
        //   T:System.OperationCanceledException:
        //     The operation was canceled.
        //
        // Remarks:
        //     The continuationToken is relative to the state of actor state provider at the
        //     time of invocation of this API. If the state of actor state provider changes
        //     (i.e. new actors are activated or existing actors are deleted) in between calls
        //     to this API and the continuation token from previous call (before the state was
        //     modified) is supplied, the result may contain entries that were already fetched
        //     in previous calls.
        Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(string stateName, int numItemsToReturn, ContinuationToken continuationToken,
            CancellationToken cancellationToken);
    }
}