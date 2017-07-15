using System;
using System.Fabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{

	public class MockableServiceRegistration : IMockableServiceRegistration
	{
		public MockableServiceRegistration(
			Type interfaceType,
			Type implementationType,
			CreateStatefulService createStatefulService = null,
			CreateStatelessService createStatelessService = null,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null,
			bool isStateful = false,
			Uri serviceUri = null,
			string serviceName= null)
		{
			InterfaceType = interfaceType;
			ImplementationType = implementationType;
			CreateStatefulService = createStatefulService;
			CreateStatelessService = createStatelessService;
			CreateStateManager = createStateManager;
			IsStateful = isStateful;
			ServiceUri = serviceUri;
			ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
			Name = serviceName ?? implementationType.Name;
		}

		public Type InterfaceType { get; }
		public Type ImplementationType { get; }
		
		public CreateStateManager CreateStateManager { get; }
		public MockServiceDefinition ServiceDefinition { get; set; }
		public CreateStatefulService CreateStatefulService { get; }
		public CreateStatelessService CreateStatelessService { get; }
		public bool IsStateful { get; }
		public Uri ServiceUri { get; set; }
		public string Name { get; set; }
	}
	
}