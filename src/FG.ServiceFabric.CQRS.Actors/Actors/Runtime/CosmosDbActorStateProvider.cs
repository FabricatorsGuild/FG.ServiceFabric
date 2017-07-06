using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class CosmosDbActorStateProvider : WrappedActorStateProvider,
        IActorStateProvider
    {
        private readonly Func<IActorStateChangeMapper> _mapper;

        public CosmosDbActorStateProvider(ActorTypeInformation actorTypeInfo, Func<IActorStateChangeMapper> mapper,
            IActorStateProvider stateProvider = null)
            : base(actorTypeInfo, stateProvider)
        {
            _mapper = mapper;
        }

        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //Store unit of work.
            await base.SaveStateAsync(actorId, stateChanges, cancellationToken);
            await _mapper().SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        public override async Task<bool> ContainsStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (await base.ContainsStateAsync(actorId, stateName, cancellationToken))
                return true;

            if (await HasRestorableStateAsync(actorId, stateName, cancellationToken))
            {
                await RestoreStateAsync(actorId, stateName, cancellationToken);
                return true;
            }

            return false;
        }

        public async Task RestoreStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            var state = await _mapper().GetState(actorId, stateName, cancellationToken);
            await SaveStateAsync(actorId,
                new[] {new ActorStateChange(stateName, state.GetType(), state, StateChangeKind.Add)}, cancellationToken);
        }

        public Task<bool> HasRestorableStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            return _mapper().ContainsStateAsync(actorId, stateName, cancellationToken);
        }
    }

    // TODO: Reminders.
    // TODO: How to distinquish data for different services? Actor id "test" could exist in multipe actor services.
    // TODO: Should we abstract away the concept of ActorId? Probably.

   //
    public interface IActorStateChangeMapper
    {
        Task<object> GetState(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken());

        Task<T> GetState<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken());

        Task<bool> ContainsStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken());

        Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken());
    }
    
    public interface IDocumentDbWriter
    {
        Task UpsertDocument<T>(T document) where T : IHasIdentity;
    }

    public interface IDocumentDbReader
    {
        Task<T> GetDocument<T>(Guid id) where T : IHasIdentity;
    }

    public interface IHasIdentity
    {
        Guid Id { get; }
    }

    public class FileDocumentDb : IDocumentDbWriter, IDocumentDbReader
    {
        private JsonSerializerSettings _settings;
        private const string BaseFolderPath = @"C:\Temp\";

        public FileDocumentDb()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        private static string GetFolderPath()
        {
            var folder = Path.Combine(BaseFolderPath, "DummyCollection");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public Task UpsertDocument<T>(T document) where T : IHasIdentity
        {
            var payload = JsonConvert.SerializeObject(new ValueWrapper<T>(document), Formatting.Indented, _settings);
            File.WriteAllText(Path.Combine(GetFolderPath(), document.Id + ".json"), payload);
            return Task.FromResult(true);
        }

        public Task<T> GetDocument<T>(Guid id) where T : IHasIdentity
        {
            var filePath = Path.Combine(GetFolderPath(), id + ".json");

            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                var externalState = JsonConvert.DeserializeObject<ValueWrapper<T>>(content, _settings);

                if (externalState != null)
                {
                    return Task.FromResult((T) externalState.Value);
                }
            }

            throw new Exception("State not found");
        }
    }

    internal class ValueWrapper<T> where T : IHasIdentity
    {
        [Obsolete("Serialization only", true)]
        public ValueWrapper()
        {
        }

        public ValueWrapper(object value)
        {
            Value = value;
            Type = value.GetType().FullName;
        }

        public object Value { get; set; }
        public string Type { get; set; }
    }
}
