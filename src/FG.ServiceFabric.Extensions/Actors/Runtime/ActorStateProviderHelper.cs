using System;
using System.Reflection;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class ActorStateProviderHelper
    {
        public static IActorStateProvider CreateDefaultStateProvider(ActorTypeInformation actorTypeInfo)
        {
            var assembly = typeof(Microsoft.ServiceFabric.Actors.Runtime.ActorService).Assembly;

            var internalActorStateProviderHelperType = assembly.GetType("Microsoft.ServiceFabric.Actors.Runtime.ActorStateProviderHelper");

            var createDefaultStateProviderMethod = internalActorStateProviderHelperType.GetMethod("CreateDefaultStateProvider", BindingFlags.NonPublic | BindingFlags.Static);
            var actorStateProvider = (IActorStateProvider)createDefaultStateProviderMethod.Invoke(null, new object[] {actorTypeInfo});

            return actorStateProvider;
        }
    }
}