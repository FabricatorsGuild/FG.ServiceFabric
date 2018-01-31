using System.Fabric;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    /// <summary>
    /// Extenstion methods to create service listeners with logging
    /// </summary>
    public static class ServiceListenerExtensions
    {
        /// <summary>
        /// Creates a ServiceInstanceListener for a stateless service with embedded logging using the supplied IServiceCommunicationLogger
        /// </summary>
        /// <param name="service">The owning service exposing the listener</param>
        /// <param name="context">The context of the service</param>
        /// <param name="logger">The IServiceCommunicationLogger logging service requests and failures</param>
        /// <returns></returns>
        public static ServiceInstanceListener CreateServiceReplicaListener(this IService service, StatelessServiceContext context,
            IServiceCommunicationLogger logger)
        {
            return new ServiceInstanceListener(ctxt =>
                (IServiceRemotingListener)new FabricTransportServiceRemotingListener(
                    ctxt,
                    new ServiceRemotingDispatcher(
                        service,
                        new Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher(context,
                            service),
                        logger),
                    new FabricTransportRemotingListenerSettings
                    {
                        MaxConcurrentCalls = 1000
                    }
                ));
        }

        /// <summary>
        /// Creates a ServiceReplicaListener for a stateful service with embedded logging using the supplied IServiceCommunicationLogger
        /// </summary>
        /// <param name="service">The owning service exposing the listener</param>
        /// <param name="context">The context of the service</param>
        /// <param name="logger">The IServiceCommunicationLogger logging service requests and failures</param>
        /// <returns></returns>
        public static ServiceReplicaListener CreateServiceReplicaListener(this IService service, StatefulServiceContext context,
            IServiceCommunicationLogger logger)
        {
            return new ServiceReplicaListener(ctxt =>
                (IServiceRemotingListener) new FabricTransportServiceRemotingListener(
                    ctxt,
                    new ServiceRemotingDispatcher(
                        service,
                        new Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher(context,
                            service),
                        logger),
                    new FabricTransportRemotingListenerSettings
                    {
                        MaxConcurrentCalls = 1000
                    }
                ));
        }
    }
}