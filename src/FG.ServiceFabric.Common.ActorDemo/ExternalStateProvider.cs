using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Tests.Actor
{
    public interface IExternalStateProvider : IActorStateProvider
    {
        Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task RestoreExternalState<T>(ActorId actorId, string stateName, CancellationToken cancellationToken);
    }

    public class FileStateProvider : ExternalStateProviderBase, IExternalStateProvider
    {
        private readonly IActorStateProvider _innerStateProvider;
        private readonly JsonSerializerSettings _settings;
        private const string BaseFolderPath = @"C:\Temp\";
        

        public FileStateProvider(ActorTypeInformation actorTypeInfor, IActorStateProvider innerStateProvider = null) 
            : base(actorTypeInfor, innerStateProvider)
        {
            _innerStateProvider = innerStateProvider;
            _settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        private static string GetPath(ActorId actorId)
        {
            var folder = Path.Combine(BaseFolderPath, actorId.ToString(), "Actors");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public override async Task RestoreExternalState<T>(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            {
                var content = File.ReadAllText(Path.Combine(GetPath(actorId), stateName + ".json"));
                var externalState = JsonConvert.DeserializeObject<T>(content, _settings);

                if (externalState != null)
                {
                    // Set lost state.
                    await _innerStateProvider.SaveStateAsync(actorId,
                        new[] { new ActorStateChange(stateName, typeof(T), externalState, StateChangeKind.Add) }, cancellationToken);
                }
            }
        }

        public override async Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await Task.Run(() => File.Exists(Path.Combine(GetPath(actorId), stateName + ".json")), cancellationToken);
        }

        protected override async Task SaveExternalStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken)
        {
            //todo:transactional
            foreach (var actorStateChange in stateChanges)
            {
                switch (actorStateChange.ChangeKind)
                {
                    case StateChangeKind.None:
                        break;
                    case StateChangeKind.Add:
                        var addData = JsonConvert.SerializeObject(actorStateChange.Value, Formatting.Indented, _settings);
                        await Task.Run(() => File.WriteAllText(Path.Combine(GetPath(actorId), actorStateChange.StateName + ".json"), addData), cancellationToken);
                        break;
                    case StateChangeKind.Update:
                        var updateData = JsonConvert.SerializeObject(actorStateChange.Value, Formatting.Indented, _settings);
                        await Task.Run(() => File.WriteAllText(Path.Combine(GetPath(actorId), actorStateChange.StateName + ".json"), updateData), cancellationToken);
                        break;
                    case StateChangeKind.Remove:
                        await Task.Run(() => File.Delete(Path.Combine(GetPath(actorId), actorStateChange.StateName + ".json")), cancellationToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(actorStateChange.ChangeKind));
                }
            }
        }
    }

    public abstract class ExternalStateProviderBase : IExternalStateProvider
    {
        private readonly IActorStateProvider _innerStateProvider;

        protected ExternalStateProviderBase(ActorTypeInformation actorTypeInfo, IActorStateProvider stateProvider = null)
        {
            _innerStateProvider = stateProvider ?? Actors.Runtime.ActorStateProviderHelper.CreateDefaultStateProvider(actorTypeInfo);
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

        public void Initialize(ActorTypeInformation actorTypeInformation)
        {
            _innerStateProvider.Initialize(actorTypeInformation);
        }

        public Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.ActorActivatedAsync(actorId, cancellationToken);
        }

        public Task ReminderCallbackCompletedAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.ReminderCallbackCompletedAsync(actorId, reminder, cancellationToken);
        }
        
        public async Task<T> LoadStateAsync<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (!await _innerStateProvider.ContainsStateAsync(actorId, stateName, cancellationToken) && await ContainsExternalStateAsync(actorId, stateName, cancellationToken))
            {
                await RestoreExternalState<T>(actorId, stateName, cancellationToken);
            }
            
            return await _innerStateProvider.LoadStateAsync<T>(actorId, stateName, cancellationToken);
        }

        public abstract Task RestoreExternalState<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken);
        

        public Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            SaveExternalStateAsync(actorId, stateChanges, cancellationToken);

            return _innerStateProvider.SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        protected abstract Task SaveExternalStateAsync(ActorId actorId,
            IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken);

        public Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.ContainsStateAsync(actorId, stateName, cancellationToken);
        }

        public abstract Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken());

        public Task RemoveActorAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.RemoveActorAsync(actorId, cancellationToken);
        }

        public Task<IEnumerable<string>> EnumerateStateNamesAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
        }

        public Task<PagedResult<ActorId>> GetActorsAsync(int numItemsToReturn, ContinuationToken continuationToken, CancellationToken cancellationToken)
        {
            return _innerStateProvider.GetActorsAsync(numItemsToReturn, continuationToken, cancellationToken);
        }

        public Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.SaveReminderAsync(actorId, reminder, cancellationToken);
        }

        public Task DeleteReminderAsync(ActorId actorId, string reminderName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.DeleteReminderAsync(actorId, reminderName, cancellationToken);
        }

        public Task<IActorReminderCollection> LoadRemindersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateProvider.LoadRemindersAsync(cancellationToken);
        }
    }
}