namespace FG.ServiceFabric.Testing.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;

    using FG.ServiceFabric.Fabric;
    using FG.ServiceFabric.Fabric.Runtime;
    using FG.ServiceFabric.Services.Remoting.Runtime.Client;
    using FG.ServiceFabric.Services.Runtime;
    using FG.ServiceFabric.Testing.Mocks.Actors.Client;
    using FG.ServiceFabric.Testing.Mocks.Fabric;
    using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
    using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
    using FG.ServiceFabric.Testing.Setup;

    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

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

            this._serviceProxyFactory = serviceProxyFactory;
            this._actorProxyFactory = actorProxyFactory;

            ServiceProxyFactoryBase.SetInnerFactory((factory, serviceType) => this._serviceProxyFactory);
            ServiceFabric.Actors.Client.ActorProxyFactory.SetInnerFactory((factory, serviceType, actorType) => this._actorProxyFactory);

            this._partitionEnumerationManager = new MockPartitionEnumerationManager(this);

            this._applications = new List<MockFabricApplication>();
            this._activeInstances = new List<MockServiceInstance>();

            this.CancellationTokenSource = new CancellationTokenSource();
            this.CancellationToken = this.CancellationTokenSource.Token;
        }

        public static MockFabricRuntime Current
        {
            get => (MockFabricRuntime)FabricRuntimeContextWrapper.Current?[FabricRuntimeContextKeys.CurrentMockFabricRuntime];
            set => FabricRuntimeContextWrapper.Current[FabricRuntimeContextKeys.CurrentMockFabricRuntime] = value;
        }

        public IActorProxyFactory ActorProxyFactory => this._actorProxyFactory;

        public CancellationToken CancellationToken { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

        public bool DisableMethodCallOutput { get; set; }

        public IPartitionEnumerationManager PartitionEnumerationManager => this._partitionEnumerationManager;

        public IServiceProxyFactory ServiceProxyFactory => this._serviceProxyFactory;

        private IEnumerable<MockServiceInstance> Instances => this._activeInstances;

        public MockFabricApplication GetApplication(string applicationInstanceName)
        {
            var existingApplication = this._applications.FirstOrDefault(a => a.ApplicationInstanceName.Equals(applicationInstanceName, StringComparison.InvariantCulture));
            if (existingApplication == null)
            {
                throw new ArgumentException(
                    $"An application with name {applicationInstanceName} could not be found this runtime, register one with {nameof(this.RegisterApplication)}");
            }

            return existingApplication;
        }

        public IEnumerable<IMockServiceInstance> GetInstances()
        {
            return this._activeInstances.Select(i => i as IMockServiceInstance).ToArray();
        }

        public MockFabricApplication RegisterApplication(string applicationInstanceName)
        {
            var existingApplication = this._applications.FirstOrDefault(a => a.ApplicationInstanceName.Equals(applicationInstanceName, StringComparison.InvariantCulture));
            if (existingApplication != null)
            {
                throw new ArgumentException($"An application with name {applicationInstanceName} is already registered in this runtime");
            }

            var mockFabricApplication = new MockFabricApplication(this, applicationInstanceName);

            this._applications.Add(mockFabricApplication);
            return mockFabricApplication;
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
                new NodeContext("Node0", new NodeId(0, 1), 0, "NodeType1", "TEST.MACHINE"),
                this.GetCodePackageContext(applicationName, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault()),
                $"{serviceName}Type",
                new Uri($"fabric:/{applicationName}/{serviceName}"),
                null,
                partitionInformation.Id,
                replicaId);
        }

        internal StatelessServiceContext BuildStatelessServiceContext(string applicationName, string serviceName, IServiceManifest serviceManifest, IServiceConfig serviceConfig)
        {
            return new StatelessServiceContext(
                new NodeContext("Node0", new NodeId(0, 1), 0, "NodeType1", "TEST.MACHINE"),
                this.GetCodePackageContext(applicationName, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault()),
                serviceName,
                new Uri($"fabric:/{applicationName}/{serviceName}"),
                null,
                Guid.NewGuid(),
                1L);
        }

        internal MockActorServiceInstance GetActorServiceInstance(Type actorInterfaceType, ServicePartitionKey partitionKey)
        {
            return this._activeInstances.SingleOrDefault(i => i.Equals(actorInterfaceType, partitionKey)) as MockActorServiceInstance;
        }

        internal ApplicationUriBuilder GetApplicationUriBuilder(string applicationName)
        {
            return new ApplicationUriBuilder(this.GetCodePackageContext(applicationName, ServiceManifest.DefaultFor("no-service"), null));
        }

        internal ICodePackageActivationContext GetCodePackageContext(string applicationName, IServiceManifest serviceManifest, IServiceConfig serviceConfig)
        {
            return new MockCodePackageActivationContext(
                $"fabric:/{applicationName}",
                $"{applicationName}Type",
                "Code",
                "1.0.0.0",
                Guid.NewGuid().ToString(),
                @"C:\Log",
                @"C:\Temp",
                @"C:\Work",
                serviceManifest,
                serviceConfig);
        }

        internal MockServiceInstance GetServiceInstance(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
        {
            return this._activeInstances.SingleOrDefault(i => i.Equals(serviceUri, serviceInterfaceType, partitionKey));
        }

        internal IEnumerable<MockServiceInstance> GetServiceInstances(Uri serviceUri)
        {
            return this._activeInstances.Where(i => i.ServiceUri.ToString().Equals(serviceUri.ToString(), StringComparison.InvariantCultureIgnoreCase));
        }

        internal void RegisterInstances(IEnumerable<MockServiceInstance> instances)
        {
            var mockServiceInstances = instances.ToArray();
            foreach (var instance in mockServiceInstances)
            {
                if (this._activeInstances.Any(i => i.ServiceUri.Equals(instance.ServiceUri)))
                {
                    throw new MockFabricSetupException(
                        $"Trying to register services with {instance.ServiceUri}, but MockFabricRuntime already has a registration for that ServiceUri");
                }
            }

            this._activeInstances.AddRange(mockServiceInstances);
        }

        private static class FabricRuntimeContextKeys
        {
            public const string CurrentMockFabricRuntime = "mockFabricRuntime";
        }
    }
}