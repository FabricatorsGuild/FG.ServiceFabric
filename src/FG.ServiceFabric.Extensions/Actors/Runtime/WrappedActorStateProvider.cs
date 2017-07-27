using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class WrappedActorStateProvider : IActorStateProvider
    {
        private readonly IActorStateProvider _innerStateProvider;

        protected WrappedActorStateProvider(IActorStateProvider stateProvider = null, ActorTypeInformation actorTypeInfo = null)
        {
            if (actorTypeInfo == null && stateProvider == null)
            {
                throw new ArgumentNullException(
                    $"A non null instance of {nameof(actorTypeInfo)} must be provided when no {nameof(stateProvider)} is given in order to determine an backing state provider.");
            }

            _innerStateProvider = stateProvider ?? ActorStateProviderHelper.CreateDefaultStateProvider(actorTypeInfo);
        }

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
            _innerStateProvider.Initialize(initializationParameters);
        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
        {
            return _innerStateProvider.OpenAsync(openMode, partition, cancellationToken);
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            return _innerStateProvider.ChangeRoleAsync(newRole, cancellationToken);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return _innerStateProvider.CloseAsync(cancellationToken);
        }

        public void Abort()
        {
            _innerStateProvider.Abort();
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return _innerStateProvider.BackupAsync(backupCallback);
        }

        public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return _innerStateProvider.BackupAsync(option, timeout, cancellationToken, backupCallback);
        }

        public Task RestoreAsync(string backupFolderPath)
        {
            return _innerStateProvider.RestoreAsync(backupFolderPath);
        }

        public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
        {
            return _innerStateProvider.RestoreAsync(backupFolderPath, restorePolicy, cancellationToken);
        }

        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            set { _innerStateProvider.OnDataLossAsync = value; }
        }

        public virtual void Initialize(ActorTypeInformation actorTypeInformation)
        {
            _innerStateProvider.Initialize(actorTypeInformation);
        }

        public virtual Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.ActorActivatedAsync(actorId, cancellationToken);
        }

        public virtual Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.ReminderCallbackCompletedAsync(actorId, reminder, cancellationToken);
        }
        
        public virtual Task<T> LoadStateAsync<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.LoadStateAsync<T>(actorId, stateName, cancellationToken);
        }

        public virtual Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        public virtual Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.ContainsStateAsync(actorId, stateName, cancellationToken);
        }

        public virtual Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.RemoveActorAsync(actorId, cancellationToken);
        }

        public virtual Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
        }

        public virtual Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken, CancellationToken cancellationToken)
        {
            return _innerStateProvider.GetActorsAsync(numItemsToReturn, continuationToken, cancellationToken);
        }

        public virtual Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.SaveReminderAsync(actorId, reminder, cancellationToken);
        }

        public virtual Task DeleteReminderAsync(ActorId actorId, string reminderName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.DeleteReminderAsync(actorId, reminderName, cancellationToken);
        }

        public virtual Task<IActorReminderCollection> LoadRemindersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.LoadRemindersAsync(cancellationToken);
        }
    }
}