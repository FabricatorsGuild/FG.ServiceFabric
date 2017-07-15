// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    /// <summary>
    /// Wrapper class for the static ServiceProxy.
    /// </summary>
    public class MockServiceProxyFactory : IServiceProxyFactory
    {
		private object _lock = new object();

        private readonly MockFabricRuntime _fabricRuntime;
		private readonly IDictionary<Type, IDictionary<Guid, IReliableStateManagerReplica>> _serviceStateManagers;

		//private readonly IList<IMockableServiceRegistration> _serviceRegistrations;
		private readonly IDictionary<string, object> _serviceProxies;


		public MockServiceProxyFactory(MockFabricRuntime fabricRuntime)
        {
            _fabricRuntime = fabricRuntime;

			_serviceRegistrations = new List<IMockableServiceRegistration>();
			_serviceProxies = new Dictionary<string, object>();
			_serviceStateManagers = new Dictionary<Type, IDictionary<Guid, IReliableStateManagerReplica>>();
        }
		
		public void AddServiceRegistration(IMockableServiceRegistration serviceRegistration)
		{
			_serviceRegistrations.Add(serviceRegistration);
		}

	    private IReliableStateManagerReplica GetServiceStateManager(IMockableServiceRegistration serviceRegistration, ServicePartitionKey partitionKey)
		{
			var serviceImplementationType = serviceRegistration.ImplementationType;

			var partitionId = serviceRegistration.ServiceDefinition.GetPartion(partitionKey);
			if (_serviceStateManagers.ContainsKey(serviceImplementationType))
			{
				var stateProvidersForPartition = _serviceStateManagers[serviceImplementationType];
				if (stateProvidersForPartition.ContainsKey(partitionId))
				{
					return stateProvidersForPartition[partitionId];
				}
			}
			return null;
		}

		private IReliableStateManagerReplica GetOrCreateServiceStateManager(
			IMockableServiceRegistration serviceRegistration,
			Func<IReliableStateManagerReplica> createStateManager,
			ServicePartitionKey partitionKey)
		{
			var serviceImplementationType = serviceRegistration.ImplementationType;

			var serviceStateManager = GetServiceStateManager(serviceRegistration, partitionKey);
			if (serviceStateManager != null) return serviceStateManager;

			serviceStateManager = createStateManager();			

			var partitionId = serviceRegistration.ServiceDefinition.GetPartion(partitionKey);
			IDictionary<Guid, IReliableStateManagerReplica> stateProvidersForPartition = null;
			if (_serviceStateManagers.ContainsKey(serviceImplementationType))
			{
				stateProvidersForPartition = _serviceStateManagers[serviceImplementationType];
			}
			else
			{
				stateProvidersForPartition = new Dictionary<Guid, IReliableStateManagerReplica>();
				_serviceStateManagers.Add(serviceImplementationType, stateProvidersForPartition);
			}

			if (stateProvidersForPartition.ContainsKey(partitionId))
			{
				return stateProvidersForPartition[partitionId];
			}

			stateProvidersForPartition.Add(partitionId, serviceStateManager);
			return serviceStateManager;
		}

		private StatefulService GetMockService(
			StatefulServiceContext serviceContext,
			ServiceTypeInformation serviceTypeInformation,
			IReliableStateManagerReplica stateManager)
		{
			return new MockStatefulService(
					codePackageActivationContext: _fabricRuntime.CodePackageContext,
					serviceProxyFactory: _fabricRuntime.ServiceProxyFactory,
					nodeContext: _fabricRuntime.BuildNodeContext(),
					statefulServiceContext: serviceContext,
					serviceTypeInfo: serviceTypeInformation,
					stateManager: stateManager);
		}

	    private string GetStatelessServiceInstanceKey(Uri serviceUri, long instanceId)
	    {
		    return $"{serviceUri}?instanceId={instanceId}";
	    }

	    private string GetStatefulServicePartitionKey(Uri serviceUri, Guid partitionId)
	    {
			return $"{serviceUri}?partitionId={partitionId}";
		}

		private TServiceInterface CreateStatelessService<TServiceInterface>(IMockableServiceRegistration serviceRegistration, ServicePartitionKey partitionKey) where TServiceInterface : IService
		{
			if (serviceRegistration == null)
			{
				throw new ArgumentException(
					$"Expected a MockableServiceRegistration with ServiceType for the type {typeof(TServiceInterface).Name}");
			}

			lock (_lock)
			{
				var serviceName = serviceRegistration.ImplementationType.Name;
				var serviceUri = _fabricRuntime.ApplicationUriBuilder.Build(serviceName).ToUri();

				var availableInstances = serviceRegistration.ServiceDefinition.Instances.Count();
				var randomInstance = Environment.TickCount % availableInstances;
				var instance = serviceRegistration.ServiceDefinition.Instances.ElementAt(randomInstance);

				var instanceKey = GetStatelessServiceInstanceKey(serviceUri, instance.Id);

				if (_serviceProxies.ContainsKey(instanceKey))
				{
					return (TServiceInterface) _serviceProxies[instanceKey];
				}

				var createStateProvider = serviceRegistration.CreateStateManager ??
				                          (() => (IReliableStateManagerReplica) new MockReliableStateManager(_fabricRuntime));

				var stateManager = GetOrCreateServiceStateManager(
					serviceRegistration,
					() => createStateProvider(),
					partitionKey);

				var serviceTypeInformation = ServiceTypeInformation.Get(serviceRegistration.ImplementationType);
				var statefulServiceContext = _fabricRuntime.BuildStatefulServiceContext(serviceName);
				var serviceFactory = serviceRegistration.CreateStatefulService ?? GetMockService;

				var statefulService = serviceFactory(statefulServiceContext, serviceTypeInformation, stateManager);
				if (statefulService is FG.ServiceFabric.Services.Runtime.StatefulService)
				{
					statefulService.SetPrivateField("_serviceProxyFactory", _fabricRuntime.ServiceProxyFactory);
					statefulService.SetPrivateField("_actorProxyFactory", _fabricRuntime.ActorProxyFactory);
					statefulService.SetPrivateField("_applicationUriBuilder", _fabricRuntime.ApplicationUriBuilder);
				}

				_serviceProxies.Add(instanceKey, statefulService);

				return (TServiceInterface) (object) statefulService;
			}
		}

		private TServiceInterface CreateStatefulService<TServiceInterface>(IMockableServiceRegistration serviceRegistration, ServicePartitionKey partitionKey) where TServiceInterface : IService
		{
			if (serviceRegistration == null)
			{
				throw new ArgumentException(
					$"Expected a MockableServiceRegistration with ServiceType for the type {typeof(TServiceInterface).Name}");
			}

			lock (_lock)
			{
				var serviceName = serviceRegistration.ImplementationType.Name;
				var serviceUri = _fabricRuntime.ApplicationUriBuilder.Build(serviceName).ToUri();
				var partionId = serviceRegistration.ServiceDefinition.GetPartion(partitionKey);
				var serviceKey = GetStatefulServicePartitionKey(serviceUri, partionId);

				if (_serviceProxies.ContainsKey(serviceKey))
				{
					return (TServiceInterface) _serviceProxies[serviceKey];
				}

				var createStateProvider = serviceRegistration.CreateStateManager ??
				                          (() => (IReliableStateManagerReplica) new MockReliableStateManager(_fabricRuntime));

				var stateManager = GetOrCreateServiceStateManager(
					serviceRegistration,
					() => createStateProvider(),
					partitionKey);

				var serviceTypeInformation = ServiceTypeInformation.Get(serviceRegistration.ImplementationType);
				var statefulServiceContext = _fabricRuntime.BuildStatefulServiceContext(serviceName);
				var serviceFactory = serviceRegistration.CreateStatefulService ?? GetMockService;

				var statefulService = serviceFactory(statefulServiceContext, serviceTypeInformation, stateManager);
				if (statefulService is FG.ServiceFabric.Services.Runtime.StatefulService)
				{
					statefulService.SetPrivateField("_serviceProxyFactory", _fabricRuntime.ServiceProxyFactory);
					statefulService.SetPrivateField("_actorProxyFactory", _fabricRuntime.ActorProxyFactory);
					statefulService.SetPrivateField("_applicationUriBuilder", _fabricRuntime.ApplicationUriBuilder);
				}

				_serviceProxies.Add(serviceKey, statefulService);

				return (TServiceInterface) (object) statefulService;
			}
		}		

	    public TServiceInterface CreateServiceProxy<TServiceInterface>(Uri serviceUri, ServicePartitionKey partitionKey = null,
		    TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null) where TServiceInterface : IService
	    {
			var statefulServiceRegistration = _serviceRegistrations.FirstOrDefault(
				registration => typeof(StatefulService).IsAssignableFrom(registration.ImplementationType));
			var statelessServiceRegistration = _serviceRegistrations.FirstOrDefault(
				registration => typeof(StatelessService).IsAssignableFrom(registration.ImplementationType));

			if (statefulServiceRegistration == null && statelessServiceRegistration == null)
			{
				throw new ArgumentException(
					$"Expected a MockableServiceRegistration with ServiceType for the type {typeof(TServiceInterface).Name}");
			}

			if (statefulServiceRegistration != null)
			{
				return CreateStatefulService<TServiceInterface>(statefulServiceRegistration, partitionKey);
			}
			return CreateStatelessService<TServiceInterface>(statelessServiceRegistration, partitionKey);
		}
    }

    
}