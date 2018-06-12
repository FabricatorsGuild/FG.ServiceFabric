namespace FG.ServiceFabric.Actors.Runtime
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using FG.Common.Utils;
    using FG.ServiceFabric.Actors.Runtime.ActorDocument;
    using FG.ServiceFabric.Actors.Runtime.Reminders;
    using FG.ServiceFabric.Actors.Runtime.RuntimeInformation;
    using FG.ServiceFabric.Services.Runtime.StateSession;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Query;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    public class OverloadedStateSessionActorStateProvider : IOverloadedStateSessionActorStateProvider
    {
        private const string CosmosDefaultBaseUrl = "cosmos://default/";

        /// <summary>
        ///     The actorId used to
        /// </summary>
        private static readonly ActorId StateLoadedActorId = new ActorId(new Guid("1862f043-2507-41c4-8903-d1fd8f42ed8a"));

        private readonly IStateSessionActorDocumentManager _actorDocumentManager;

        private readonly IActorStateProvider _innerActorStateProvider;

        private readonly OverloadedStateSessionActorStateProviderSettings settings;

        private ReplicaRole _currentRole;

        private Func<CancellationToken, Task<bool>> _onDataLossAsync;

        private Func<CancellationToken, Task> _onRestoreCompletedAsync;

        private readonly ActorRuntimeInformationCollection actorRuntimeInformation = new ActorRuntimeInformationCollection();

        public OverloadedStateSessionActorStateProvider(
            IActorStateProvider innerActorStateProvider,
            IStateSessionManager stateSessionManager,
            OverloadedStateSessionActorStateProviderSettings settings = null)
        {
            this._innerActorStateProvider = innerActorStateProvider ?? throw new ArgumentNullException(nameof(innerActorStateProvider));
            this.settings = settings?.Clone() ?? new OverloadedStateSessionActorStateProviderSettings();

            this._actorDocumentManager = stateSessionManager as IStateSessionActorDocumentManager ?? new DefaultStateSessionActorDocumentManager(stateSessionManager);
        }

        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            get => this._onDataLossAsync;
            set
            {
                this._onDataLossAsync = value;
                this._innerActorStateProvider.OnDataLossAsync = this._onDataLossAsync;
            }
        }

        public Func<CancellationToken, Task> OnRestoreCompletedAsync
        {
            get => this._onRestoreCompletedAsync;
            set
            {
                this._onRestoreCompletedAsync = value;
                this._innerActorStateProvider.OnRestoreCompletedAsync = this._onRestoreCompletedAsync;
            }
        }

        public void Abort()
        {
            this._innerActorStateProvider.Abort();
        }

        public async Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            await this._innerActorStateProvider.ActorActivatedAsync(actorId, cancellationToken);

            if (this.actorRuntimeInformation.IsSaveInProgress(actorId) || await this._innerActorStateProvider.ContainsStateAsync(actorId, StateNames.IsSaveInProgress, cancellationToken))
            {
                // Previous state was not stored correctly. Update from documentdb
                await this.UpdateInnerStateFromDocumentStateSessionAsync(actorId, cancellationToken);
            }

            if (this.actorRuntimeInformation.ContainsActor(actorId) == false)
            {
                var hasDocument = await this._innerActorStateProvider.ContainsStateAsync(actorId, StateNames.HasDocumentState, cancellationToken);
                var isSaveInProgress = await this._innerActorStateProvider.ContainsStateAsync(actorId, StateNames.IsSaveInProgress, cancellationToken);
                this.actorRuntimeInformation.AddOrUpdateActorRuntimeInformation(actorId, ari => ari.SetIsSaveInProgress(isSaveInProgress).SetHasDocumentState(hasDocument));
            }
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return this._innerActorStateProvider.BackupAsync(backupCallback);
        }

        public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return this._innerActorStateProvider.BackupAsync(option, timeout, cancellationToken, backupCallback);
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            this._currentRole = newRole;
            return this._innerActorStateProvider.ChangeRoleAsync(newRole, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return this._innerActorStateProvider.CloseAsync(cancellationToken);
        }

        public Task<bool> ContainsStateAsync(ActorId actorId, string actorStateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this._innerActorStateProvider.ContainsStateAsync(actorId, actorStateName, cancellationToken);
        }

        public async Task DeleteReminderAsync(ActorId actorId, string reminderName, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this._innerActorStateProvider.DeleteReminderAsync(actorId, reminderName, cancellationToken);
            await this._actorDocumentManager.UpdateActorDocumentRemoveReminders(actorId, new[] { reminderName }, cancellationToken.OrNone());
        }

        public async Task DeleteRemindersAsync(
            IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>> reminderNames,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await this._innerActorStateProvider.DeleteRemindersAsync(reminderNames, cancellationToken);
            foreach (var reminder in reminderNames)
            {
                var actorId = reminder.Key;

                var reminderNamesForActorId = reminderNames[actorId];
                await this._actorDocumentManager.UpdateActorDocumentRemoveReminders(actorId, reminderNamesForActorId, cancellationToken.OrNone());
            }
        }

        public Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this._innerActorStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
        }

        public Task<PagedResult<ActorId>> GetActorsAsync(
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this._innerActorStateProvider.GetActorsAsync(numItemsToReturn, continuationToken, cancellationToken.OrNone());
        }

        public Task<PagedLookupResult<ActorId, T>> GetActorStatesAsync<T>(
            string stateName,
            int numItemsToReturn,
            ContinuationToken continuationToken,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            return this._actorDocumentManager.GetActorStatesAsync<T>(stateName, numItemsToReturn, continuationToken, cancellationToken);
        }

        public void Initialize(ActorTypeInformation actorTypeInformation)
        {
            this._innerActorStateProvider.Initialize(actorTypeInformation);
        }

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
            this._innerActorStateProvider.Initialize(initializationParameters);
        }

        public async Task<IActorReminderCollection> LoadRemindersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken = cancellationToken.OrNone();

            // If reminders are already loaded into state, no need to load the from db
            if (!await this._innerActorStateProvider.ContainsStateAsync(StateLoadedActorId, StateNames.IsStateLoaded, cancellationToken))
            {
                await this.LoadStateFromCosmosAsync(cancellationToken);
                await this._innerActorStateProvider.SaveStateAsync(
                    StateLoadedActorId,
                    new[] { new ActorStateChange(StateNames.IsStateLoaded, typeof(bool), true, StateChangeKind.Add) },
                    cancellationToken);
            }

            return await this._innerActorStateProvider.LoadRemindersAsync(cancellationToken);
        }

        public Task<T> LoadStateAsync<T>(ActorId actorId, string actorStateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this._innerActorStateProvider.LoadStateAsync<T>(actorId, actorStateName, cancellationToken);
        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
        {
            return this._innerActorStateProvider.OpenAsync(openMode, partition, cancellationToken);
        }

        public async Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this._innerActorStateProvider.ReminderCallbackCompletedAsync(actorId, reminder, cancellationToken);
            await this._actorDocumentManager.UpdateActorDocumentReminderComplete(actorId, reminder, cancellationToken.OrNone());
        }

        public async Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var isRemoving = await this._innerActorStateProvider.ContainsStateAsync(actorId, StateNames.IsDeleteInProgress, cancellationToken);

            if (isRemoving == false)
            {
                await this._innerActorStateProvider.SaveStateAsync(
                    actorId,
                    new[] { new ActorStateChange(StateNames.IsDeleteInProgress, typeof(bool), true, StateChangeKind.Add), },
                    cancellationToken);
            }

            this.actorRuntimeInformation.SetIsSaveInProgress(actorId);

            await this._actorDocumentManager.RemoveActorDocument(actorId, cancellationToken);

            cancellationToken = cancellationToken.OrNone();
            await this._innerActorStateProvider.RemoveActorAsync(actorId, cancellationToken);

            this.actorRuntimeInformation.RemoveActor(actorId);
        }

        public async Task RestoreAsync(string backupFolderPath)
        {
            if (backupFolderPath == null)
            {
                throw new ArgumentNullException(nameof(backupFolderPath), "backupFolderPath may not be null");
            }

            if (backupFolderPath.StartsWith(CosmosDefaultBaseUrl))
            {
                await this.RestoreFromCosmosAsync(backupFolderPath, CancellationToken.None);
                await this.OnRestoreCompletedAsync(CancellationToken.None);
                return;
            }

            await this._innerActorStateProvider.RestoreAsync(backupFolderPath);
        }

        public async Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
        {
            if (backupFolderPath == null)
            {
                throw new ArgumentNullException(nameof(backupFolderPath), "backupFolderPath may not be null");
            }

            if (backupFolderPath.StartsWith(CosmosDefaultBaseUrl))
            {
                await this.RestoreFromCosmosAsync(backupFolderPath, cancellationToken);
                await this.OnRestoreCompletedAsync(cancellationToken);
                return;
            }

            await this._innerActorStateProvider.RestoreAsync(backupFolderPath, restorePolicy, cancellationToken);
        }

        public async Task SaveReminderAsync(ActorId actorId, IActorReminder reminder, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this._innerActorStateProvider.SaveReminderAsync(actorId, reminder, cancellationToken);
            await this._actorDocumentManager.UpdateActorDocumentReminder(actorId, reminder, cancellationToken.OrNone());
        }

        public async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = default(CancellationToken))
        {
            var actorInformation = this.actorRuntimeInformation.GetActorRuntimeInformation(actorId);

            if (actorInformation.IsSaveInProgress == false)
            {
                // Save that we've started saving data
                await this._innerActorStateProvider.SaveStateAsync(
                actorId,
                new[] { new ActorStateChange(StateNames.IsSaveInProgress, typeof(bool), true, StateChangeKind.Add) },
                cancellationToken);
            }

            var hasDocument = actorInformation.HasDocumentState;

            this.actorRuntimeInformation.SetIsSaveInProgress(actorId, true);

            // Save to document
            await this._actorDocumentManager.UpdateActorDocument(actorId, stateChanges, hasDocument ? UpsertType.Update : UpsertType.Insert, cancellationToken);

            this.actorRuntimeInformation.SetHasDocumentState(actorId);

            // Save to state, and remove "save in progress" flag
            var newChanges = stateChanges?.ToList() ?? new List<ActorStateChange>();
            newChanges.Add(new ActorStateChange(StateNames.IsSaveInProgress, null, null, StateChangeKind.Remove));

            if (hasDocument == false)
            {
                newChanges.Add(new ActorStateChange(StateNames.HasDocumentState, typeof(bool), true, StateChangeKind.Add));
            }

            await this._innerActorStateProvider.SaveStateAsync(actorId, newChanges, cancellationToken);

            this.actorRuntimeInformation.SetIsSaveInProgress(actorId, false);
        }

        public async Task UpdateInnerStateFromDocumentStateSessionAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            ActorDocumentState document;

            if (this.settings.AlwaysCreateActorDocument)
            {
                document = await this._actorDocumentManager.UpdateActorDocument(actorId, null, UpsertType.Auto, cancellationToken.OrNone());
            }
            else
            {
                document = await this._actorDocumentManager.LoadActorDocument(actorId, cancellationToken.OrNone());

                if (document == null)
                {
                    document = new ActorDocumentState(actorId);
                }
                else
                {
                    this.actorRuntimeInformation.SetHasDocumentState(actorId);
                }
            }

            await this.UpdateInnerStateFromStateSession(document, cancellationToken.OrNone());
        }

        private async Task LoadStateFromCosmosAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this._actorDocumentManager.IterateAllDocumentStatesAsync(this.UpdateStateAndRemindersAsync, cancellationToken);
            }
            catch (Exception exception)
            {
                throw new Exception("Failed loading state from cosmos", exception);
            }
        }

        private async Task RestoreFromCosmosAsync(string backupFolderPath, CancellationToken cancellationToken)
        {
            var arguments = backupFolderPath.Substring(CosmosDefaultBaseUrl.Length);

            IReadOnlyCollection<ActorId> actors = null;

            if (arguments != "*" && string.IsNullOrEmpty(arguments))
            {
                var argumentArray = arguments.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                actors = argumentArray.Select(actorStringId => new ActorId(actorStringId)).ToArray();
            }

            await this.RestoreFromCosmosAsync(actors, cancellationToken);
        }

        /// <summary>
        ///     Restores state from the default cosmos document source
        /// </summary>
        /// <param name="actors">The actor ids to restore, or null to restore all actors of the current partition</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task</returns>
        private async Task RestoreFromCosmosAsync(IReadOnlyCollection<ActorId> actors = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (actors == null)
            {
                await this._actorDocumentManager.IterateAllDocumentStatesAsync(
                    async (actorId, actorDocumentState, token) => { await this.UpdateInnerStateFromStateSession(actorDocumentState, token); },
                    cancellationToken);
            }
            else
            {
                foreach (var actorId in actors)
                {
                    await this.UpdateInnerStateFromDocumentStateSessionAsync(actorId, cancellationToken);
                }
            }
        }


        private async Task UpdateInnerStateFromStateSession(ActorDocumentState actorDocument, CancellationToken cancellationToken)
        {
            var actorStateChanges = new List<ActorStateChange>();
            var actorId = ActorSchemaKey.TryGetActorIdFromSchemaKey(actorDocument.ActorId);

            var stateNamesEnumerable = await this._innerActorStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
            var stateNames = new HashSet<string>(stateNamesEnumerable);

            // Add or update state from document
            foreach (var actorState in actorDocument.States)
            {
                var containsState = stateNames.Contains(actorState.Key);

                actorStateChanges.Add(
                    new ActorStateChange(actorState.Key, actorState.Value.GetType(), actorState.Value, containsState ? StateChangeKind.Update : StateChangeKind.Add));
            }

            // Remove state that is no longer valid, but don't remove the preservables
            foreach (var stateName in stateNames.Where(sn => actorDocument.States.ContainsKey(sn) == false && StateNames.ActorStatesToPreserve.ContainsKey(sn) == false))
            {
                actorStateChanges.Add(new ActorStateChange(stateName, null, null, StateChangeKind.Remove));
            }

            if (stateNames.Contains(StateNames.IsSaveInProgress) == false)
            {
                actorStateChanges.Add(new ActorStateChange(StateNames.IsSaveInProgress, null, null, StateChangeKind.Remove));
            }

            if (stateNames.Contains(StateNames.HasDocumentState) == false)
            {
                actorStateChanges.Add(new ActorStateChange(StateNames.HasDocumentState, typeof(bool), true, StateChangeKind.Add));
            }

            await this._innerActorStateProvider.SaveStateAsync(actorId, actorStateChanges, cancellationToken);
        }

        private async Task UpdateInnerStateRemindersAsync(ActorId actorId, IEnumerable<ActorReminderData> reminders, CancellationToken cancellationToken)
        {
            foreach (var reminder in reminders)
            {
                var actorReminderState = new ActorReminderState(reminder, DateTime.UtcNow);
                await this._innerActorStateProvider.SaveReminderAsync(actorId, actorReminderState, cancellationToken);
            }
        }

        private async Task UpdateInnerStateAsync(ActorId actorId, Dictionary<string, object> states, HashSet<string> existingStateNames, CancellationToken cancellationToken)
        {
            var actorStateChanges = states.Select(
                actorState => new ActorStateChange(
                    actorState.Key,
                    actorState.Value.GetType(),
                    actorState.Value,
                    existingStateNames.Contains(actorState.Key) ? StateChangeKind.Update : StateChangeKind.Add));
            await this._innerActorStateProvider.SaveStateAsync(actorId, actorStateChanges.ToArray(), cancellationToken);
        }

        private async Task UpdateStateAndRemindersAsync(ActorId actorId, ActorDocumentState actorDocumentState, CancellationToken cancellationToken)
        {
            // Reminders
            if (actorDocumentState.Reminders != null)
            {
                await this.UpdateInnerStateRemindersAsync(actorId, actorDocumentState.Reminders.Values, cancellationToken);
            }

            // State
            if (actorDocumentState.States != null)
            {
                await this.UpdateInnerStateAsync(actorId, actorDocumentState, cancellationToken);
            }
        }

        private async Task UpdateInnerStateAsync(ActorId actorId, ActorDocumentState actorDocumentState, CancellationToken cancellationToken)
        {
            var existingStateNames = new HashSet<string>(await this._innerActorStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken));

            var actorStates = actorDocumentState.States;

            if (existingStateNames.Contains(StateNames.HasDocumentState) == false)
            {
                actorStates[StateNames.HasDocumentState] = true;
            }

            await this.UpdateInnerStateAsync(actorId, actorStates, existingStateNames, cancellationToken);

            this.actorRuntimeInformation.SetHasDocumentState(actorId);
        }

        private static class StateNames
        {
            public const string HasDocumentState = "HasDocumentState-389edb4a-30f9-4693-8f5c-78148e10480d";

            public const string IsSaveInProgress = "InnerStateIsSaveInProgressKeyName-877287eb-3c4f-4634-8006-ed2a6776d14c";

            public const string IsDeleteInProgress = "IsDeleteInProgress-ab9903f1-3b13-460d-a168-604d8e0dc106";

            public const string IsStateLoaded = "IsStateLoaded-e813376e-9991-4a1a-8cf3-6c59974645a7";

            public static readonly IReadOnlyDictionary<string, string> ActorStatesToPreserve = new Dictionary<string, string>
            {
                [HasDocumentState] = HasDocumentState
            };
        }
    }
}