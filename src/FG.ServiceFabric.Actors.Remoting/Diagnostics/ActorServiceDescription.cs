using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Diagnostics
{
	public class ActorServiceDescription : ActorOrActorServiceDescription
	{
		private readonly ActorService _actorService;

		public ActorServiceDescription(ActorService actorService)
		{
			_actorService = actorService;
		}

		public override Type ActorType => _actorService.ActorTypeInformation.ImplementationType;
		public override string ActorId => "";
		public override string ApplicationTypeName => _actorService.Context.CodePackageActivationContext.ApplicationTypeName;
		public override string ApplicationName => _actorService.Context.CodePackageActivationContext.ApplicationName;
		public override string ServiceTypeName => _actorService.Context.ServiceTypeName;
		public override string ServiceName => _actorService.Context.ServiceName.ToString();
		public override Guid PartitionId => _actorService.Context.PartitionId;
		public override long ReplicaOrInstanceId => _actorService.Context.ReplicaId;
		public override string NodeName => _actorService.Context.NodeContext.NodeName;
	}
}