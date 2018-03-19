using System.Collections.Generic;
using System.Runtime.Serialization;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime.ActorDocument
{
    [DataContract]
    public class ActorDocumentState
    {
        public ActorDocumentState()
        {
            States = new Dictionary<string, object>();
            Reminders = new Dictionary<string, ActorReminderData>();
        }

        public ActorDocumentState(ActorId actorId)
        {
            States = new Dictionary<string, object>();
            Reminders = new Dictionary<string, ActorReminderData>();
            ActorId = ActorSchemaKey.GetActorIdSchemaKey(actorId);
        }

        public ActorDocumentState(ActorDocumentStateKey key)
        {
            States = new Dictionary<string, object>();
            Reminders = new Dictionary<string, ActorReminderData>();
            ActorId = key.Key;
        }

        [JsonProperty("states")]
        [DataMember]
        public Dictionary<string, object> States { get; private set; }

        [JsonProperty("reminders")]
        [DataMember]
        internal Dictionary<string, ActorReminderData> Reminders { get; private set; }

        [JsonProperty("actorId")]
        [DataMember]
        public string ActorId { get; private set; }
    }
}