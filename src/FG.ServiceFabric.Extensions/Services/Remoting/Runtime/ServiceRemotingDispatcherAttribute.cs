using System;
using System.Fabric;
using System.Reflection;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ServiceRemotingDispatcherAttribute : Attribute
    {
        public ServiceRemotingDispatcherAttribute(Type serviceRemotingDispatcherType)
        {
            ServiceRemotingDispatcherType = serviceRemotingDispatcherType;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public Type ServiceRemotingDispatcherType { get; set; }

        public static IServiceRemotingMessageHandler GetServiceRemotingDispatcher(ServiceContext serviceContext, IService service)
        {
            try
            {
                var types = ServiceTypeInformation.Get(service.GetType()).InterfaceTypes;
                if (types != null)
                {
                    foreach (var type in types)
                    {
                        var customAttribute = type.Assembly.GetCustomAttribute<ServiceRemotingDispatcherAttribute>();
                        if (customAttribute != null)
                        {
                            return
                                (IServiceRemotingMessageHandler)
                                Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, new object[] {serviceContext, service});
                        }
                    }
                }
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != (Assembly) null)
                {
                    var customAttribute = entryAssembly.GetCustomAttribute<ServiceRemotingDispatcherAttribute>();
                    if (customAttribute != null)
                    {
                        return
                            (IServiceRemotingMessageHandler)
                            Activator.CreateInstance(customAttribute.ServiceRemotingDispatcherType, new object[] {serviceContext, service});
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
                // TODO: Should probably log this.
            }
            return new FG.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingDispatcher(serviceContext, service);
        }
    }
}