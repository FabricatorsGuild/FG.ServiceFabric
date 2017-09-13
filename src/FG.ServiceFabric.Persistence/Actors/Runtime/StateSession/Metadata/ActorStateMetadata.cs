using System.Net.Http.Headers;
using System.Runtime.Serialization;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime.StateSession.Metadata
{
	public class ActorStateValueMetadata : ValueMetadata, IActorValueMetadata
	{
		public ActorStateValueMetadata(StateWrapperType type, ActorId actorId) 
			: base(type)
		{
			ActorId = actorId;
		}
		public ActorId ActorId { get; private set; }

		public override StateWrapper<T> BuildStateWrapper<T>(string id, T value, IServiceMetadata serviceMetadata)
		{
			return new ActorStateWrapper<T>(id, value, serviceMetadata, this);
		}
	}
}