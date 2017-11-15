using System;
using System.Runtime.Serialization;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
	[DataContract]
	internal sealed class ActorReminderCompletedData
	{
		private ActorReminderCompletedData()
		{
		}

		internal ActorReminderCompletedData(ActorId actorId, string reminderName, DateTime utcTime)
		{
			ActorId = actorId;
			ReminderName = reminderName;
			UtcTime = utcTime;
		}

		[JsonProperty("actorId")]
		private string ActorIdValue
		{
			get { return StateSessionHelper.GetActorIdSchemaKey(ActorId); }
			set { ActorId = StateSessionHelper.TryGetActorIdFromSchemaKey(value); }
		}

		[JsonIgnore]
		[DataMember]
		internal ActorId ActorId { get; private set; }

		[JsonProperty("reminderName")]
		[DataMember]
		internal string ReminderName { get; private set; }

		[JsonProperty("utcTime")]
		[DataMember]
		internal DateTime UtcTime { get; private set; }
	}
}