using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Data;
using FG.ServiceFabric.Persistance;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{

    public interface IExternalActorStateProvider : IActorStateProvider
    {
        Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task RestoreExternalState<T>(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
    }

    public class FileStoreStateProvider : WrappedActorStateProvider, IExternalActorStateProvider
    {
        private readonly Func<IDbSession> _dbSessionFactory;
        private const string BaseFolderPath = @"C:\Temp\";

        public FileStoreStateProvider(ActorTypeInformation actorTypeInfor, IActorStateProvider stateProvider = null)
            : base(actorTypeInfor, stateProvider)
        {
            _dbSessionFactory = () => new FileSystemSession();
        }
        
        public async Task RestoreExternalState<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (await ContainsExternalStateAsync(actorId, stateName, cancellationToken))
            {
                var externalState = await _dbSessionFactory()
                    .GetState(actorId, stateName, cancellationToken);

                if (externalState is T)
                {
                    // Set lost state.
                    await base.SaveStateAsync(actorId,
                        new[] { new ActorStateChange(stateName, typeof(T), externalState, StateChangeKind.Add) },
                        cancellationToken);
                }
            }
        }
        
        public Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _dbSessionFactory().ContainsStateAsync(actorId, stateName, cancellationToken);
        }

        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = new CancellationToken())
        {
            await _dbSessionFactory().SaveStateAsync(actorId, stateChanges, cancellationToken);
            await base.SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        //todo: reminders
        private static string GetFolderPath()
        {
            var folder = Path.Combine(BaseFolderPath, "ActorService");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public override Task<IActorReminderCollection> LoadRemindersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return base.LoadRemindersAsync(cancellationToken);
        }

        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public Task RestoreExternalReminders(CancellationToken cancellationToken = new CancellationToken())
        {
            var folderPath = Path.Combine(GetFolderPath(), ".json");

            foreach (var fileName in Directory.EnumerateFiles(folderPath))
            {
                if (!fileName.Contains("Reminder"))
                    continue;

                var content = File.ReadAllText(Path.Combine(folderPath,fileName));
                var externalState = JsonConvert.DeserializeObject<ExternalReminder>(content, _settings);

                if (externalState != null)
                {
                    // Set lost state.
                    return base.SaveReminderAsync(
                        externalState.ActorId,
                        externalState,
                        cancellationToken);
                }
            }
            
            return Task.FromResult(false);
        }
        
        public override async Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //todo: do we loose anything by doing this mapping? 
            var externalReminder = new ExternalReminder()
            {
                ActorId = actorId,
                DueTime = reminder.DueTime,
                Name = reminder.Name,
                Period = reminder.Period,
                State = reminder.State
            };

            var reminderData = JsonConvert.SerializeObject(externalReminder, Formatting.Indented, _settings);
            await Task.Run(() => File.WriteAllText(Path.Combine(GetFolderPath(), "Reminder_" + actorId + reminder.Name + ".json"), reminderData), cancellationToken);

            await base.SaveReminderAsync(actorId, reminder, cancellationToken);
        }

        private class ExternalReminder : IActorReminder
        {
            public ActorId ActorId { get; set; }
            public string Name { get; set; }
            public TimeSpan DueTime { get; set; }
            public TimeSpan Period { get; set; }
            public byte[] State { get; set; }
        }

        public override Task DeleteReminderAsync(ActorId actorId, string reminderName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            File.Delete(Path.Combine(GetFolderPath(), "Reminder", actorId + reminderName + ".json"));
            return base.DeleteReminderAsync(actorId, reminderName, cancellationToken);
        }
    }
}