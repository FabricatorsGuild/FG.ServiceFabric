using System;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Diagnostics
{
    public abstract class ActorOrActorServiceDescription
    {
        public abstract Type ActorType { get; }

        public abstract string ActorId { get; }

        public abstract string ApplicationTypeName { get; }
        public abstract string ApplicationName { get; }
        public abstract string ServiceTypeName { get; }
        public abstract string ServiceName { get; }
        public abstract Guid PartitionId { get; }
        public abstract long ReplicaOrInstanceId { get; }
        public abstract string NodeName { get; }

        public static implicit operator ActorOrActorServiceDescription(Actor actor)
        {
            return new ActorDescription(actor);
        }

        public static implicit operator ActorOrActorServiceDescription(ActorService actorService)
        {
            return new ActorServiceDescription(actorService);
        }
    }
}