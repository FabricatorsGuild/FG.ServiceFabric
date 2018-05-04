namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using FG.Common.Utils;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;

    using Serpent.InterfaceProxy.Extensions;
    using Serpent.InterfaceProxy.Implementations.ProxyTypeBuilder;

    using ActorBase = FG.ServiceFabric.Actors.Runtime.ActorBase;

    public class BaseActorProxy<TInterface> : BaseMethodProxyWithMethodNames<TInterface, IActorProxy>
        where TInterface : IActor
    {
        private static readonly Func<TInterface, ActorMethodContext, Task> OnPreActorMethodAsync;

        private static readonly Func<TInterface, ActorMethodContext, Task> OnPostActorMethodAsync;

        // ReSharper disable once StaticMemberInGenericType - By design. One dictionary per type will exist
        private static readonly IReadOnlyDictionary<string, ActorMethodContext> ActorMethodContextCache;

        static BaseActorProxy()
        {
            var actorType = typeof(TInterface);

            if (actorType.Is<ActorBase>())
            {
                // ReSharper disable PossibleNullReferenceException - the actor will always be of type ActorBase
                OnPreActorMethodAsync = (actor, context) => (actor as ActorBase).MockRuntimeHelperInstance.OnPreActorMethodAsync(context);
                OnPostActorMethodAsync = async (actor, context) =>
                    {
                        await (actor as ActorBase).MockRuntimeHelperInstance.OnPostActorMethodAsync(context);
                        await (actor as Actor).StateManager.SaveStateAsync(CancellationToken.None);
                    };

            }
            else
            {
                OnPreActorMethodAsync = (actor, context) => (Task)actor.CallPrivateMethod("OnPreActorMethodAsync", context);
                OnPostActorMethodAsync = async (actor, context) =>
                    {
                        await (Task)actor.CallPrivateMethod("OnPostActorMethodAsync", context);
                        await (actor as Actor).StateManager.SaveStateAsync(CancellationToken.None);
                    };
            }

            ActorMethodContextCache = actorType.GetMethods().Select(mi => mi.Name).Distinct().ToDictionary(name => name, name => ReflectionUtils.ActivateInternalCtor<ActorMethodContext>(name, ActorCallType.ActorInterfaceMethod));
        }

        public BaseActorProxy(TInterface actorInterface, IActorProxy actorProxy)
            : base(actorInterface, actorProxy)
        {
        }

        protected override async Task ExecuteAsync(string methodName, Func<TInterface, Task> func)
        {
            var actorMethodContext = ActorMethodContextCache[methodName];

            await OnPreActorMethodAsync(this.InnerType1Reference, actorMethodContext);

            try
            {
                await func(this.InnerType1Reference);
            }
            finally
            {
                await OnPostActorMethodAsync(this.InnerType1Reference, actorMethodContext);
            }
        }

        protected override async Task<TResult> ExecuteAsync<TResult>(string methodName, Func<TInterface, Task<TResult>> func)
        {
            var actorMethodContext = ActorMethodContextCache[methodName];

            await OnPreActorMethodAsync(this.InnerType1Reference, actorMethodContext);

            try
            {
                return await func(this.InnerType1Reference);
            }
            finally
            {
                await OnPostActorMethodAsync(this.InnerType1Reference, actorMethodContext);
            }
        }

        protected override async Task ExecuteAsync<TParameter>(string methodName, TParameter parameter, Func<TParameter, TInterface, Task> func)
        {
            var actorMethodContext = ActorMethodContextCache[methodName];

            await OnPreActorMethodAsync(this.InnerType1Reference, actorMethodContext);

            try
            {
                await func(parameter, this.InnerType1Reference);
            }
            finally
            {
                await OnPostActorMethodAsync(this.InnerType1Reference, actorMethodContext);
            }
        }

        protected override async Task<TResult> ExecuteAsync<TParameter, TResult>(string methodName, TParameter parameter, Func<TParameter, TInterface, Task<TResult>> func)
        {
            var actorMethodContext = ActorMethodContextCache[methodName];

            await OnPreActorMethodAsync(this.InnerType1Reference, actorMethodContext);

            try
            {
                return await func(parameter, this.InnerType1Reference);
            }
            finally
            {
                await OnPostActorMethodAsync(this.InnerType1Reference, actorMethodContext);
            }
        }
    }
}