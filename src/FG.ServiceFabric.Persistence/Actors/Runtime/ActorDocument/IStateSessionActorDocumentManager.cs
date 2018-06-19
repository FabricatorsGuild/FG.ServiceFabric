using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FG.ServiceFabric.Services.Runtime.StateSession;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.ActorDocument
{
    using System;

    internal interface IStateSessionActorDocumentManager
    {
        Task<ActorDocumentState> LoadActorAsync(ActorId actorId, CancellationToken cancellationToken);

//         Task<ActorDocumentState> UpdateActorDocument(ActorId actorId, ActorStateChange[] actorStateChanges, UpsertType upsertType, CancellationToken cancellationToken);

        Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken);

//         Task<IActorReminderCollection> LoadAllRemindersAsync(CancellationToken cancellationToken);

        Task IterateActorsAsync(Func<ActorId, ActorDocumentState, CancellationToken, Task> iterationFunc, CancellationToken cancellationToken);

        Task UpdateActorStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> actorStateChanges, UpsertType upsertType, CancellationToken cancellationToken);

        Task UpdateActorReminder(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken);

        /// <summary>
        /// Sets a reminder status
        /// </summary>
        /// <param name="actorId">The actor id</param>
        /// <param name="reminder">The reminder</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task</returns>
        Task CompleteReminderAsync(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an actors named reminders
        /// </summary>
        /// <param name="actorId">The actor id</param>
        /// <param name="reminderNamesToDelete">The names of the reminders to delete</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task</returns>
        Task DeleteRemindersAsync(ActorId actorId, IReadOnlyCollection<string> reminderNamesToDelete, CancellationToken cancellationToken);

//         Task<IEnumerable<string>> GetAllStateNames(ActorId actorId, CancellationToken cancellationToken);

        Task<PagedResult<ActorId>> GetActorsIdsAsync(int numItemsToReturn, ContinuationToken continuationToken, CancellationToken cancellationToken = default(CancellationToken));

        Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(
            string stateName,
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class;
    }

    public static class StateSessionActorDocumentManagerExtensions
    {

    }
}