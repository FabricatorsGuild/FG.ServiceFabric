using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.ServiceFabric.Actors.Remoting.V1.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1.Runtime;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport
{
    [AttributeUsage(AttributeTargets.Assembly)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ActorServiceRemotingDispatcherAttribute : Attribute
    {
        public ActorServiceRemotingDispatcherAttribute(Type serviceRemotingDispatcherType)
        {
            ServiceRemotingDispatcherType = serviceRemotingDispatcherType;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public Type ServiceRemotingDispatcherType { get; set; }

        public static IServiceRemotingMessageHandler GetServiceRemotingDispatcher(ActorService actorService)
        {
            try
            {
                var types = new List<Type> {actorService.ActorTypeInformation.ImplementationType};
                types.AddRange(actorService.ActorTypeInformation.InterfaceTypes);
                if (types != null)
                    foreach (var type in types)
                    {
                        var customAttribute =
                            type.Assembly.GetCustomAttribute<ActorServiceRemotingDispatcherAttribute>();
                        if (customAttribute != null)
                            return (IServiceRemotingMessageHandler) Activator.CreateInstance(
                                customAttribute.ServiceRemotingDispatcherType, actorService);
                    }
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var customAttribute = entryAssembly.GetCustomAttribute<ActorServiceRemotingDispatcherAttribute>();
                    if (customAttribute != null)
                        return
                            (IServiceRemotingMessageHandler)
                            Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, actorService);
                }
            }
            catch (Exception)
            {
                // Ignore
                // TODO: Should probably log this.
            }
            return new Runtime.ActorServiceRemotingDispatcher(actorService,
                new ActorServiceRemotingDispatcher(actorService), null);
        }
    }
}