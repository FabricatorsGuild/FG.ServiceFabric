using System.Fabric;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    public class MockStatelessService : StatelessService
    {
        private readonly ICodePackageActivationContext _codePackageActivationContext;
        private readonly NodeContext _nodeContext;
        private readonly IServiceProxyFactory _serviceProxyFactory;

        public MockStatelessService(
            ICodePackageActivationContext codePackageActivationContext,
            IServiceProxyFactory serviceProxyFactory,
            NodeContext nodeContext,
            StatelessServiceContext statelessServiceContext
        ) :
            base(
                statelessServiceContext
            )
        {
            _codePackageActivationContext = codePackageActivationContext;
            _serviceProxyFactory = serviceProxyFactory;

            _nodeContext = nodeContext;
        }
    }
}