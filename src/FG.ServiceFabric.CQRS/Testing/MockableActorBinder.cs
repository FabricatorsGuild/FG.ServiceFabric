using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;

namespace FG.ServiceFabric.Testing
{
	public class MockableActorBinder<T> : IReceiverActorBinder where T : IReliableMessageReceiverActor
	{
		private readonly IActorProxyFactory _actorProxyFactory;

		public MockableActorBinder(IActorProxyFactory actorProxyFactory)
		{
			_actorProxyFactory = actorProxyFactory;
		}

		public IReliableMessageReceiverActor Bind(ActorReference actorReference)
		{
			return _actorProxyFactory.CreateActorProxy<T>(actorReference.ServiceUri,
				actorReference.ActorId, actorReference.ListenerName);
		}
	}
}