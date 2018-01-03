namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Reflection;

    using FG.Common.Utils;
    using FG.ServiceFabric.Services.Runtime;
    using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
    using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Client;

    internal class MockActorServiceInstance : MockServiceInstance
    {
        public IDictionary<ActorId, Actor> Actors { get; private set; }

        public IActorStateProvider ActorStateProvider { get; private set; }

        public override string ToString()
        {
            return $"{nameof(MockActorServiceInstance)}: {this.ServiceUri}";
        }

        internal override bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
        {
            if (this.ActorRegistration?.ServiceRegistration.ServiceDefinition.PartitionKind != partitionKey.Kind)
            {
                return false;
            }

            var partitionId = this.ActorRegistration?.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            return this.ActorRegistration.InterfaceType == actorInterfaceType && this.Partition.PartitionInformation.Id == partitionId;
        }

        internal override bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
        {
            if (this.ActorRegistration.ServiceRegistration.ServiceDefinition.PartitionKind != partitionKey.Kind)
            {
                return false;
            }

            var partitionId = this.ActorRegistration.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            return serviceUri.ToString().Equals(this.ServiceUri.ToString(), StringComparison.InvariantCultureIgnoreCase)
                   && this.ActorRegistration.ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType)
                   && this.Partition.PartitionInformation.Id == partitionId;
        }

        protected override void Build()
        {
            var isActorService = this.ActorRegistration != null;

            if (!isActorService)
            {
                base.Build();
                return;
            }

            var actorTypeInformation = ActorTypeInformation.Get(this.ActorRegistration.ImplementationType);
            var statefulServiceContext = this.FabricRuntime.BuildStatefulServiceContext(
                this.ActorRegistration.ServiceRegistration.GetApplicationName(),
                this.ActorRegistration.ServiceRegistration.Name,
                this.Partition.PartitionInformation,
                this.Replica.Id,
                this.ServiceManifest,
                this.ServiceConfig);
            this.ActorStateProvider =
                (this.ActorRegistration.CreateActorStateProvider ?? ((context, actorInfo) => (IActorStateProvider)new MockActorStateProvider(this.FabricRuntime))).Invoke(
                    statefulServiceContext,
                    actorTypeInformation);

            var stateManagerFactory = this.ActorRegistration.CreateActorStateManager != null
                                          ? (Func<ActorBase, IActorStateProvider, IActorStateManager>)((actor, stateProvider) =>
                                                                                                              this.ActorRegistration.CreateActorStateManager(actor, stateProvider))
                                          : null;
            var actorServiceFactory = this.ActorRegistration.CreateActorService ?? this.CreateActorService;

            // TODO: consider this further, is it really what should be done???
            var actorService = actorServiceFactory(statefulServiceContext, actorTypeInformation, this.ActorStateProvider, stateManagerFactory);
            if (actorService is ServiceFabric.Actors.Runtime.ActorService)
            {
                var applicationUriBuilder = new ApplicationUriBuilder(
                    statefulServiceContext.CodePackageActivationContext,
                    statefulServiceContext.CodePackageActivationContext.ApplicationName);
                actorService.SetPrivateField("_serviceProxyFactory", this.FabricRuntime.ServiceProxyFactory);
                actorService.SetPrivateField("_actorProxyFactory", this.FabricRuntime.ActorProxyFactory);
                actorService.SetPrivateField("_applicationUriBuilder", applicationUriBuilder);
            }

            this.ServiceInstance = actorService;

            this.Actors = new Dictionary<ActorId, Actor>();

            base.Build();
        }

        private ActorService CreateActorService(
            StatefulServiceContext serviceContext,
            ActorTypeInformation actorTypeInformation,
            IActorStateProvider actorStateProvider,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            var actorServiceType = this.ActorRegistration.ServiceRegistration.ImplementationType;

            if (actorServiceType == typeof(ActorService))
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

            return this.GetMockActorService(serviceContext, actorTypeInformation, actorStateProvider, stateManagerFactory);
        }

        private ActorService GetMockActorService(
            StatefulServiceContext serviceContext,
            ActorTypeInformation actorTypeInformation,
            IActorStateProvider actorStateProvider,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            return new MockActorService(
                serviceContext.CodePackageActivationContext,
                this.FabricRuntime.ServiceProxyFactory,
                this.FabricRuntime.ActorProxyFactory,
                this.FabricRuntime.BuildNodeContext(),
                serviceContext,
                actorTypeInformation,
                stateManagerFactory: stateManagerFactory,
                stateProvider: actorStateProvider);
        }
    }
}