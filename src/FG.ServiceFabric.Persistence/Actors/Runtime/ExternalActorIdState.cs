using System;
using System.CodeDom;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
	[DataContract]
	internal sealed class ActorReminderCompletedData
	{
		[DataMember]
		internal ActorId ActorId { get; private set; }

		[DataMember]
		internal string ReminderName { get; private set; }

		[DataMember]
		internal DateTime UtcTime { get; private set; }

		internal ActorReminderCompletedData(ActorId actorId, string reminderName, DateTime utcTime)
		{
			ActorId = actorId;
			ReminderName = reminderName;
			UtcTime = utcTime;
		}		
	}

	[DataContract]
	internal sealed class ActorReminderData
	{
		[DataMember]
		internal ActorId ActorId { get; private set; }

		[DataMember]
		internal string Name { get; private set; }

		[DataMember]
		internal TimeSpan DueTime { get; private set; }

		[DataMember]
		internal TimeSpan Period { get; private set; }

		[DataMember]
		internal byte[] State { get; private set; }

		[DataMember]
		internal TimeSpan LogicalCreationTime { get; private set; }

		internal ActorReminderData(ActorId actorId, string name, TimeSpan dueTime, TimeSpan period, byte[] state, TimeSpan logicalCreationTime)
		{
			this.ActorId = actorId;
			this.Name = name;
			this.DueTime = dueTime;
			this.Period = period;
			this.State = state;
			this.LogicalCreationTime = logicalCreationTime;
		}

		internal ActorReminderData(ActorId actorId, IActorReminder reminder, TimeSpan logicalCreationTime)
		{
			this.ActorId = actorId;
			this.Name = reminder.Name;
			this.DueTime = reminder.DueTime;
			this.Period = reminder.Period;
			this.State = reminder.State;
			this.LogicalCreationTime = logicalCreationTime;
		}		
	}

	public class ExternalActorReminderState
	{
		public ExternalActorReminderState()
		{

		}

		public ExternalActorReminderState(ActorId actorId, IActorReminder reminder)
		{
			ActorId = new ExternalActorIdState(actorId);
			State = reminder.State;
			Name = reminder.Name;

		}

		public ExternalActorIdState ActorId { get; set; }

		public string Name { get; set; }

		public byte[] State { get; set; }
	}

	public class ExternalActorIdState
	{
		public ExternalActorIdState()
		{
			
		}

		public ExternalActorIdState(ActorId actorId)
		{
			Kind = actorId.Kind;
			Value = actorId.ToString();
		}

		public string Value { get; set; }

		public ActorIdKind Kind { get; set; }

		public ActorId ToActorId()
		{
			switch (Kind)
			{
				case (ActorIdKind.Guid):
					return new ActorId(Guid.Parse(Value));
				case (ActorIdKind.Long):
					return new ActorId(long.Parse(Value));
				case (ActorIdKind.String):
					return new ActorId(Value);
			}

			return null;
		}		
	}
}