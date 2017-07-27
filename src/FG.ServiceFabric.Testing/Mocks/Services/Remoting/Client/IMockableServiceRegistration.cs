using System;
using System.Fabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
	public interface IMockableServiceRegistration
	{
		bool IsStateful { get; }
		Type[] InterfaceTypes { get; }
		Type ImplementationType { get; }
		CreateStatefulService CreateStatefulService { get; }
		CreateStatelessService CreateStatelessService { get; }
		CreateStateManager CreateStateManager { get; }
		MockServiceDefinition ServiceDefinition { get; set; }
		Uri ServiceUri { get; set; }
		string Name { get; set; }
	}

	public delegate IReliableStateManagerReplica CreateStateManager();

	public delegate StatefulService CreateStatefulService(
		StatefulServiceContext context,
		IReliableStateManagerReplica stateManager);

	public delegate TService CreateStatefulService<out TService>(
		StatefulServiceContext context,
		IReliableStateManagerReplica stateManager)
		where TService : IService;

	public delegate StatelessService CreateStatelessService(
		StatelessServiceContext context);


	public delegate TService CreateStatelessService<out TService>(
		StatelessServiceContext context)
		where TService : IService;
}