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

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    using System.Collections.Concurrent;

    internal class MockActorServiceInstance : BaseMockActorServiceInstance
    {
        public IActorStateProvider ActorStateProvider { get; private set; }

        public override string ToString()
        {
            return $"{nameof(MockActorServiceInstance)}: {ServiceUri}";
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
            var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(
                ActorRegistration.ServiceRegistration.GetApplicationName(),
                ActorRegistration.ServiceRegistration.Name,
                Partition.PartitionInformation,
                Replica.Id,
                ServiceManifest,
                ServiceConfig);
            ActorStateProvider =
            (ActorRegistration.CreateActorStateProvider ?? ((context, actorInfo) =>
                 (IActorStateProvider)new MockActorStateProvider(FabricRuntime))).Invoke(
                statefulServiceContext,
                actorTypeInformation);

            var stateManagerFactory = ActorRegistration.CreateActorStateManager != null
                ? (Func<ActorBase, IActorStateProvider, IActorStateManager>)((actor, stateProvider) =>
                   ActorRegistration.CreateActorStateManager(actor, stateProvider))
                : null;
            var actorServiceFactory = ActorRegistration.CreateActorService ?? CreateActorService;

            // TODO: consider this further, is it really what should be done???
            var actorService = actorServiceFactory(statefulServiceContext, actorTypeInformation, ActorStateProvider,
                stateManagerFactory);
            if (actorService is ServiceFabric.Actors.Runtime.ActorService)
            {
                var applicationUriBuilder = new ApplicationUriBuilder(
                    statefulServiceContext.CodePackageActivationContext,
                    statefulServiceContext.CodePackageActivationContext.ApplicationName);
                actorService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
                actorService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
                actorService.SetPrivateField("_applicationUriBuilder", applicationUriBuilder);
            }

            ServiceInstance = actorService;

            base.Build();
        }

        private ActorService CreateActorService(
            StatefulServiceContext serviceContext,
            ActorTypeInformation actorTypeInformation,
            IActorStateProvider actorStateProvider,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            var actorServiceType = ActorRegistration.ServiceRegistration.ImplementationType;

            if (actorServiceType == typeof(ActorService))
                return new ActorService(serviceContext, actorTypeInformation, stateProvider: actorStateProvider,
                    stateManagerFactory: stateManagerFactory);

            var constructors =
                actorServiceType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
            {
                var arguments = new List<object>();
                var parameters = constructor.GetParameters().ToDictionary(p => p.ParameterType, p => p);

                var canConstruct = true;

                foreach (var parameter in parameters.Values)
                    if (parameter.ParameterType == typeof(StatefulServiceContext))
                        arguments.Add(serviceContext);
                    else if (parameter.ParameterType == typeof(ActorTypeInformation))
                        arguments.Add(actorTypeInformation);
                    else if (parameter.ParameterType == typeof(Func<ActorService, ActorId, ActorBase>))
                        arguments.Add(null);
                    else if (parameter.ParameterType ==
                             typeof(Func<ActorBase, IActorStateProvider, IActorStateManager>))
                        arguments.Add(stateManagerFactory);
                    else if (parameter.ParameterType == typeof(IActorStateProvider))
                        arguments.Add(actorStateProvider);
                    else if (parameter.ParameterType == typeof(ActorServiceSettings))
                        arguments.Add(null);
                    else
                        canConstruct = false;

                if (canConstruct)
                    return (ActorService)constructor.Invoke(null, arguments.ToArray());
            }

            return GetMockActorService(serviceContext, actorTypeInformation, actorStateProvider, stateManagerFactory);
        }

        private ActorService GetMockActorService(
            StatefulServiceContext serviceContext,
            ActorTypeInformation actorTypeInformation,
            IActorStateProvider actorStateProvider,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            return new MockActorService(
                serviceContext.CodePackageActivationContext,
                FabricRuntime.ServiceProxyFactory,
                FabricRuntime.ActorProxyFactory,
                FabricRuntime.BuildNodeContext(),
                serviceContext,
                actorTypeInformation,
                stateManagerFactory: stateManagerFactory,
                stateProvider: actorStateProvider);
        }
    }
}