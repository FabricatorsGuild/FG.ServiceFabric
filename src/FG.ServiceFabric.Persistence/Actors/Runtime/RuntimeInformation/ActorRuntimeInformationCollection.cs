using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Actors.Runtime.RuntimeInformation
{
    using System.Collections.Concurrent;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Data;

    public class ActorRuntimeInformationCollection
    {
        private readonly ConcurrentDictionary<ActorId, ActorRuntimeInformation> actorRuntimeInformation = new ConcurrentDictionary<ActorId, ActorRuntimeInformation>();

        public bool ContainsActor(ActorId actorId)
        {
            return this.actorRuntimeInformation.ContainsKey(actorId);
        }

        public ActorRuntimeInformationCollection Set(ActorId actorId, Func<ActorRuntimeInformation, ActorRuntimeInformation> updateFunc)
        {
            this.actorRuntimeInformation.AddOrUpdate(
                actorId,
                aid => updateFunc(new ActorRuntimeInformation()),
                (aid, ari) => updateFunc(ari));

            return this;
        }

        public ActorRuntimeInformationCollection RemoveActor(ActorId actorId)
        {
            this.actorRuntimeInformation.TryRemove(actorId, out _);
            return this;
        }


        public ActorRuntimeInformation GetActorRuntimeInformation(ActorId actorId)
        {
            if (!this.actorRuntimeInformation.TryGetValue(actorId, out var actorRuntimeInformation))
            {
                throw new Exception("Unexpected runtime error - Actor not activated correctly but state save requested. If this exception is thrown - it's most likely a bug.");
            }

            return actorRuntimeInformation;
        }

        public ConditionalValue<ActorRuntimeInformation> TryGetActorRuntimeInformation(ActorId actorId)
        {
            if (!this.actorRuntimeInformation.TryGetValue(actorId, out var actorRuntimeInformation))
            {
                return new ConditionalValue<ActorRuntimeInformation>();
            }

            return new ConditionalValue<ActorRuntimeInformation>(true, actorRuntimeInformation);
        }
    }
}
