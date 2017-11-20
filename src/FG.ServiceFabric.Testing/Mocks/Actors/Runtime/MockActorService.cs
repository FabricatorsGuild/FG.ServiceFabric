using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
	public class MockActorService : ActorService
	{
		private readonly IActorProxyFactory _actorProxyFactory;
		private readonly ICodePackageActivationContext _codePackageActivationContext;
		private readonly NodeContext _nodeContext;
		private readonly IServiceProxyFactory _serviceProxyFactory;

		public MockActorService(
			ICodePackageActivationContext codePackageActivationContext,
			IServiceProxyFactory serviceProxyFactory,
			IActorProxyFactory actorProxyFactory,
			NodeContext nodeContext,
			StatefulServiceContext statefulServiceContext,
			ActorTypeInformation actorTypeInfo,
			Func<ActorService, ActorId, ActorBase> actorFactory = null,
			Func<ActorBase, IActorStateProvider,
				IActorStateManager> stateManagerFactory = null,
			IActorStateProvider stateProvider = null,
			ActorServiceSettings settings = null
		) :
			base(
				context: statefulServiceContext,
				actorTypeInfo: actorTypeInfo,
				actorFactory: actorFactory,
				stateManagerFactory: stateManagerFactory,
				stateProvider: stateProvider,
				settings: settings)
		{
			_codePackageActivationContext = codePackageActivationContext;
			_serviceProxyFactory = serviceProxyFactory;
			_actorProxyFactory = actorProxyFactory;
			_nodeContext = nodeContext;
		}
	}
}