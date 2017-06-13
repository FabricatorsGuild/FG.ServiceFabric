using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Services.Remoting.Runtime.Client
{
    public class AuditedServiceProxyFactory : ServiceProxyFactory
    {
        protected override IServiceRemotingClientFactory CreateServiceRemotingClientFactory(IServiceRemotingCallbackClient callbackClient)
        {
            var factory = base.CreateServiceRemotingClientFactory(callbackClient);
            return factory;
        }
    }
}