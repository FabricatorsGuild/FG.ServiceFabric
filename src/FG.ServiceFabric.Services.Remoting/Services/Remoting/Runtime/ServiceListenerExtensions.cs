using System.Fabric;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    public static class ServiceListenerExtensions
    {
        public static ServiceReplicaListener CreateServiceReplicaListener(this IService service, ServiceContext context,
            IServiceCommunicationLogger logger)
        {
            return new ServiceReplicaListener(ctxt =>
                (IServiceRemotingListener)new FabricTransportServiceRemotingListener(
                    serviceContext: ctxt,
                    messageHandler: new ServiceRemotingDispatcher(
                        service: service,
                        innerMessageHandler: new Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher(context,
                            service),
                        logger: logger),
                    listenerSettings: new FabricTransportRemotingListenerSettings()
                    {
                        MaxConcurrentCalls = 1000,
                    }
                ));
        }
    }
}