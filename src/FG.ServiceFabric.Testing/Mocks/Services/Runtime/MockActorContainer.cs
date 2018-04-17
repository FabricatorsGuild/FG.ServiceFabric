namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    using System;
    using System.Collections.Concurrent;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public class MockActorContainer
    {
        private readonly ConcurrentDictionary<ActorId, object> actors = new ConcurrentDictionary<ActorId, object>();

        public bool TryAddActor(ActorId actorId, IActor actor)
        {
            return this.actors.TryAdd(actorId, actor);
        }

        public bool TryAddActor(ActorId actorId, Actor actor)
        {
            return this.actors.TryAdd(actorId, actor);
        }

        public TActorInterface GetOrAddActor<TActorInterface>(ActorId actorId, Func<ActorId, TActorInterface> addFunc)
        {
            return (TActorInterface)this.actors.GetOrAdd(actorId, aid => addFunc(aid));
        }

        public TActorInterface GetOrAddActor<TActorInterface>(ActorId actorId, Func<ActorId, IActor> addFunc)
        {
            return (TActorInterface)this.actors.GetOrAdd(actorId, addFunc);
        }

        public bool TryGetActor<TActorInterface>(ActorId actorId, out TActorInterface actorInterface)
            where TActorInterface : IActor
        {
            var result = this.actors.TryGetValue(actorId, out var actor);

            if (!result)
            {
                actorInterface = default(TActorInterface);
                return false;
            }

            actorInterface = (TActorInterface)actor;
            return false;
        }

        public bool TryGetActor(ActorId actorId, out Actor actor)
        {
            var result = this.actors.TryGetValue(actorId, out var innerActor);

            if (!result)
            {
                actor = null;
                return false;
            }

            actor = (Actor)innerActor;
            return false;
        }

    }
}