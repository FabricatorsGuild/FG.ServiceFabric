using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class DbStoredActor : Actor, IDbStoredActor
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ICosmosDocumentClientFactory _documentClientFactory;
        private DocumentClient _documentClient;
        private string _stateName = "count";

        private string DatabaseName => _settingsProvider["Database_Name"];
        private string DatabaseCollection => _settingsProvider["Database_Collection"];

        public DbStoredActor(ActorService actorService, ActorId actorId, ISettingsProvider settingsProvider, ICosmosDocumentClientFactory documentClientFactory)
            : base(actorService, actorId)
        {
            _settingsProvider = settingsProvider;
            _documentClientFactory = documentClientFactory;
        }

        protected override Task OnDeactivateAsync()
        {
            _documentClient.Dispose();
            return base.OnDeactivateAsync();
        }

        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            _documentClient = _documentClientFactory.Create(DatabaseName, DatabaseCollection, _settingsProvider).GetAwaiter().GetResult();
            return StateManager.TryAddStateAsync(_stateName, new CountState() { Count = 0});
        }
        

        Task<CountState> IDbStoredActor.GetCountAsync(CancellationToken cancellationToken)
        {
            return StateManager.GetStateAsync<CountState>(_stateName, cancellationToken);
        }

        async Task IDbStoredActor.SetCountAsync(CountState count, CancellationToken cancellationToken)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection);

            var document = new StateWrapper<CountState> {Payload = count, StateName = _stateName};

            await Task.WhenAll(new List<Task>
            {
                _documentClient.UpsertDocumentAsync(uri, document),

            });
            await StateManager.AddOrUpdateStateAsync(_stateName, count, (key, value) => count, cancellationToken);
        }
    }


    [Serializable]
    [DataContract]
    public class StateWrapper<T>
    {
        [JsonProperty(PropertyName = "id")]
        [DataMember]
        public string Id { get; set; }
        
        [DataMember]
        public string StateName { get; set; }

        [DataMember]
        public T Payload { get; set; }
    }
}
