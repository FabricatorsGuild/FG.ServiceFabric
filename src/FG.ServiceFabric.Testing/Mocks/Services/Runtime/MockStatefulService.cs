using System.Fabric;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	public class MockStatefulService : StatefulService
	{
		private readonly ICodePackageActivationContext _codePackageActivationContext;
		private readonly IServiceProxyFactory _serviceProxyFactory;
		private readonly NodeContext _nodeContext;

		public MockStatefulService(
			ICodePackageActivationContext codePackageActivationContext,
			IServiceProxyFactory serviceProxyFactory,
			NodeContext nodeContext,
			StatefulServiceContext statefulServiceContext,
			IReliableStateManagerReplica stateManager = null
		) :
			base(
				serviceContext: statefulServiceContext, 
				reliableStateManagerReplica:stateManager
			)
		{
			_codePackageActivationContext = codePackageActivationContext;
			_serviceProxyFactory = serviceProxyFactory;
			
			_nodeContext = nodeContext;
		}
	}
}