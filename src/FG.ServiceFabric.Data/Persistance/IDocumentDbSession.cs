using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Persistance
{
    // TODO: Reminders.
    // TODO: How to distinquish data for different services? Actor id "test" could exist in multipe actor services.
    // TODO: Should we abstract away the concept of ActorId? Probably.
    public interface IDocumentDbSession
    {
        Task<object> GetState(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task<T> GetState<T>(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = new CancellationToken());
    }
}