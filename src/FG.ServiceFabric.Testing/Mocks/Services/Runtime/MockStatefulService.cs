using System.Fabric;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    public class MockStatefulService : StatefulService
    {
        private readonly ICodePackageActivationContext _codePackageActivationContext;
        private readonly NodeContext _nodeContext;
        private readonly IServiceProxyFactory _serviceProxyFactory;

        public MockStatefulService(
            ICodePackageActivationContext codePackageActivationContext,
            IServiceProxyFactory serviceProxyFactory,
            NodeContext nodeContext,
            StatefulServiceContext statefulServiceContext,
            IReliableStateManagerReplica2 stateManager = null
        ) :
            base(
                statefulServiceContext,
                stateManager
            )
        {
            _codePackageActivationContext = codePackageActivationContext;
            _serviceProxyFactory = serviceProxyFactory;

            _nodeContext = nodeContext;
        }
    }
}