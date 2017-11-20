using System;
using System.CodeDom;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
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