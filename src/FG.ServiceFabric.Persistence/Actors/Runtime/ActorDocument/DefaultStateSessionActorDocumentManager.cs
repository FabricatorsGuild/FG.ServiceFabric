using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FG.Common.Utils;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Actors.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Services.Runtime.StateSession;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.ActorDocument
{
    using Microsoft.ServiceFabric.Data;

    public class DefaultStateSessionActorDocumentManager : IStateSessionActorDocumentManager
    {
        private readonly IStateSessionManager _stateSessionManager;

        public DefaultStateSessionActorDocumentManager(IStateSessionManager stateSessionManager)
        {
            _stateSessionManager = stateSessionManager;
        }

        public async Task<ActorDocumentState> LoadActorAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.CreateSession())
            {
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                if (!state.HasValue)
                    return null;

                return state.Value;
            }
        }

        public async Task<ActorDocumentState> UpdateActorDocument(ActorId actorId, ActorStateChange[] actorStateChanges, UpsertType upsertType, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.Writable.CreateSession())
            {
                cancellationToken = cancellationToken.OrNone();
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var state = new ConditionalValue<ActorDocumentState>();

                bool loadActorDocument = upsertType == UpsertType.Update || upsertType == UpsertType.Auto;

                if (loadActorDocument)
                {
                    state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                }

                var actorDocument = !state.HasValue ? new ActorDocumentState(actorId) : state.Value;

                // No changes to the state document, bail out
                if (state.HasValue && (actorStateChanges == null || !actorStateChanges.Any()))
                    return actorDocument;

                foreach (var actorStateChange in actorStateChanges ?? new ActorStateChange[0])
                    switch (actorStateChange.ChangeKind)
                    {
                        case StateChangeKind.Add:
                        case StateChangeKind.Update:

                            actorDocument.States[actorStateChange.StateName] = actorStateChange.Value;
                            break;
                        case StateChangeKind.Remove:

                            if (actorDocument.States.ContainsKey(actorStateChange.StateName))
                                actorDocument.States.Remove(actorStateChange.StateName);
                            break;
                        case StateChangeKind.None:
                            break;
                    }

                var metadata = new ActorStateValueMetadata(StateWrapperType.ActorState, actorId);
                await session.SetValueAsync(key.Schema, key.Key, actorDocument, metadata, cancellationToken);

                await session.CommitAsync();

                return actorDocument;
            }
        }

        public async Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            cancellationToken = cancellationToken.OrNone();
            using (var session = _stateSessionManager.Writable.CreateSession())
            {
                // Save to database
                try
                {
                    // e.g.: servicename_partition1_ACTORDOC_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                    var key = new ActorDocumentStateKey(actorId);
                    await session.RemoveAsync(key.Schema, key.Key, cancellationToken);
                    await session.CommitAsync();
                }
                catch (KeyNotFoundException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new SessionStateActorStateProviderException($"Failed to RemoveActorAsync", ex);
                }
            }
        }

        public async Task<IActorReminderCollection> LoadAllRemindersAsync(CancellationToken cancellationToken)
        {
            var reminderCollection = new ActorReminderCollection();

            try
            {
                await this.IterateActorsAsync(
                    (actorId, actorDocument, _) =>
                        {
                            if (actorDocument.Reminders != null)
                            {
                                foreach (var reminder in actorDocument.Reminders?.Values)
                                {
                                    var actorReminderState = new ActorReminderState(reminder, DateTime.UtcNow);
                                    reminderCollection.Add(actorId, actorReminderState);
                                }
                            }

                            return Task.CompletedTask;
                        },
                    cancellationToken);
            }
            catch (SessionStateActorStateProviderException ex)
            {
                throw new SessionStateActorStateProviderException("Failed to LoadAllRemindersAsync", ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new SessionStateActorStateProviderException("Failed to LoadAllRemindersAsync", ex);
            }

            return reminderCollection;
        }

        public async Task IterateActorsAsync(Func<ActorId, ActorDocumentState, CancellationToken, Task> iterationFunc, CancellationToken cancellationToken)
        {
            var session = this._stateSessionManager.CreateSession();
            try
            {
                var schemaName = ActorDocumentStateKey.ActorDocumentStateSchemaName;

                cancellationToken = cancellationToken.OrNone();
                ContinuationToken continuationToken = null;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await session.FindByKeyPrefixAsync(schemaName, null, 100, continuationToken, cancellationToken);

                    // e.g.: servicename_partition1_ACTORDOC_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                    var actorIds = result.Items.Select(ActorSchemaKey.TryGetActorIdFromSchemaKey).ToArray();

                    foreach (var actorId in actorIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var key = new ActorDocumentStateKey(actorId);
                        var actorDocument = await session.GetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        await iterationFunc(actorId, actorDocument, cancellationToken);
                    }

                    continuationToken = result.ContinuationToken;
                }
                while (continuationToken != null);
            }
            catch (Exception ex)
            {
                throw new SessionStateActorStateProviderException("Failed to iterate through state", ex);
            }
            finally
            {
                session?.Dispose();
            }
        }

        public async Task UpdateActorStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> actorStateChanges, UpsertType upsertType, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.Writable.CreateSession())
            {
                cancellationToken = cancellationToken.OrNone();
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORDOC_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9

                ActorDocumentState actorDocument;

                bool hasDocument = false;

                if (upsertType == UpsertType.Auto || upsertType == UpsertType.Update)
                {
                    var state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                    actorDocument = !state.HasValue ? new ActorDocumentState(actorId) : state.Value;

                    hasDocument = state.HasValue;
                }
                else
                {
                    actorDocument = new ActorDocumentState();
                }

                // No changes to the state document, bail out
                if (hasDocument && (actorStateChanges == null || !actorStateChanges.Any())) return;

                foreach (var actorStateChange in actorStateChanges)
                    switch (actorStateChange.ChangeKind)
                    {
                        case StateChangeKind.Add:
                        case StateChangeKind.Update:

                            actorDocument.States[actorStateChange.StateName] = actorStateChange.Value;
                            break;
                        case StateChangeKind.Remove:

                            if (actorDocument.States.ContainsKey(actorStateChange.StateName))
                                actorDocument.States.Remove(actorStateChange.StateName);
                            break;
                        case StateChangeKind.None:
                            break;
                    }

                var metadata = new ActorStateValueMetadata(StateWrapperType.ActorState, actorId);
                await session.SetValueAsync(key.Schema, key.Key, actorDocument, metadata, cancellationToken);

                await session.CommitAsync();
            }
        }

        public async Task UpdateActorReminder(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.Writable.CreateSession())
            {
                cancellationToken = cancellationToken.OrNone();
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORDOC_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                var actorDocument = !state.HasValue ? new ActorDocumentState(actorId) : state.Value;

                var actorReminderData = new ActorReminderData(actorId, reminder.Name, reminder.DueTime, reminder.Period, reminder.State, DateTime.UtcNow);

                actorDocument.Reminders[reminder.Name] = actorReminderData;

                var metadata = new ActorStateValueMetadata(StateWrapperType.ActorState, actorId);
                await session.SetValueAsync(key.Schema, key.Key, actorDocument, metadata, cancellationToken);

                await session.CommitAsync();
            }
        }

        public async Task CompleteReminderAsync(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.Writable.CreateSession())
            {
                cancellationToken = cancellationToken.OrNone();
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORDOC_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                var actorDocument = !state.HasValue ? new ActorDocumentState(actorId) : state.Value;

                if (actorDocument.Reminders.ContainsKey(reminder.Name))
                {
                    var actorReminderData = new ActorReminderData(actorId, reminder.Name, reminder.DueTime, reminder.Period, reminder.State, DateTime.UtcNow);
                    actorReminderData.SetCompleted();
                    actorDocument.Reminders[reminder.Name] = actorReminderData;
                }

                var metadata = new ActorStateValueMetadata(StateWrapperType.ActorState, actorId);
                await session.SetValueAsync(key.Schema, key.Key, actorDocument, metadata, cancellationToken);

                await session.CommitAsync();
            }
        }

        public async Task DeleteRemindersAsync(ActorId actorId, IReadOnlyCollection<string> reminderNamesToDelete, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.Writable.CreateSession())
            {
                cancellationToken = cancellationToken.OrNone();
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORDOC_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                var actorDocument = !state.HasValue ? new ActorDocumentState(actorId) : state.Value;

                foreach (var reminderNameToDelete in reminderNamesToDelete)
                    if (actorDocument.Reminders.ContainsKey(reminderNameToDelete))
                        actorDocument.Reminders.Remove(reminderNameToDelete);

                var metadata = new ActorStateValueMetadata(StateWrapperType.ActorState, actorId);
                await session.SetValueAsync(key.Schema, key.Key, actorDocument, metadata, cancellationToken);

                await session.CommitAsync();
            }
        }

        public async Task<IEnumerable<string>> GetAllStateNames(ActorId actorId, CancellationToken cancellationToken)
        {
            using (var session = _stateSessionManager.CreateSession())
            {
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
                var key = new ActorDocumentStateKey(actorId);

                // e.g.: servicename_partition1_ACTORSTATE-myState_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var state = await session.TryGetValueAsync<ActorDocumentState>(key.Schema, key.Key, cancellationToken);
                if (!state.HasValue)
                    throw new KeyNotFoundException($"ActorDocument with id {key} was not found");

                return state.Value.States.Select(actorState => actorState.Key).ToArray();
            }
        }

        public async Task<PagedResult<ActorId>> GetActorsIdsAsync(
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var session = _stateSessionManager.CreateSession();
            try
            {
                var schemaName = ActorDocumentStateKey.ActorDocumentStateSchemaName;

                var result = await session.FindByKeyPrefixAsync(schemaName, null, numItemsToReturn, continuationToken, cancellationToken);
                // e.g.: servicename_partition1_ACTORID_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var actorIds = result.Items.Select(ActorSchemaKey.TryGetActorIdFromSchemaKey).ToArray();
                return new PagedResult<ActorId>
                           {
                               Items = actorIds,
                               ContinuationToken = result.ContinuationToken
                           };
            }
            catch (Exception ex)
            {
                throw new SessionStateActorStateProviderException($"Failed to GetActorsAsync", ex);
            }
            finally
            {
                session?.Dispose();
            }
        }

        public async Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(
            string stateName,
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            var session = _stateSessionManager.CreateSession();
            try
            {
                var schemaName = ActorDocumentStateKey.ActorDocumentStateSchemaName;

                var page = await session.FindByKeyPrefixAsync(schemaName, null, numItemsToReturn, continuationToken, cancellationToken);
                // e.g.: servicename_partition1_ACTORID_G:A4F3A8FC-801E-4940-8993-98CB6D7BCEF9
                var actorIds = page.Items.Select(ActorSchemaKey.TryGetActorIdFromSchemaKey).ToArray();
                var result = new List<KeyValuePair<ActorId, T>>();
                foreach (var actorId in actorIds)
                {
                    var key = new ActorStateKey(actorId, schemaName);
                    var state = await session.GetValueAsync<T>(key.Schema, key.Key, cancellationToken);
                    result.Add(new KeyValuePair<ActorId, T>(actorId, state));
                }

                return new PagedLookupResult<ActorId, T>
                           {
                               Items = result,
                               ContinuationToken = page.ContinuationToken
                           };
            }
            catch (Exception ex)
            {
                throw new SessionStateActorStateProviderException($"Failed to GetActorStatesAsync", ex);
            }
            finally
            {
                session?.Dispose();
            }
        }
    }
}