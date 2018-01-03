using System;
using System.Runtime.Serialization;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
    [DataContract]
    internal sealed class ActorReminderData : IActorReminder
    {
        private ActorReminderData()
        {
        }

        internal ActorReminderData(ActorId actorId, string name, TimeSpan dueTime, TimeSpan period, byte[] state,
            DateTime utcCreationTime)
        {
            ActorId = actorId;
            Name = name;
            DueTime = dueTime;
            Period = period;
            State = state;
            UtcCreationTime = utcCreationTime;
        }

        internal ActorReminderData(ActorId actorId, IActorReminder reminder, DateTime utcCreationTime)
        {
            ActorId = actorId;
            Name = reminder.Name;
            DueTime = reminder.DueTime;
            Period = reminder.Period;
            State = reminder.State;
            UtcCreationTime = utcCreationTime;
        }

        [JsonProperty("actorId")]
        private string ActorIdValue
        {
            get => ActorSchemaKey.GetActorIdSchemaKey(ActorId);
            set => ActorId = ActorSchemaKey.TryGetActorIdFromSchemaKey(value);
        }

        [JsonIgnore]
        [DataMember]
        internal ActorId ActorId { get; private set; }

        [JsonProperty("utcCreationTime")]
        [DataMember]
        internal DateTime UtcCreationTime { get; private set; }

        [JsonProperty("isComplete")]
        [DataMember]
        internal bool IsComplete { get; private set; }


        [JsonProperty("utcCompletedTime")]
        [DataMember]
        internal DateTime UtcCompletedTime { get; private set; }

        [JsonProperty("name")]
        [DataMember]
        public string Name { get; private set; }

        [JsonProperty("dueTime")]
        [DataMember]
        public TimeSpan DueTime { get; private set; }

        [JsonProperty("period")]
        [DataMember]
        public TimeSpan Period { get; private set; }

        [JsonProperty("state")]
        [DataMember]
        public byte[] State { get; private set; }

        internal void SetCompleted()
        {
            IsComplete = true;
        }
    }
}