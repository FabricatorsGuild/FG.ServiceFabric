using System.Fabric;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	public class MockStatelessService : StatelessService
	{
		private readonly ICodePackageActivationContext _codePackageActivationContext;
		private readonly IServiceProxyFactory _serviceProxyFactory;
		private readonly NodeContext _nodeContext;

		public MockStatelessService(
			ICodePackageActivationContext codePackageActivationContext,
			IServiceProxyFactory serviceProxyFactory,
			NodeContext nodeContext,
			StatelessServiceContext statelessServiceContext
		) :
			base(
				serviceContext: statelessServiceContext
			)
		{
			_codePackageActivationContext = codePackageActivationContext;
			_serviceProxyFactory = serviceProxyFactory;

			_nodeContext = nodeContext;
		}
	}
}