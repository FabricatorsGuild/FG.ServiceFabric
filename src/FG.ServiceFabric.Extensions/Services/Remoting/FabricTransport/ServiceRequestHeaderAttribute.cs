using System;
using System.Collections.Generic;
using System.Fabric;
using System.Reflection;
using FG.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = true, AllowMultiple = true)]
    public class ServiceRequestHeaderAttribute : Attribute
    {
        public Type HeaderType { get; private set; }

        public ServiceRequestHeaderAttribute(Type headerType)
        {
            HeaderType = headerType;
        }

        public static IEnumerable<ServiceRequestHeader> GetServiceRequestHeader(ServiceContext serviceContext, IService service)
        {
            var types = ServiceTypeInformation.Get(service.GetType()).InterfaceTypes;
            if (types != null)
            {
                foreach (var type in types)
                {
                    var customAttributes = type.Assembly.GetCustomAttributes<ServiceRequestHeaderAttribute>();
                    if (customAttributes != null)
                    {
                        foreach (var customAttribute in customAttributes)
                        {
                            yield return (ServiceRequestHeader) Activator.CreateInstance(customAttribute.HeaderType);
                        }
                    }
                }
            }
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != (Assembly) null)
            {
                var customAttributes = entryAssembly.GetCustomAttributes<ServiceRequestHeaderAttribute>();
                if (customAttributes != null)
                {
                    foreach (var customAttribute in customAttributes)
                    {
                        yield return (ServiceRequestHeader) Activator.CreateInstance(customAttribute.HeaderType);
                    }
                }
            }
        }
    }
}