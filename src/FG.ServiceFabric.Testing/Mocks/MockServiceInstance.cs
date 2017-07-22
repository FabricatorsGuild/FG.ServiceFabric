using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using StatefulService = Microsoft.ServiceFabric.Services.Runtime.StatefulService;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace FG.ServiceFabric.Testing.Mocks
{
	internal class MockStatelessServiceInstance : MockServiceInstance
	{
		private StatelessService GetMockStatelessService(
			StatelessServiceContext serviceContext)
		{
			return new MockStatelessService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statelessServiceContext: serviceContext);
		}

		protected override void Build()
		{
			var isStateless = (!ServiceRegistration?.IsStateful) ?? false;

			if (!isStateless)
			{
				base.Build();
				return;
			}


			var statelessServiceContext = FabricRuntime.BuildStatelessServiceContext(ServiceRegistration.Name);
			var serviceFactory = ServiceRegistration.CreateStatelessService ?? GetMockStatelessService;
			// TODO: consider this further, is it really what should be done???

			var statelessService = serviceFactory(statelessServiceContext);
			if (statelessService is FG.ServiceFabric.Services.Runtime.StatelessService)
			{
				statelessService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
				statelessService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
				statelessService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
			}

			ServiceInstance = statelessService;
		}
	}

	internal class MockStatefulServiceInstance : MockServiceInstance
	{
		public IReliableStateManagerReplica StateManager { get; private set; }

		private StatefulService GetMockStatefulService(
			StatefulServiceContext serviceContext,
			IReliableStateManagerReplica stateManager)
		{
			return new MockStatefulService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statefulServiceContext: serviceContext,
				stateManager: stateManager);
		}

		protected override void Build()
		{
			var isStatefull = ServiceRegistration?.IsStateful ?? false;

			if (!isStatefull)
			{
				base.Build();
				return;
			}

			var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(ServiceRegistration.Name);
			StateManager = (ServiceRegistration.CreateStateManager ??
								(() => (IReliableStateManagerReplica)new MockReliableStateManager(FabricRuntime))).Invoke();
			var serviceFactory = ServiceRegistration.CreateStatefulService ?? GetMockStatefulService;
			// TODO: consider this further, is it really what should be done???

			var statefulService = serviceFactory(statefulServiceContext, StateManager);
			if (statefulService is FG.ServiceFabric.Services.Runtime.StatefulService)
			{
				statefulService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
				statefulService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
				statefulService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
			}

			ServiceInstance = statefulService;
		}		
	}

	internal class MockActorServiceInstance : MockServiceInstance
	{
		public IActorStateProvider ActorStateProvider { get; private set; }

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

		protected override void Build()
		{
			var isActorService = ActorRegistration != null;

			if (!isActorService)
			{
				base.Build();
				return;
			}

			var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(ActorRegistration.ServiceRegistration.Name);
			ActorStateProvider = (ActorRegistration.CreateActorStateProvider ??
			                          (() => (IActorStateProvider) new MockActorStateProvider(FabricRuntime))).Invoke();

			var actorTypeInformation = ActorTypeInformation.Get(ActorRegistration.ImplementationType);
			var stateManagerFactory = ActorRegistration.CreateActorStateManager != null
				? (Func<ActorBase, IActorStateProvider, IActorStateManager>) (
					(actor, stateProvider) => ActorRegistration.CreateActorStateManager(actor, stateProvider))
				: null;
			var actorServiceFactory = ActorRegistration.CreateActorService ?? GetMockActorService;
			// TODO: consider this further, is it really what should be done???

			var actorService = actorServiceFactory(statefulServiceContext, actorTypeInformation, ActorStateProvider, stateManagerFactory);
			if (actorService is FG.ServiceFabric.Actors.Runtime.ActorService)
			{
				actorService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
				actorService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
				actorService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
			}

			ServiceInstance = actorService;
		}

		internal override bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
		{
			if (ActorRegistration == null) return false;

			var partitionId = ActorRegistration?.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

			return ActorRegistration.InterfaceType == actorInterfaceType &&
				   Partition.PartitionInformation.Id == partitionId;
		}

		internal override bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
		{
			var partitionId = ActorRegistration.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

			return serviceUri.Equals(this.ServiceUri) &&
				   ActorRegistration.ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType) &&
				   Partition.PartitionInformation.Id == partitionId;
		}
	}

	internal class MockServiceInstance
	{
		public MockFabricRuntime FabricRuntime { get; private set; }

		public Uri ServiceUri { get; private set; }
		public Partition Partition { get; private set; }
		public Replica Replica { get; private set; }

		public IMockableServiceRegistration ServiceRegistration { get; private set; }
		public IMockableActorRegistration ActorRegistration { get; private set; }

		public object ServiceInstance { get; set; }

		internal virtual bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
		{
			if (ServiceRegistration == null) return false;

			var partitionId = ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

			return serviceUri.Equals(this.ServiceUri) &&
			       ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType) &&
			       Partition.PartitionInformation.Id == partitionId;
		}

		internal virtual bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
		{
			return false;
		}


		protected virtual void Build()
		{
			throw new NotSupportedException("Could not determine the type of service instance");
		}

		internal void Run()
		{
			if (ServiceRegistration.IsStateful)
			{

			}
			else
			{

			}
		}


		public static IEnumerable<MockServiceInstance> Build(
			MockFabricRuntime fabricRuntime,
			IMockableActorRegistration actorRegistration
		)
		{
			var instances = new List<MockServiceInstance>();
			foreach (var replica in actorRegistration.ServiceRegistration.ServiceDefinition.Instances)
			{
				foreach (var partition in actorRegistration.ServiceRegistration.ServiceDefinition.Partitions)
				{
					var instance = new MockActorServiceInstance()
					{						
						ActorRegistration = actorRegistration,
						FabricRuntime = fabricRuntime,
						Partition = partition,
						Replica = replica,
						ServiceUri = actorRegistration.ServiceRegistration.ServiceUri
					};
					instance.Build();
					instances.Add(instance);
				}
			}

			return instances;
		}

		public static IEnumerable<MockServiceInstance> Build(
			MockFabricRuntime fabricRuntime,
			IMockableServiceRegistration serviceRegistration
		)
		{
			var instances = new List<MockServiceInstance>();
			foreach (var replica in serviceRegistration.ServiceDefinition.Instances)
			{
				foreach (var partition in serviceRegistration.ServiceDefinition.Partitions)
				{
					MockServiceInstance instance = null;
					if (serviceRegistration.IsStateful)
					{
						instance = new MockStatefulServiceInstance()
						{
							ServiceRegistration = serviceRegistration,
							FabricRuntime = fabricRuntime,
							Partition = partition,
							Replica = replica,
							ServiceUri = serviceRegistration.ServiceUri
						};
					}
					else
					{
						instance = new MockStatelessServiceInstance()
						{
							ServiceRegistration = serviceRegistration,
							FabricRuntime = fabricRuntime,
							Partition = partition,
							Replica = replica,
							ServiceUri = serviceRegistration.ServiceUri
						};
					}
					
					instance.Build();
					instances.Add(instance);
				}
			}

			return instances;
		}
	}
}