using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using FG.Common.Utils;
using FG.ServiceFabric.Fabric;
using FG.ServiceFabric.Fabric.Runtime;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Testing.Setup;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockFabricRuntime
	{
		private readonly List<MockServiceInstance> _activeInstances;
		private readonly MockActorProxyFactory _actorProxyFactory;
		private readonly List<MockFabricApplication> _applications;
		private readonly MockPartitionEnumerationManager _partitionEnumerationManager;
		private readonly MockServiceProxyFactory _serviceProxyFactory;

		public MockFabricRuntime()
		{
			var serviceProxyFactory = new MockServiceProxyFactory(this);
			var actorProxyFactory = new MockActorProxyFactory(this);

			_serviceProxyFactory = serviceProxyFactory;
			_actorProxyFactory = actorProxyFactory;

			FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory.SetInnerFactory((factory, serviceType) =>
				_serviceProxyFactory);
			FG.ServiceFabric.Actors.Client.ActorProxyFactory.SetInnerFactory((factory, serviceType, actorType) =>
				_actorProxyFactory);

			_partitionEnumerationManager = new MockPartitionEnumerationManager(this);

			_applications = new List<MockFabricApplication>();
			_activeInstances = new List<MockServiceInstance>();

			CancellationTokenSource = new CancellationTokenSource();
			CancellationToken = CancellationTokenSource.Token;
		}

		public CancellationToken CancellationToken { get; private set; }
		public CancellationTokenSource CancellationTokenSource { get; private set; }

		public static MockFabricRuntime Current
		{
			get => (MockFabricRuntime) FabricRuntimeContextWrapper.Current?[FabricRuntimeContextKeys.CurrentMockFabricRuntime];
			set => FabricRuntimeContextWrapper.Current[FabricRuntimeContextKeys.CurrentMockFabricRuntime] = value;
		}


		public bool DisableMethodCallOutput { get; set; }

		internal IEnumerable<MockServiceInstance> Instances => _activeInstances;

		public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory;
		public IActorProxyFactory ActorProxyFactory => _actorProxyFactory;
		public IPartitionEnumerationManager PartitionEnumerationManager => _partitionEnumerationManager;

		internal ICodePackageActivationContext GetCodePackageContext(string applicationName, IServiceManifest serviceManifest, IServiceConfig serviceConfig)
		{
			return new MockCodePackageActivationContext(
				applicationName: $"fabric:/{applicationName}",
				applicationTypeName: $"{applicationName}Type",
				codePackageName: "Code",
				codePackageVersion: "1.0.0.0",
				context: Guid.NewGuid().ToString(),
				logDirectory: @"C:\Log",
				tempDirectory: @"C:\Temp",
				workDirectory: @"C:\Work",
				serviceManifest: serviceManifest,
				serviceConfig: serviceConfig
			);
		}

		internal ApplicationUriBuilder GetApplicationUriBuilder(string applicationName)
		{
			return new ApplicationUriBuilder(GetCodePackageContext(applicationName, ServiceManifest.DefaultFor("no-service"), null));
		}

		internal void RegisterInstances(IEnumerable<MockServiceInstance> instances)
		{
			var mockServiceInstances = instances.ToArray();
			foreach (var instance in mockServiceInstances)
			{
				if (_activeInstances.Any(i => i.ServiceUri.Equals(instance.ServiceUri)))
				{
					throw new MockFabricSetupException($"Trying to register services with {instance.ServiceUri}, but MockFabricRuntime already has a registration for that ServiceUri");
				}
			}

			_activeInstances.AddRange(mockServiceInstances);
		}

		public IEnumerable<IMockServiceInstance> GetInstances()
		{
			return _activeInstances.Select(i => i as IMockServiceInstance).ToArray();
		}

		internal NodeContext BuildNodeContext()
		{
			return new NodeContext("NODE_1", new NodeId(1L, 5L), 1L, "NODE_TYPE_1", "10.0.0.1");
		}

		internal StatefulServiceContext BuildStatefulServiceContext(
			string applicationName, 
			string serviceName,
			ServicePartitionInformation partitionInformation, 
			long replicaId, 
			IServiceManifest serviceManifest,
			IServiceConfig serviceConfig)
		{
			return new StatefulServiceContext(
				new NodeContext(
					"Node0",
					nodeId: new NodeId(0, 1),
					nodeInstanceId: 0,
					nodeType: "NodeType1",
					ipAddressOrFQDN: "TEST.MACHINE"),
				codePackageActivationContext: GetCodePackageContext(applicationName, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault()),
				serviceTypeName: $"{serviceName}Type",
				serviceName: new Uri($"fabric:/{applicationName}/{serviceName}"),
				initializationData: null,
				partitionId: partitionInformation.Id,
				replicaId: replicaId
			);
		}

		internal StatelessServiceContext BuildStatelessServiceContext(
			string applicationName, 
			string serviceName,
			IServiceManifest serviceManifest,
			IServiceConfig serviceConfig)
		{
			return new StatelessServiceContext(
				new NodeContext(
					"Node0",
					nodeId: new NodeId(0, 1),
					nodeInstanceId: 0,
					nodeType: "NodeType1",
					ipAddressOrFQDN: "TEST.MACHINE"),
				codePackageActivationContext: GetCodePackageContext(applicationName, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault()),
				serviceTypeName: serviceName,
				serviceName: new Uri($"fabric:/{applicationName}/{serviceName}"),
				initializationData: null,
				partitionId: Guid.NewGuid(),
				instanceId: 1L
			);
		}

		public MockFabricApplication GetApplication(string applicationInstanceName)
		{
			var existingApplication = _applications.FirstOrDefault(a =>
				a.ApplicationInstanceName.Equals(applicationInstanceName, StringComparison.InvariantCulture));
			if (existingApplication == null)
			{
				throw new ArgumentException(
					$"An application with name {applicationInstanceName} could not be found this runtime, register one with {nameof(RegisterApplication)}");
			}

			return existingApplication;
		}

		public MockFabricApplication RegisterApplication(string applicationInstanceName)
		{
			var existingApplication = _applications.FirstOrDefault(a =>
				a.ApplicationInstanceName.Equals(applicationInstanceName, StringComparison.InvariantCulture));
			if (existingApplication != null)
			{
				throw new ArgumentException(
					$"An application with name {applicationInstanceName} is already registered in this runtime");
			}

			var mockFabricApplication = new MockFabricApplication(this, applicationInstanceName);

			_applications.Add(mockFabricApplication);
			return mockFabricApplication;
		}

		private static class FabricRuntimeContextKeys
		{
			public const string CurrentMockFabricRuntime = "mockFabricRuntime";
		}
	}
}