using System;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Diagnostics
{
    public class ActorDescription : ActorServiceDescription
    {
        private readonly Actor _actor;

        public ActorDescription(Actor actor) : base(actor.ActorService)
        {
            _actor = actor;
        }

        public override Type ActorType => _actor.GetType();
    }
}