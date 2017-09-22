using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	public enum MockActorServiceInstanceStatus
	{
		Registered = 0,
		Built = 1,
		Running = 2,
		Stopped,
	}

	internal class MockActorServiceInstance : MockServiceInstance
	{
		public MockActorServiceInstanceStatus Status { get; set; }

		public IActorStateProvider ActorStateProvider { get; private set; }

		public IDictionary<ActorId, Actor> Actors { get; private set; }

		private ActorService GetMockActorService(
			StatefulServiceContext serviceContext,
			ActorTypeInformation actorTypeInformation,
			IActorStateProvider actorStateProvider,
			Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
		{
			return (ActorService)new MockActorService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				actorProxyFactory: FabricRuntime.ActorProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statefulServiceContext: serviceContext,
				actorTypeInfo: actorTypeInformation,
				stateManagerFactory: stateManagerFactory,
				stateProvider: actorStateProvider);
		}

		private ActorService CreateActorService(
			StatefulServiceContext serviceContext,
			ActorTypeInformation actorTypeInformation,
			IActorStateProvider actorStateProvider,
			Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
		{
			var actorServiceType = ActorRegistration.ServiceRegistration.ImplementationType;

			if (actorServiceType == typeof(Microsoft.ServiceFabric.Actors.Runtime.ActorService))
			{
				return new ActorService(serviceContext, actorTypeInformation, stateProvider: actorStateProvider, stateManagerFactory: stateManagerFactory);
			}

			var constructors = actorServiceType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
			{
				var arguments = new List<object>();
				var parameters = constructor.GetParameters().ToDictionary(p => p.ParameterType, p => p);

				var canConstruct = true;

				foreach (var parameter in parameters.Values)
				{
					if (parameter.ParameterType == typeof(StatefulServiceContext))
					{
						arguments.Add(serviceContext);
					}
					else if (parameter.ParameterType == typeof(ActorTypeInformation))
					{
						arguments.Add(actorTypeInformation);
					}
					else if (parameter.ParameterType == typeof(Func<ActorService, ActorId, ActorBase>))
					{
						arguments.Add(null);
					}
					else if (parameter.ParameterType == typeof(Func<ActorBase, IActorStateProvider, IActorStateManager>))
					{
						arguments.Add(stateManagerFactory);
					}
					else if (parameter.ParameterType == typeof(IActorStateProvider))
					{
						arguments.Add(actorStateProvider);
					}
					else if (parameter.ParameterType == typeof(ActorServiceSettings))
					{
						arguments.Add(null);
					}
					else
					{
						canConstruct = false;
					}
				}

				if (canConstruct)
				{
					return (ActorService)constructor.Invoke(null, arguments.ToArray());
				}
			}

			return GetMockActorService(serviceContext, actorTypeInformation, actorStateProvider, stateManagerFactory);
		}

		protected override void Build()
		{
			var isActorService = ActorRegistration != null;

			if (!isActorService)
			{
				base.Build();
				return;
			}

			var actorTypeInformation = ActorTypeInformation.Get(ActorRegistration.ImplementationType);
			var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(ActorRegistration.ServiceRegistration.Name, this.Partition.PartitionInformation, this.Replica.Id);
			ActorStateProvider = (ActorRegistration.CreateActorStateProvider ??
			                      ((context, actorInfo) => (IActorStateProvider) new MockActorStateProvider(FabricRuntime))).Invoke(statefulServiceContext, actorTypeInformation);

			var stateManagerFactory = ActorRegistration.CreateActorStateManager != null
				? (Func<ActorBase, IActorStateProvider, IActorStateManager>) (
					(actor, stateProvider) => ActorRegistration.CreateActorStateManager(actor, stateProvider))
				: null;
			var actorServiceFactory = ActorRegistration.CreateActorService ?? CreateActorService;
			// TODO: consider this further, is it really what should be done???

			var actorService = actorServiceFactory(statefulServiceContext, actorTypeInformation, ActorStateProvider, stateManagerFactory);
			if (actorService is FG.ServiceFabric.Actors.Runtime.ActorService)
			{
				actorService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
				actorService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
				actorService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
			}
	
			ServiceInstance = actorService;

			Actors = new Dictionary<ActorId, Actor>();

			base.Build();
		}

		internal override bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
		{
			if (ActorRegistration?.ServiceRegistration.ServiceDefinition.PartitionKind != partitionKey.Kind) return false;

			var partitionId = ActorRegistration?.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

			return ActorRegistration.InterfaceType == actorInterfaceType &&
			       Partition.PartitionInformation.Id == partitionId;
		}

		internal override bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
		{
			if(ActorRegistration.ServiceRegistration.ServiceDefinition.PartitionKind != partitionKey.Kind) return false;

			var partitionId = ActorRegistration.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

			return serviceUri.ToString().Equals(this.ServiceUri.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
			       ActorRegistration.ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType) &&
			       Partition.PartitionInformation.Id == partitionId;
		}		
	}
}