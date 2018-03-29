using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.Runtime.ActorDocument;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IOverloadedActorStateManager
    {
        Task ResetInnerStateAsync(ActorId actorId, CancellationToken cancellationToken);
        Task RewriteExternalStateAsync(ActorId actorId, CancellationToken cancellationToken);
    }

    public class OverloadedStateSessionActorStateProvider : IActorStateProvider, IQueryableActorStateProvider, IOverloadedActorStateManager
    {
        private readonly IStateSessionActorDocumentManager _actorDocumentManager;
        private readonly IActorStateProvider _innerActorStateProvider;

        private ReplicaRole _currentRole;

        private Func<CancellationToken, Task<bool>> _onDataLossAsync;
        private Func<CancellationToken, Task> _onRestoreCompletedAsync;

        public OverloadedStateSessionActorStateProvider(IActorStateProvider innerActorStateProvider,
            IStateSessionManager stateSessionManager)
        {
            _innerActorStateProvider = innerActorStateProvider;
            _actorDocumentManager = stateSessionManager as IStateSessionActorDocumentManager ??
                                    new DefaultStateSessionActorDocumentManager(stateSessionManager);
        }

        #region Queryable

        public Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(string stateName, int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            return _actorDocumentManager.GetActorStatesAsync<T>(stateName, numItemsToReturn, continuationToken,
                cancellationToken);
        }

        #endregion

        #region StateProviderReplica

        public void Initialize(ActorTypeInformation actorTypeInformation)
        {
            _innerActorStateProvider.Initialize(actorTypeInformation);
        }

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
            _innerActorStateProvider.Initialize(initializationParameters);
        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition,
            CancellationToken cancellationToken)
        {
            return _innerActorStateProvider.OpenAsync(openMode, partition, cancellationToken);
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            _currentRole = newRole;
            return _innerActorStateProvider.ChangeRoleAsync(newRole, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return _innerActorStateProvider.CloseAsync(cancellationToken);
        }

        public void Abort()
        {
            _innerActorStateProvider.Abort();
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return _innerActorStateProvider.BackupAsync(backupCallback);
        }

        public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken,
            Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return _innerActorStateProvider.BackupAsync(option, timeout, cancellationToken, backupCallback);
        }

        public Task RestoreAsync(string backupFolderPath)
        {
            return _innerActorStateProvider.RestoreAsync(backupFolderPath);
        }

        public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy,
            CancellationToken cancellationToken)
        {
            return _innerActorStateProvider.RestoreAsync(backupFolderPath, restorePolicy, cancellationToken);
        }

        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            get => _onDataLossAsync;
            set
            {
                _onDataLossAsync = value;
                _innerActorStateProvider.OnDataLossAsync = _onDataLossAsync;
            }
        }

        public Func<CancellationToken, Task> OnRestoreCompletedAsync
        {
            get => _onRestoreCompletedAsync;
            set
            {
                _onRestoreCompletedAsync = value;
                _innerActorStateProvider.OnRestoreCompletedAsync = _onRestoreCompletedAsync;
            }
        }

        #endregion

        #region Actor State

        private async Task UpdateInnerStateFromStateSession(ActorDocumentState actorDocument,
            CancellationToken cancellationToken)
        {
            var actorStateChanges = new List<ActorStateChange>();
            var actorId = ActorSchemaKey.TryGetActorIdFromSchemaKey(actorDocument.ActorId);
            foreach (var actorState in actorDocument.States)
            {
                var containsState =
                    await _innerActorStateProvider.ContainsStateAsync(actorId, actorState.Key, cancellationToken);
                actorStateChanges.Add(new ActorStateChange(actorState.Key, actorState.Value.GetType(),
                    actorState.Value, containsState ? StateChangeKind.Update : StateChangeKind.Add));
            }

            await _innerActorStateProvider.SaveStateAsync(actorId, actorStateChanges, cancellationToken);
        }

        public async Task ActorActivatedAsync(ActorId actorId,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await _innerActorStateProvider.ActorActivatedAsync(actorId, cancellationToken);

            var document = await _actorDocumentManager.UpdateActorDocument(actorId, null, cancellationToken.OrNone());
            await UpdateInnerStateFromStateSession(document, cancellationToken.OrNone());
        }

        public async Task<T> LoadStateAsync<T>(ActorId actorId, string actorStateName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var innerState =
                    await _innerActorStateProvider.LoadStateAsync<T>(actorId, actorStateName, cancellationToken);
                return innerState;
            }
            catch (KeyNotFoundException)
            {
                cancellationToken = cancellationToken.OrNone();
                // Throws KeyNotFoundException if not found in manager
                var document = await _actorDocumentManager.LoadActorDocument(actorId, cancellationToken);
                await UpdateInnerStateFromStateSession(document, cancellationToken);
                if (!document.States.ContainsKey(actorStateName))
                    throw;

                return (T) document.States[actorStateName];
            }
        }

        public async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _innerActorStateProvider.SaveStateAsync(actorId, stateChanges, cancellationToken);

            await _actorDocumentManager.UpdateActorDocument(actorId, stateChanges, cancellationToken.OrNone());
        }

        public async Task<bool> ContainsStateAsync(ActorId actorId, string actorStateName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var containsInner =
                await _innerActorStateProvider.ContainsStateAsync(actorId, actorStateName, cancellationToken);
            if (containsInner) return true;

            var document = await _actorDocumentManager.LoadActorDocument(actorId, cancellationToken);
            if (document == null)
                return false;
            await UpdateInnerStateFromStateSession(document, cancellationToken);
            return document.States.ContainsKey(actorStateName);
        }

        public async Task RemoveActorAsync(ActorId actorId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await _innerActorStateProvider.RemoveActorAsync(actorId, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                // Ignore this, if the StateSession reports KeyNotFound as well we throw that instead
            }

            cancellationToken = cancellationToken.OrNone();
            await _actorDocumentManager.RemoveActorDocument(actorId, cancellationToken);
        }

        public async Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var innerStateNames = await _innerActorStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
            if (innerStateNames != null && innerStateNames.Any())
                return innerStateNames;

            cancellationToken = cancellationToken.OrNone();
            var stateNames = await _actorDocumentManager.GetAllStateNames(actorId, cancellationToken);
            return stateNames;
        }

        public Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _actorDocumentManager.GetActorsAsync(numItemsToReturn, continuationToken,
                cancellationToken.OrNone());
        }

        #endregion

        #region Actor Reminders

        public async Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _innerActorStateProvider.SaveReminderAsync(actorId, reminder, cancellationToken);
            await _actorDocumentManager.UpdateActorDocumentReminder(actorId, reminder, cancellationToken.OrNone());
        }

        public async Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _innerActorStateProvider.ReminderCallbackCompletedAsync(actorId, reminder, cancellationToken);
            await _actorDocumentManager.UpdateActorDocumentReminderComplete(actorId, reminder,
                cancellationToken.OrNone());
        }

        public async Task DeleteReminderAsync(ActorId actorId, string reminderName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _innerActorStateProvider.DeleteReminderAsync(actorId, reminderName, cancellationToken);
            await _actorDocumentManager.UpdateActorDocumentRemoveReminders(actorId, new[] {reminderName},
                cancellationToken.OrNone());
        }

        public async Task DeleteRemindersAsync(IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>> reminderNames,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _innerActorStateProvider.DeleteRemindersAsync(reminderNames, cancellationToken);
            foreach (var actorId in reminderNames.Keys)
            {
                var reminderNamesForActorId = reminderNames[actorId];
                await _actorDocumentManager.UpdateActorDocumentRemoveReminders(actorId, reminderNamesForActorId,
                    cancellationToken.OrNone());
            }
        }

        public async Task<IActorReminderCollection> LoadRemindersAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken = cancellationToken.OrNone();

            var remindersByActorId = await _actorDocumentManager.LoadAllRemindersAsync(cancellationToken);

            // var innerRemindersByActorId = await _innerActorStateProvider.LoadRemindersAsync(cancellationToken);
            // TODO: Resync these reminders, remove missing, add new.

            var remindersToRemove = new ActorReminderNamesCollection(remindersByActorId);
            await _innerActorStateProvider.DeleteRemindersAsync(remindersToRemove, cancellationToken);

            foreach (var remindersForActorId in remindersByActorId)
            {
                var actorId = remindersForActorId.Key;
                var reminders = remindersForActorId.Value;

                foreach (var actorReminderState in reminders)
                    await _innerActorStateProvider.SaveReminderAsync(actorId, actorReminderState, cancellationToken);
            }

            return remindersByActorId;
        }

        #endregion

        #region  Overloaded Actor State Manager

        public async Task ResetInnerStateAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            await _innerActorStateProvider.RemoveActorAsync(actorId, cancellationToken);

            var document = await _actorDocumentManager.UpdateActorDocument(actorId, null, cancellationToken.OrNone());
            await UpdateInnerStateFromStateSession(document, cancellationToken.OrNone());
        }
        
        public async Task RewriteExternalStateAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            await _actorDocumentManager.RemoveActorDocument(actorId, cancellationToken);


        }
        

        #endregion
    }
}