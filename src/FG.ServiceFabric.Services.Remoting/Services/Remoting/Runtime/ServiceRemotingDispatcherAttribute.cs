using System;
using System.Fabric;
using System.Reflection;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    [AttributeUsage(AttributeTargets.Assembly)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ServiceRemotingDispatcherAttribute : Attribute
    {
        public ServiceRemotingDispatcherAttribute(Type serviceRemotingDispatcherType)
        {
            ServiceRemotingDispatcherType = serviceRemotingDispatcherType;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public Type ServiceRemotingDispatcherType { get; set; }

        public static IServiceRemotingMessageHandler GetServiceRemotingDispatcher(ServiceContext serviceContext,
            IService service)
        {
            try
            {
                var types = ServiceTypeInformation.Get(service.GetType()).InterfaceTypes;
                if (types != null)
                    foreach (var type in types)
                    {
                        var customAttribute = type.Assembly.GetCustomAttribute<ServiceRemotingDispatcherAttribute>();
                        if (customAttribute != null)
                            return
                                (IServiceRemotingMessageHandler)
                                Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, serviceContext,
                                    service);
                    }
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var customAttribute = entryAssembly.GetCustomAttribute<ServiceRemotingDispatcherAttribute>();
                    if (customAttribute != null)
                        return
                            (IServiceRemotingMessageHandler)
                            Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, serviceContext,
                                service);
                }
            }
            catch (Exception)
            {
                // Ignore
                // TODO: Should probably log this.
            }
            return new ServiceRemotingDispatcher(service,
                new Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher(serviceContext,
                    service), null);
        }
    }
}