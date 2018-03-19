using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using FG.Common.Extensions;
using FG.Common.Settings;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Utils;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Setup
{
    public class MockFabricRuntimeIntegratedSetupBase
    {
        protected void Setup(MockFabricRuntime mockFabricRuntime, string applicationProjectPath,
            string applicationParametersPath = null)
        {
            var applicationProjectFolder = Path.GetDirectoryName(applicationProjectPath);

            if (applicationParametersPath == null)
                applicationParametersPath =
                    PathExtensions.GetAbsolutePath(applicationProjectFolder, @"ApplicationParameters\Cloud.xml");

            var applicationProject = ApplicationProject.Load(applicationProjectPath);
            var applicationManifest =
                ApplicationManifest.Load(applicationProject.ApplicationManifestPath, applicationParametersPath);

            var mockFabricApplication = mockFabricRuntime.RegisterApplication(applicationManifest.ApplicationName);

            var serviceProjects = new List<ServiceProject>();
            foreach (var defaultService in applicationManifest.DefaultServices)
            {
                MockServiceDefinition serviceDefinition = null;

                if (defaultService.IsStateless)
                {
                    serviceDefinition = MockServiceDefinition.CreateStateless(defaultService.InstanceCount);
                }
                else
                {
                    if (defaultService.ServicePartitionKind == ServicePartitionKind.Int64Range)
                        serviceDefinition = MockServiceDefinition.CreateUniformInt64Partitions(
                            defaultService.PartitionCount ?? 10, defaultService.PartitioningLowLey ?? long.MinValue,
                            defaultService.PartitioningHighLey ?? long.MaxValue);
                    else if (defaultService.ServicePartitionKind == ServicePartitionKind.Named)
                        serviceDefinition = MockServiceDefinition.CreateNamedPartitions(defaultService.NamedPartitions);
                    else
                        serviceDefinition = MockServiceDefinition.CreateSingletonPartition();
                }

                if (!(applicationProject.ServiceProjects.FirstOrDefault(s =>
                    s.Manifest.ServiceTypes.Any(t => t == defaultService.TypeName)) is ServiceProject serviceProject))
                    throw new MockFabricSetupException(
                        $"Could not find ServiceType {defaultService.TypeName} in referenced services in the Application {applicationManifest.ApplicationName}");

                if (!serviceProjects.Contains(serviceProject))
                {
                    serviceProject.ApplicationName = applicationManifest.ApplicationName;
                    serviceProject.FabricApplication = mockFabricApplication;
                    serviceProjects.Add(serviceProject);
                }

                serviceProject.ServiceDefinitions.Add(defaultService.TypeName, serviceDefinition);
            }

            ServiceProject.LoadAssemblies(GetType(), serviceProjects);
            foreach (var serviceProject in serviceProjects)
            {
                var actorTypes = serviceProject.Assembly.GetTypes().Where(type => typeof(IActor).IsAssignableFrom(type))
                    .ToDictionary(t => t.Name, t => t);
                var actorServiceTypes = serviceProject.Assembly.GetTypes()
                    .Where(type => typeof(ActorService).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);
                var statelessServiceTypes = serviceProject.Assembly.GetTypes()
                    .Where(type => typeof(StatelessService).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);
                var statefulServiceTypes = serviceProject.Assembly.GetTypes()
                    .Where(type => typeof(StatefulService).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);

                var serviceManifestImport =
                    applicationManifest.ServiceManifestImports.FirstOrDefault(import =>
                        import.Name.Equals(serviceProject.Manifest.Name));
                if (serviceManifestImport == null)
                    throw new MockFabricSetupException(
                        $"Tried to load project {serviceProject.Name}/{serviceProject.Assembly.FullName} but did not find a ServiceManifestImport in the ApplicationManifest for {serviceProject.Manifest.Name}");

                serviceProject.LoadConfigOverrides(serviceManifestImport);

                foreach (var serviceDefinition in serviceProject.ServiceDefinitions)
                {
                    var serviceName = serviceDefinition.Key.RemoveFromEnd("Type");
                    var actorName = serviceDefinition.Key.RemoveFromEnd("ServiceType");

                    // TODO: Make this consider ActorServiceName attribute for actorTypes as well and not just naive name/string matching
                    if (actorTypes.TryGetValue(actorName, out var actorType))
                    {
                        var actorServiceType = actorServiceTypes.GetValueOrDefault(serviceName, typeof(Actor));

                        Console.WriteLine(
                            $"Create ActorService {serviceName} for type {actorName}/{actorServiceType.FullName} - {serviceDefinition}");

                        SetupActor(serviceProject.FabricApplication, actorType, actorServiceType,
                            serviceDefinition.Value);
                    }
                    else if (actorServiceTypes.TryGetValue(serviceName, out var actorServiceType))
                    {
                        var actorTypeNameBeginning = serviceName.RemoveFromEnd("Service").RemoveFromEnd("Actor");
                        var likelyActorType =
                            actorTypes.Values.FirstOrDefault(at => at.Name.StartsWith(actorTypeNameBeginning));
                        Console.WriteLine(
                            $"Create ActorService {serviceName} for type {actorName}/{likelyActorType?.FullName} - {serviceDefinition}");

                        SetupActor(serviceProject.FabricApplication, likelyActorType, actorServiceType,
                            serviceDefinition.Value);
                    }
                    else if (statefulServiceTypes.TryGetValue(serviceName, out var statefulServiceType))
                    {
                        Console.WriteLine(
                            $"Create StatefulService {serviceName} for type {statefulServiceTypes[serviceName].FullName} - {serviceDefinition}");

                        SetupStatefulService(serviceProject.FabricApplication, statefulServiceType,
                            serviceDefinition.Value);
                    }
                    else if (statelessServiceTypes.TryGetValue(serviceName, out var statelessServiceType))
                    {
                        Console.WriteLine(
                            $"Create Stateless {serviceName} for type {statelessServiceTypes[serviceName].FullName} - {serviceDefinition}");

                        SetupStatelessService(serviceProject.FabricApplication, statelessServiceType,
                            serviceDefinition.Value);
                    }
                }
            }
        }

        private ActorBase CreateActor(Type actorType, ActorService actorService, ActorId actorId)
        {
            var maxArgsConstructor = actorType.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();
            if (maxArgsConstructor == null)
                throw new MockFabricSetupException($"Could not find a .ctor for Actor {actorType.Name}");

            var parameters = new List<object>();
            foreach (var constructorParameter in maxArgsConstructor.GetParameters())
            {
                var parameterType = constructorParameter.ParameterType;

                if (typeof(ActorService).IsAssignableFrom(parameterType))
                {
                    parameters.Add(actorService);
                }
                else if (typeof(ActorId).IsAssignableFrom(parameterType))
                {
                    parameters.Add(actorId);
                }
                else
                {
                    object parameter = null;
                    if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
                        parameter = CreateStateSessionManager(actorService.Context);
                    else if (typeof(ISettingsProvider).IsAssignableFrom(parameterType))
                        parameter = CreateSettingsProvider(actorService.Context, actorService.GetType());

                    parameter = CreateActorParameter(actorType, actorService, actorId, parameterType, parameter);
                    if (parameter == null)
                        throw new MockServiceRuntimeException(
                            $"Trying to setup Actor {actorType.Name} {actorId} and needs parameter of type {parameterType.GetFriendlyName()} but test class does not override CreateActorParameter or the override returns null");
                    if (parameter is IgnoredSetupParameter)
                        parameter = null;
                    parameters.Add(parameter);
                }
            }

            var actor = maxArgsConstructor.Invoke(parameters.ToArray()) as ActorBase;
            return actor;
        }

        private void SetupActor(MockFabricApplication fabricApplication, Type actorType, Type actorServiceType,
            MockServiceDefinition serviceDefinition)
        {
            var actorStateManagerFactory = CreateActorStateManagerFactory();

            fabricApplication.SetupActor(
                actorType,
                actorServiceType,
                (actorService, actorId) => CreateActor(actorType, actorService, actorId),
                (context, information, stateProvider, stateManagerFactory) => CreateActorService(actorServiceType,
                    information, context, stateProvider, stateManagerFactory),
                actorStateManagerFactory != null
                    ? (CreateActorStateManager) ((actor, provider) => actorStateManagerFactory(actor, provider))
                    : null,
                CreateActorStateProvider,
                serviceDefinition);
        }

        private void SetupStatelessService(MockFabricApplication fabricApplication, Type serviceType,
            MockServiceDefinition serviceDefinition)
        {
            fabricApplication.SetupService(
                serviceType,
                context => CreateStatelessService(serviceType, context),
                serviceDefinition);
        }

        private void SetupStatefulService(MockFabricApplication fabricApplication, Type serviceType,
            MockServiceDefinition serviceDefinition)
        {
            fabricApplication.SetupService(
                serviceType,
                (context, stateManagerReplica) => CreateStatefulService(serviceType, context, stateManagerReplica),
                () => CreateStateManager(serviceType, serviceDefinition),
                serviceDefinition);
        }

        private StatefulService CreateStatefulService(Type serviceType, StatefulServiceContext context,
            IReliableStateManagerReplica2 stateManagerReplica)
        {
            var maxArgsConstructor = serviceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();
            if (maxArgsConstructor == null)
                throw new MockFabricSetupException($"Could not find a .ctor for ServiceType {serviceType.Name}");

            var parameters = new List<object>();
            foreach (var constructorParameter in maxArgsConstructor.GetParameters())
            {
                var parameterType = constructorParameter.ParameterType;
                if (typeof(StatefulServiceContext).IsAssignableFrom(parameterType))
                {
                    parameters.Add(context);
                }
                else if (typeof(StatefulServiceContext).IsAssignableFrom(parameterType))
                {
                    parameters.Add(context);
                }
                else
                {
                    object parameter = null;
                    if (typeof(IReliableStateManagerReplica2).IsAssignableFrom(parameterType))
                        parameters.Add(stateManagerReplica);
                    else if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
                        parameter = CreateStateSessionManager(context);
                    else if (typeof(ISettingsProvider).IsAssignableFrom(parameterType))
                        parameter = CreateSettingsProvider(context, serviceType);

                    parameter = CreateServiceParameter(context, serviceType, parameterType, parameter);
                    if (parameter == null)
                        throw new MockServiceRuntimeException(
                            $"Trying to setup StatefulService {serviceType.Name} but test class does not override CreateServiceParameter or the override returns null for parameter {parameterType.GetFriendlyName()} on {context.ServiceName}");
                    if (parameter is IgnoredSetupParameter)
                        parameter = null;
                    parameters.Add(parameter);
                }
            }

            var service = maxArgsConstructor.Invoke(parameters.ToArray()) as StatefulService;
            return service;
        }

        private StatelessService CreateStatelessService(Type serviceType, StatelessServiceContext context)
        {
            var maxArgsConstructor = serviceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();
            if (maxArgsConstructor == null)
                throw new MockFabricSetupException($"Could not find a .ctor for ServiceType {serviceType.Name}");

            var parameters = new List<object>();
            foreach (var constructorParameter in maxArgsConstructor.GetParameters())
            {
                var parameterType = constructorParameter.ParameterType;
                if (typeof(StatelessServiceContext).IsAssignableFrom(parameterType))
                {
                    parameters.Add(context);
                }
                else
                {
                    object parameter = null;
                    if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
                        parameter = CreateStateSessionManager(context);
                    else if (typeof(ISettingsProvider).IsAssignableFrom(parameterType))
                        parameter = CreateSettingsProvider(context, serviceType);

                    parameter = CreateServiceParameter(context, serviceType, parameterType, parameter);
                    if (parameter == null)
                        throw new MockServiceRuntimeException(
                            $"Trying to setup StatelessService {serviceType.Name} but test class does not override CreateServiceParameter or the override returns null for parameter {parameterType.GetFriendlyName()} on {context.ServiceName}");
                    if (parameter is IgnoredSetupParameter)
                        parameter = null;
                    parameters.Add(parameter);
                }
            }

            var service = maxArgsConstructor.Invoke(parameters.ToArray()) as StatelessService;
            return service;
        }


        private ActorService CreateActorService(Type actorServiceType, ActorTypeInformation actorTypeInformation,
            StatefulServiceContext context, IActorStateProvider stateProvider,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            var maxArgsConstructor = actorServiceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();
            if (maxArgsConstructor == null)
                throw new MockFabricSetupException($"Could not find a .ctor for ServiceType {actorServiceType.Name}");

            var parameters = new List<object>();
            foreach (var constructorParameter in maxArgsConstructor.GetParameters())
            {
                var parameterType = constructorParameter.ParameterType;


                if (typeof(IActorStateProvider).IsAssignableFrom(parameterType))
                {
                    parameters.Add(stateProvider);
                }
                else if (typeof(StatefulServiceContext).IsAssignableFrom(parameterType))
                {
                    parameters.Add(context);
                }
                else if (typeof(ActorTypeInformation).IsAssignableFrom(parameterType))
                {
                    parameters.Add(actorTypeInformation);
                }
                else if (typeof(Func<ActorService, ActorId, ActorBase>).IsAssignableFrom(parameterType))
                {
                    parameters.Add((Func<ActorService, ActorId, ActorBase>) ((service, actorId) =>
                        CreateActor(actorTypeInformation.ImplementationType, service, actorId)));
                }
                else
                {
                    object parameter = null;
                    if (typeof(Func<ActorBase, IActorStateProvider, IActorStateManager>).IsAssignableFrom(parameterType)
                    )
                        parameter = stateManagerFactory;
                    else if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
                        parameter = CreateStateSessionManager(context);
                    else if (typeof(ISettingsProvider).IsAssignableFrom(parameterType))
                        parameter = CreateSettingsProvider(context, actorServiceType);

                    parameter = CreateActorServiceParameter(actorServiceType, parameterType, parameter);
                    if (parameter == null && !constructorParameter.IsOptional)
                        throw new MockServiceRuntimeException(
                            $"Trying to setup ActorService {actorServiceType.Name} but test class does not override CreateActorServiceParameter or the override returns null for parameter {parameterType.GetFriendlyName()} on {context.ServiceName}");
                    if (parameter is IgnoredSetupParameter)
                        parameter = null;
                    parameters.Add(parameter);
                }
            }

            var actorService = maxArgsConstructor.Invoke(parameters.ToArray()) as ActorService;
            return actorService;
        }

        protected virtual ISettingsProvider CreateSettingsProvider(ServiceContext context, Type serviceType)
        {
            return new ServiceConfigSettingsProvider(context);
        }

        protected virtual object CreateActorParameter(Type actorType, ActorService actorService, ActorId actorId,
            Type parameterType, object defaultValue)
        {
            return defaultValue;
        }

        protected virtual Func<ActorBase, IActorStateProvider, IActorStateManager> CreateActorStateManagerFactory()
        {
            return null;
        }

        protected virtual IActorStateProvider CreateActorStateProvider(StatefulServiceContext context,
            ActorTypeInformation actorTypeInformation)
        {
            return null;
        }

        protected virtual object CreateActorServiceParameter(Type actorServiceType, Type parameterType,
            object defaultValue)
        {
            return defaultValue;
        }


        protected virtual IStateSessionManager CreateStateSessionManager(ServiceContext context)
        {
            return null;
        }

        protected virtual IReliableStateManagerReplica2 CreateStateManager(Type serviceType,
            MockServiceDefinition serviceDefinition)
        {
            return null;
        }

        protected virtual object CreateServiceParameter(StatefulServiceContext context, Type serviceType,
            Type parameterType, object defaultValue)
        {
            return defaultValue;
        }

        protected virtual object CreateServiceParameter(StatelessServiceContext context, Type serviceType,
            Type parameterType, object defaultValue)
        {
            return defaultValue;
        }
    }
}