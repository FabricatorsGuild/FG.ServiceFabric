using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
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
                var types = new List<Type>() { actorService.ActorTypeInformation.ImplementationType };
                types.AddRange(actorService.ActorTypeInformation.InterfaceTypes);
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        var customAttribute = type.Assembly.GetCustomAttribute<ActorServiceRemotingDispatcherAttribute>();
                        if (customAttribute != null)
                        {
                            return (IServiceRemotingMessageHandler)Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, new object[] {actorService});
                        }
                    }
                }
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != (Assembly) null)
                {
                    var customAttribute = entryAssembly.GetCustomAttribute<ActorServiceRemotingDispatcherAttribute>();
                    if (customAttribute != null)
                    {
                        return
                            (IServiceRemotingMessageHandler)
                            Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, new object[] { actorService });
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
                // TODO: Should probably log this.
            }
            return new FG.ServiceFabric.Actors.Remoting.Runtime.ActorServiceRemotingDispatcher(actorService);
        }
    }
}