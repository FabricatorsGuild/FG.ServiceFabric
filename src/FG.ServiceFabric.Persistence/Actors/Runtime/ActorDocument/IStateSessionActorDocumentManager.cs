using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.ActorDocument
{
    internal interface IStateSessionActorDocumentManager
    {
        Task<ActorDocumentState> LoadActorDocument(ActorId actorId, CancellationToken cancellationToken);

        Task<ActorDocumentState> UpdateActorDocument(ActorId actorId, ActorStateChange[] actorStateChanges,
            CancellationToken cancellationToken);

        Task RemoveActorDocument(ActorId actorId, CancellationToken cancellationToken);

        Task<IActorReminderCollection> LoadAllRemindersAsync(
            CancellationToken cancellationToken);

        Task UpdateActorDocument(ActorId actorId, IReadOnlyCollection<ActorStateChange> actorStateChanges,
            CancellationToken cancellationToken);

        Task UpdateActorDocumentReminder(ActorId actorId, IActorReminder reminder, 
            CancellationToken cancellationToken);

        Task UpdateActorDocumentReminderComplete(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken);

        Task UpdateActorDocumentRemoveReminders(ActorId actorId, IReadOnlyCollection<string> reminderNamesToDelete,
            CancellationToken cancellationToken);

        Task<IEnumerable<string>> GetAllStateNames(ActorId actorId, CancellationToken cancellationToken);

        Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(string stateName, int numItemsToReturn, ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken)) where T : class;
    }
}