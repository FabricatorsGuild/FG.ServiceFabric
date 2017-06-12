using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{
    public static class ActorServiceListenerExtensions
    {
        public static ServiceReplicaListener CreateServiceReplicaListener(this ActorService actorService, IActorServiceCommunicationLogger logger)
        {
            return new ServiceReplicaListener(ctxt =>
                (IServiceRemotingListener) new FabricTransportActorServiceRemotingListener(
                    serviceContext: ctxt,
                    messageHandler: new ActorServiceRemotingDispatcher(
                        actorService: actorService,
                        innerMessageHandler: new Microsoft.ServiceFabric.Actors.Remoting.Runtime.ActorServiceRemotingDispatcher(actorService),
                        logger: logger),
                    listenerSettings: new FabricTransportRemotingListenerSettings()
                    {
                        MaxConcurrentCalls = 1000,
                    }
                ));
        }
    }
}