using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime.ActorDocument
{
    public class ActorDocumentStateKey : ActorSchemaKey
    {
        internal const string ActorDocumentStateSchemaName = @"ACTORDOC";

        public ActorDocumentStateKey(ActorId actorId) : base(ActorDocumentStateSchemaName, GetActorIdSchemaKey(actorId))
        {
        }

        public static implicit operator ActorDocumentStateKey(ActorId actorId)
        {
            return new ActorDocumentStateKey(actorId);
        }

        public static implicit operator ActorId(ActorDocumentStateKey actordocStateKey)
        {
            return ActorSchemaKey.TryGetActorIdFromSchemaKey(actordocStateKey.Key);
        }
    }
}