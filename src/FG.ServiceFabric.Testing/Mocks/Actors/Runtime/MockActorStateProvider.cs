using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    public class MockActorStateProvider : IActorStateProvider
    {
        private readonly MockFabricRuntime _fabricRuntime;

        private readonly ConcurrentDictionary<ActorId, MockedInternalActorState> _trackedActors =
            new ConcurrentDictionary<ActorId, MockedInternalActorState>();

        public MockActorStateProvider(MockFabricRuntime fabricRuntime, IList<string> actionsPerformed = null)
        {
            ActionsPerformed = actionsPerformed ?? new List<string>();
            _fabricRuntime = fabricRuntime;
        }

        public IList<string> ActionsPerformed { get; }

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
            ActionsPerformed.Add("Initialize");
        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            throw new NotImplementedException();
        }

        public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken,
            Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(string backupFolderPath)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Func<CancellationToken, Task<bool>> OnDataLossAsync { get; set; }

        public void Initialize(ActorTypeInformation actorTypeInformation)
        {
            ActionsPerformed.Add($"{nameof(Initialize)} - {actorTypeInformation.ImplementationType}");
        }

        public Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add($"{nameof(ActorActivatedAsync)} - {actorId}");

            _trackedActors.GetOrAdd(actorId, a => new MockedInternalActorState());
            return Task.FromResult(true);
        }

        public Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add(nameof(ReminderCallbackCompletedAsync));
            return Task.FromResult(true);
        }

        public Task<T> LoadStateAsync<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add($"{nameof(LoadStateAsync)} - {actorId} - {stateName}");
            if (_trackedActors.TryGetValue(actorId, out var trackedActor))
                if (trackedActor.State.TryGetValue(stateName, out var trackedActorState))
                    return Task.FromResult((T) trackedActorState);

            return Task.FromResult(default(T));
        }

        public Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add(
                $"{nameof(SaveStateAsync)} - {actorId} - {JsonConvert.SerializeObject(stateChanges)}");

            if (!_trackedActors.TryGetValue(actorId, out var mockedInternalActorState))
            {
                mockedInternalActorState = new MockedInternalActorState();
                _trackedActors.TryAdd(actorId, mockedInternalActorState);
            }

            foreach (var actorStateChange in stateChanges)
                switch (actorStateChange.ChangeKind)
                {
                    case StateChangeKind.Add:
                        mockedInternalActorState.State.Add(actorStateChange.StateName, actorStateChange.Value);
                        break;
                    case StateChangeKind.Update:
                        mockedInternalActorState.State[actorStateChange.StateName] = actorStateChange.Value;
                        break;
                    case StateChangeKind.Remove:
                        mockedInternalActorState.State.Remove(actorStateChange.StateName);
                        break;
                }

            return Task.FromResult(true);
        }

        public Task<bool> ContainsStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add($"{nameof(ContainsStateAsync)} - {actorId} - {stateName}");
            if (_trackedActors.TryGetValue(actorId, out var actorState))
                if (actorState.State.ContainsKey(stateName))
                    return Task.FromResult(true);

            return Task.FromResult(false);
        }

        public Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add($"{nameof(RemoveActorAsync)} - {actorId}");
            _trackedActors.TryRemove(actorId, out var _);
            return Task.FromResult(true);
        }

        public Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId,
            CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add($"{nameof(EnumerateStateNamesAsync)} - {actorId}");
            var stateNames = _trackedActors[actorId].State.Select(actorState => actorState.Key);
            return Task.FromResult(stateNames);
        }

        public Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken,
            CancellationToken cancellationToken)
        {
            ActionsPerformed.Add($"{nameof(GetActorsAsync)} - {numItemsToReturn} - {continuationToken?.Marker}");

            var continueAt = continuationToken == null ? 0 : int.Parse((string) continuationToken.Marker);
            var actorsLeft = _trackedActors.Keys.Count - continueAt;
            var actualNumToReturn = Math.Min(numItemsToReturn, actorsLeft);

            var result = new PagedResult<ActorId>
            {
                Items = _trackedActors.Keys.Skip(continueAt).Take(actualNumToReturn).ToList(),
                ContinuationToken = actorsLeft - actualNumToReturn == 0
                    ? null
                    : new ContinuationToken((continueAt + actualNumToReturn).ToString())
            };

            return Task.FromResult(result);
        }

        public Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            ActionsPerformed.Add(nameof(SaveReminderAsync));

            if (_trackedActors.TryGetValue(actorId, out var actorState))
            {
                actorState.Reminders.Add(reminder.Name, reminder);
            }
            else
            {
                actorState = new MockedInternalActorState();
                actorState.Reminders.Add(reminder.Name, reminder);
                _trackedActors.TryAdd(actorId, actorState);
            }

            return Task.FromResult(true);
        }

        public Task DeleteReminderAsync(ActorId actorId, string reminderName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IActorReminderCollection> LoadRemindersAsync(
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task DeleteRemindersAsync(IReadOnlyDictionary<ActorId, IReadOnlyCollection<string>> reminderNames,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Func<CancellationToken, Task> OnRestoreCompletedAsync { get; set; }

        public void PrepareActorState(ActorId actorId, IDictionary<string, object> stateValues)
        {
            if (!_trackedActors.TryGetValue(actorId, out var actorState))
            {
                actorState = new MockedInternalActorState();
                _trackedActors.TryAdd(actorId, actorState);
            }

            foreach (var stateValue in stateValues)
                actorState.State[stateValue.Key] = stateValue.Value;
        }


        private class MockedInternalActorState
        {
            public MockedInternalActorState()
            {
                State = new ConcurrentDictionary<string, object>();
                Reminders = new ConcurrentDictionary<string, IActorReminder>();
            }

            public IDictionary<string, object> State { get; }
            public IDictionary<string, IActorReminder> Reminders { get; }
        }
    }
}