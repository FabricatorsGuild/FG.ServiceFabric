using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Persistance
{
    public interface IDbSession
    {
        Task<object> GetState(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = new CancellationToken());
    }

    public class FileSystemSession : IDbSession
    {
        private readonly JsonSerializerSettings _settings;
        private const string BaseFolderPath = @"C:\Temp\";

        public FileSystemSession()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        private static string GetFolderPath(ActorId actorId)
        {
            var folder = Path.Combine(BaseFolderPath, actorId.ToString());
            Directory.CreateDirectory(folder);
            return folder;
        }
        
        internal class ValueWrapper
        {
            [Obsolete("Serialization only", true)]
            public ValueWrapper()
            {
            }

            public ValueWrapper(ActorStateChange stateChange)
            {
                Value = stateChange.Value;
                ValueType = stateChange.Type.FullName;
            }

            public object Value { get; set; }
            public string ValueType { get; set; }
            public string PartitionId { get; set; }
        }

        public Task<object> GetState(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            var filePath = Path.Combine(GetFolderPath(actorId), stateName + ".json");

            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                var externalState = JsonConvert.DeserializeObject<ValueWrapper>(content, _settings);

                if (externalState != null)
                {
                    return Task.FromResult(externalState.Value);
                }
            }

            throw new Exception("State not found");
        }

        public Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(File.Exists(Path.Combine(GetFolderPath(actorId), stateName + ".json")));
        }

        public Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //todo:transactional
            foreach (var actorStateChange in stateChanges)
            {
                switch (actorStateChange.ChangeKind)
                {
                    case StateChangeKind.None:
                        break;
                    case StateChangeKind.Add:
                    case StateChangeKind.Update:
                        var document = JsonConvert.SerializeObject(new ValueWrapper(actorStateChange), Formatting.Indented, _settings);
                        File.WriteAllText(Path.Combine(GetFolderPath(actorId), actorStateChange.StateName + ".json"), document);

                        break;
                    case StateChangeKind.Remove:
                        File.Delete(Path.Combine(GetFolderPath(actorId), actorStateChange.StateName + ".json"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(actorStateChange.ChangeKind));
                }
            }

            return Task.FromResult(true);
        }
    }
}