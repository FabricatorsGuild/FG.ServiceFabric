using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using StatefulService = Microsoft.ServiceFabric.Services.Runtime.StatefulService;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace FG.ServiceFabric.Testing.Mocks
{
	internal class MockServiceInstance
	{
		public MockFabricRuntime FabricRuntime { get; set; }

		public Uri ServiceUri { get; set; }
		public Partition Partition { get; set; }
		public Replica Replica { get; set; }

		public IMockableServiceRegistration ServiceRegistration { get; set; }
		public IMockableActorRegistration ActorRegistration { get; set; }

		public object ServiceInstance { get; set; }

		private void Build()
		{
			var serviceTypeInformation = ServiceTypeInformation.Get(ServiceRegistration.ImplementationType);

			if (ServiceRegistration.IsStateful)
			{
				var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(ServiceRegistration.Name);
				var stateManager = (ServiceRegistration.CreateStateManager ??
									(() => (IReliableStateManagerReplica)new MockReliableStateManager(FabricRuntime))).Invoke();
				var serviceFactory = ServiceRegistration.CreateStatefulService ?? GetMockStatefulService;
				// TODO: consider this further, is it really what should be done???

				var statefulService = serviceFactory(statefulServiceContext, serviceTypeInformation, stateManager);
				if (statefulService is FG.ServiceFabric.Services.Runtime.StatefulService)
				{
					statefulService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
					statefulService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
					statefulService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
				}

				ServiceInstance = statefulService;
			}
			else
			{
				var statelessServiceContext = FabricRuntime.BuildStatelessServiceContext(ServiceRegistration.Name);
				var serviceFactory = ServiceRegistration.CreateStatelessService ?? GetMockStatelessService;
				// TODO: consider this further, is it really what should be done???

				var statelessService = serviceFactory(statelessServiceContext, serviceTypeInformation);
				if (statelessService is FG.ServiceFabric.Services.Runtime.StatelessService)
				{
					statelessService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
					statelessService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
					statelessService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
				}

				ServiceInstance = statelessService;
			}

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

		private StatelessService GetMockStatelessService(
			StatelessServiceContext serviceContext,
			ServiceTypeInformation serviceTypeInformation)
		{
			return new MockStatelessService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statelessServiceContext: serviceContext,
				serviceTypeInfo: serviceTypeInformation);
		}

		private StatefulService GetMockStatefulService(
			StatefulServiceContext serviceContext,
			ServiceTypeInformation serviceTypeInformation,
			IReliableStateManagerReplica stateManager)
		{
			return new MockStatefulService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statefulServiceContext: serviceContext,
				serviceTypeInfo: serviceTypeInformation,
				stateManager: stateManager);
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
					var instance = new MockServiceInstance()
					{
						ServiceRegistration = serviceRegistration,
						FabricRuntime = fabricRuntime,
						Partition = partition,
						Replica = replica,
						ServiceUri = serviceRegistration.ServiceUri
					};
					instance.Build();
					instances.Add(instance);
				}
			}

			return instances;
		}
	}
}