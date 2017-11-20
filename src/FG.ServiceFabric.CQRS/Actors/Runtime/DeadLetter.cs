using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class DeadLetter
	{
		internal DeadLetter(ActorReliableMessage reliableMessage)
		{
			ActorReference = reliableMessage.ActorReference;
			Message = reliableMessage.Message;
		}

		public ActorReference ActorReference { get; private set; }
		public ReliableMessage Message { get; private set; }
	}
}