namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using FG.ServiceFabric.Testing.Mocks.Actors.Client;
    using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
    using FG.ServiceFabric.Testing.Setup;

    using Microsoft.ServiceFabric.Services.Client;

    internal class MockServiceInstance : IMockServiceInstance
    {
        public IMockableActorRegistration ActorRegistration { get; private set; }

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public Partition Partition { get; private set; }

        public Replica Replica { get; private set; }

        public DateTime? RunAsyncEnded { get; private set; }

        public DateTime? RunAsyncStarted { get; private set; }

        public IServiceConfig ServiceConfig { get; private set; }

        public object ServiceInstance { get; set; }

        public IServiceManifest ServiceManifest { get; private set; }

        public IMockableServiceRegistration ServiceRegistration { get; private set; }

        public Uri ServiceUri { get; private set; }

        public MockActorServiceInstanceStatus Status { get; private set; }

        protected MockFabricRuntime FabricRuntime { get; private set; }

        public static IEnumerable<MockServiceInstance> Register(
            MockFabricRuntime fabricRuntime,
            IMockableActorRegistration actorRegistration,
            IServiceManifest serviceManifest,
            IServiceConfig serviceConfig)
        {
            var instances = new List<MockServiceInstance>();
            foreach (var replica in actorRegistration.ServiceRegistration.ServiceDefinition.Instances)
            {
                foreach (var partition in actorRegistration.ServiceRegistration.ServiceDefinition.Partitions)
                {
                    MockServiceInstance instance;

                    if (actorRegistration.IsSimple)
                    {
                        instance = new SimpleMockActorServiceInstance
                        {
                            ActorRegistration = actorRegistration,
                            ServiceRegistration = actorRegistration.ServiceRegistration,
                            FabricRuntime = fabricRuntime,
                            Partition = partition,
                            Replica = replica,
                            ServiceUri = actorRegistration.ServiceRegistration.ServiceUri,
                            ServiceManifest = serviceManifest,
                            ServiceConfig = serviceConfig,
                        };
                    }
                    else
                    {
                        instance = new MockActorServiceInstance
                        {
                            ActorRegistration = actorRegistration,
                            ServiceRegistration = actorRegistration.ServiceRegistration,
                            FabricRuntime = fabricRuntime,
                            Partition = partition,
                            Replica = replica,
                            ServiceUri = actorRegistration.ServiceRegistration.ServiceUri,
                            ServiceManifest = serviceManifest,
                            ServiceConfig = serviceConfig
                        };
                    }

                    instances.Add(instance);
                }
            }

            fabricRuntime.RegisterInstances(instances);

            foreach (var instance in instances)
            {
                instance.Build();
            }

            return instances;
        }

        public static IEnumerable<MockServiceInstance> Register(
            MockFabricRuntime fabricRuntime,
            IMockableServiceRegistration serviceRegistration,
            IServiceManifest serviceManifest,
            IServiceConfig serviceConfig)
        {
            var instances = new List<MockServiceInstance>();
            foreach (var replica in serviceRegistration.ServiceDefinition.Instances)
            {
                foreach (var partition in serviceRegistration.ServiceDefinition.Partitions)
                {
                    MockServiceInstance instance = null;

                    if (serviceRegistration.IsSimple)
                    {
                        instance = new SimpleMockServiceInstance
                        {
                            FabricRuntime = fabricRuntime,
                            Partition = partition,
                            Replica = replica,
                            ServiceUri = serviceRegistration.ServiceUri,
                            ServiceManifest = serviceManifest,
                            ServiceConfig = serviceConfig,
                            ServiceInstance = serviceRegistration.ServiceInstance,
                        };
                    }
                    else if (serviceRegistration.IsStateful)
                    {
                        instance = new MockStatefulServiceInstance
                        {
                            ServiceRegistration = serviceRegistration,
                            FabricRuntime = fabricRuntime,
                            Partition = partition,
                            Replica = replica,
                            ServiceUri = serviceRegistration.ServiceUri,
                            ServiceManifest = serviceManifest,
                            ServiceConfig = serviceConfig
                        };
                    }
                    else
                    {
                        instance = new MockStatelessServiceInstance
                        {
                            ServiceRegistration = serviceRegistration,
                            FabricRuntime = fabricRuntime,
                            Partition = partition,
                            Replica = replica,
                            ServiceUri = serviceRegistration.ServiceUri,
                            ServiceManifest = serviceManifest,
                            ServiceConfig = serviceConfig
                        };
                    }

                    instances.Add(instance);
                }
            }

            fabricRuntime.RegisterInstances(instances);

            foreach (var instance in instances)
            {
                instance.Build();
            }

            return instances;
        }

        public virtual bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
        {
            if (this.ServiceRegistration?.ServiceDefinition.PartitionKind != partitionKey.Kind)
            {
                return false;
            }

            var partitionId = this.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            return serviceUri.ToString().Equals(this.ServiceUri.ToString(), StringComparison.InvariantCultureIgnoreCase)
                   && this.ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType)
                   && this.Partition.PartitionInformation.Id == partitionId;
        }

        public virtual bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
        {
            return false;
        }

        public override string ToString()
        {
            return $"{nameof(MockServiceInstance)}: {this.ServiceUri}";
        }

        protected virtual void Build()
        {
            if (this.ServiceInstance == null)
            {
                throw new NotSupportedException("Could not determine the type of service instance");
            }

            this.RunAsync();
        }

        private Task RunAsync()
        {
            return Task.Run(
                async () =>
                    {
                        var runAsyncMethod = MockServiceReflectionHelper.GetRunAsync(this.ServiceRegistration.IsStateful);
                        this.CancellationTokenSource = new CancellationTokenSource();

                        this.RunAsyncStarted = DateTime.Now;
                        if (!this.FabricRuntime.DisableMethodCallOutput)
                        {
                            Console.WriteLine(
                                $"Started RunAsync for {this.ServiceUri} {this.Partition.PartitionInformation.Id}/{this.Replica.Id} - {this.ServiceInstance.GetHashCode()}");
                        }

                        await (Task)runAsyncMethod.Invoke(this.ServiceInstance, new object[] { this.CancellationTokenSource.Token });

                        this.RunAsyncEnded = DateTime.Now;
                        if (!this.FabricRuntime.DisableMethodCallOutput)
                        {
                            Console.WriteLine(
                                $"Finished RunAsync for {this.ServiceInstance.GetHashCode()} in {(this.RunAsyncEnded.Value - this.RunAsyncStarted.Value).TotalMilliseconds} ms");
                        }
                    });
        }
    }
}