using System;
using System.Fabric;

namespace FG.ServiceFabric.Services.Runtime
{
    public class ServiceUriBuilder
    {
        public ServiceUriBuilder(string serviceInstance)
        {
            ActivationContext = FabricRuntime.GetActivationContext();
            ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(ICodePackageActivationContext context, string serviceInstance)
        {
            ActivationContext = context;
            ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(ICodePackageActivationContext context, string applicationInstance,
            string serviceInstance)
        {
            ActivationContext = context;
            ApplicationInstance = applicationInstance;
            ServiceInstance = serviceInstance;
        }

        /// <summary>
        ///     The name of the application instance that contains he service.
        /// </summary>
        public string ApplicationInstance { get; }

        /// <summary>
        ///     The name of the service instance.
        /// </summary>
        public string ServiceInstance { get; }

        /// <summary>
        ///     The local activation context
        /// </summary>
        public ICodePackageActivationContext ActivationContext { get; }

        /// <summary>
        ///     Builds a Uri to the specified Service Instance
        /// </summary>
        /// <returns></returns>
        public Uri ToUri()
        {
            var applicationInstance = ApplicationInstance ?? ActivationContext.ApplicationName;
            applicationInstance = ActivationContext.ApplicationName.Replace("fabric:/", string.Empty);
            return new Uri("fabric:/" + applicationInstance + "/" + ServiceInstance);
        }

        public static implicit operator Uri(ServiceUriBuilder serviceUriBuilder)
        {
            return serviceUriBuilder.ToUri();
        }
    }
}