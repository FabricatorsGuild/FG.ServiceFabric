using System.Runtime.Serialization;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime.StateSession
{
	public class ActorStateWrapper<T> : StateWrapper<T>
	{
		public ActorStateWrapper()
		{
		}
		public ActorStateWrapper(string id, T value, IServiceMetadata serviceMetadata, IActorValueMetadata actorValueMetadata)
			: base(id, value, serviceMetadata, actorValueMetadata)
		{
			ActorId = actorValueMetadata.ActorId;
		}
		[JsonProperty("actorId")]
		private string ActorIdValue
		{
			get { return StateSessionHelper.GetActorIdSchemaKey(ActorId); }
			set { ActorId = StateSessionHelper.TryGetActorIdFromSchemaKey(value); }
		}

		[JsonIgnore]
		[DataMember]
		public ActorId ActorId { get; private set; }
	}
}