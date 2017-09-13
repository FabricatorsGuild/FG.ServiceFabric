using System;
using System.Runtime.Serialization;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
	[DataContract]
	internal sealed class ActorReminderData
	{
		[JsonProperty("actorId")]
		private string ActorIdValue {
			get { return StateSessionHelper.GetActorIdSchemaKey(ActorId); }
			set { ActorId = StateSessionHelper.TryGetActorIdFromSchemaKey(value); }
		}

		[JsonIgnore]
		[DataMember]
		internal ActorId ActorId { get; private set; }

		[JsonProperty("name")]
		[DataMember]
		internal string Name { get; private set; }

		[JsonProperty("dueTime")]
		[DataMember]
		internal TimeSpan DueTime { get; private set; }

		[JsonProperty("period")]
		[DataMember]
		internal TimeSpan Period { get; private set; }

		[JsonProperty("state")]
		[DataMember]
		internal byte[] State { get; private set; }

		[JsonProperty("utcCreationTime")]
		[DataMember]
		internal DateTime UtcCreationTime { get; private set; }

		private ActorReminderData()
		{
			
		}

		internal ActorReminderData(ActorId actorId, string name, TimeSpan dueTime, TimeSpan period, byte[] state, DateTime utcCreationTime)
		{
			this.ActorId = actorId;
			this.Name = name;
			this.DueTime = dueTime;
			this.Period = period;
			this.State = state;
			this.UtcCreationTime = utcCreationTime;			
		}

		internal ActorReminderData(ActorId actorId, IActorReminder reminder, DateTime utcCreationTime)
		{
			this.ActorId = actorId;
			this.Name = reminder.Name;
			this.DueTime = reminder.DueTime;
			this.Period = reminder.Period;
			this.State = reminder.State;
			this.UtcCreationTime = utcCreationTime;
		}
	}
}