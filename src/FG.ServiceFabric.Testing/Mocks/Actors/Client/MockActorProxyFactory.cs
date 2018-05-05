using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Extensions;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.State;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System.Collections.Concurrent;

    public class MockActorProxyFactory : IActorProxyFactory, IMockActorProxyManager, IMockServiceProxyManager
    {
        private readonly IDictionary<IActor, Actor> _actorProxies;
        private readonly IDictionary<Type, IDictionary<Guid, IActorStateProvider>> _actorStateProviders;
        private readonly MockFabricRuntime _fabricRuntime;

        internal MockActorProxyFactory(
            MockFabricRuntime fabricRuntime,
            params IMockableActorRegistration[] actorRegistrations
        )
        {
            _fabricRuntime = fabricRuntime;
            _actorStateProviders = new ConcurrentDictionary<Type, IDictionary<Guid, IActorStateProvider>>();
            _actorProxies = new Dictionary<IActor, Actor>();
        }

        public TActorInterface CreateActorProxy<TActorInterface>(ActorId actorId, string applicationName = null,
            string serviceName = null, string listenerName = null)
            where TActorInterface : IActor
        {
            var task = Create<TActorInterface>(actorId);
            return task.GetAwaiter().GetResult();
        }

        public TActorInterface CreateActorProxy<TActorInterface>(Uri serviceUri, ActorId actorId,
            string listenerName = null)
            where TActorInterface : IActor
        {
            var task = Create<TActorInterface>(actorId);
            return task.GetAwaiter().GetResult();
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, ActorId actorId,
            string listenerName = null)
            where TServiceInterface : IService
        {
            var partitionKey = new ServicePartitionKey(actorId.GetPartitionKey());
            return CreateActorService<TServiceInterface>(serviceUri, partitionKey);
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, long partitionKey,
            string listenerName = null) where TServiceInterface : IService
        {
            return CreateActorService<TServiceInterface>(serviceUri, new ServicePartitionKey(partitionKey));
        }

        private readonly ConcurrentDictionary<MethodInfo, ActorMethodContext> actorMethodContextCache = new ConcurrentDictionary<MethodInfo, ActorMethodContext>();

        void IMockActorProxyManager.BeforeMethod(IActor proxy, MethodInfo method)
        {
            var actor = GetActor<Actor>(proxy);
            if (actor == null) return;

            if (!_fabricRuntime.DisableMethodCallOutput)
            {
                Console.WriteLine();
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                var message = $"Actor {actor?.GetType().Name} '{actor?.Id}'({actor?.GetHashCode()}) {method} activating";
                Console.WriteLine($"{message.PadRight(80, '=')}");
                Console.ForegroundColor = color;
            }

            // CallOnPreActorMethodAsync is asynchronous and has not finished when BeforeMethod terminates
            this.CallOnPreActorMethodAsync(method, actor).GetAwaiter().GetResult();
        }

        private Task CallOnPreActorMethodAsync(MethodInfo method, Actor actor)
        {
            var actorMethodContext = this.actorMethodContextCache.GetOrAdd(
                method,
                m => ReflectionUtils.ActivateInternalCtor<ActorMethodContext>(m.Name, ActorCallType.ActorInterfaceMethod));

            if (actor is FG.ServiceFabric.Actors.Runtime.ActorBase actorBase)
            {
                return actorBase.MockRuntimeHelperInstance.OnPreActorMethodAsync(actorMethodContext);
            }

            return (Task)actor.CallPrivateMethod("OnPreActorMethodAsync", actorMethodContext);
        }

        void IMockActorProxyManager.AfterMethod(IActor proxy, MethodInfo method)
        {
            var actor = GetActor<Actor>(proxy);
            if (actor == null) return;

            this.AfterMethodInternal(method, actor).GetAwaiter().GetResult();
        }

        private async Task AfterMethodInternal(MethodInfo method, Actor actor)
        {
            if (!this._fabricRuntime.DisableMethodCallOutput)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                var message = $"Actor {actor.GetType().Name} '{actor.Id}'({actor.GetHashCode()}) {method} terminating";
                Console.WriteLine($"{message.PadRight(80, '=')}");
                Console.ForegroundColor = color;
                Console.WriteLine();
            }

            await this.CallPostActorMethodAsync(method, actor);
            await actor.StateManager.SaveStateAsync(CancellationToken.None);
        }

        private Task CallPostActorMethodAsync(MethodInfo method, Actor actor)
        {
            var actorMethodContext = this.actorMethodContextCache.GetOrAdd(
                method,
                m => ReflectionUtils.ActivateInternalCtor<ActorMethodContext>(m.Name, ActorCallType.ActorInterfaceMethod));

            if (actor is FG.ServiceFabric.Actors.Runtime.ActorBase actorBase)
            {
                return actorBase.MockRuntimeHelperInstance.OnPostActorMethodAsync(actorMethodContext);
            }

            return (Task)actor.CallPrivateMethod("OnPostActorMethodAsync", actorMethodContext);
        }

        void IMockServiceProxyManager.BeforeMethod(IService service, MethodInfo method)
        {
            if (!_fabricRuntime.DisableMethodCallOutput)
            {
                Console.WriteLine();
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                var message = $"Service {service?.GetType().Name} ({service?.GetHashCode()}) {method} activating";
                Console.WriteLine($"{message.PadRight(80, '=')}");
                Console.ForegroundColor = color;
            }
        }

        void IMockServiceProxyManager.AfterMethod(IService service, MethodInfo method)
        {
            if (!_fabricRuntime.DisableMethodCallOutput)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                var message = $"Actor {service?.GetType().Name} ({service?.GetHashCode()}) {method} terminating";
                Console.WriteLine($"{message.PadRight(80, '=')}");
                Console.ForegroundColor = color;
                Console.WriteLine();
            }
        }

        private IActorStateProvider GetOrCreateActorStateProvider(
            IMockableActorRegistration actorRegistation,
            Func<IActorStateProvider> createStateProvider,
            ServicePartitionKey partitionKey)
        {
            var actorImplementationType = actorRegistation.ImplementationType;

            var actorStateProvider = GetActorStateProvider(actorRegistation, partitionKey);
            if (actorStateProvider != null) return actorStateProvider;

            actorStateProvider = createStateProvider();

            var migrationContainerType = GetMigrationContainerType(actorImplementationType);
            if (migrationContainerType != null)
            {
                var migrationContainer = (IMigrationContainer)Activator.CreateInstance(migrationContainerType);
                actorStateProvider = new VersionedStateProvider(actorStateProvider, migrationContainer);
            }

            var partitionId = actorRegistation.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            if (!_actorStateProviders.TryGetValue(actorImplementationType, out var stateProvidersForPartition))
            {
                stateProvidersForPartition = new Dictionary<Guid, IActorStateProvider>();
                _actorStateProviders.Add(actorImplementationType, stateProvidersForPartition);
            }

            if (stateProvidersForPartition.TryGetValue(partitionId, out var stateProviderPartition))
                return stateProviderPartition;

            stateProvidersForPartition.Add(partitionId, actorStateProvider);
            return actorStateProvider;
        }

        private IActorStateProvider GetActorStateProvider(IMockableActorRegistration actorRegistation,
            ServicePartitionKey partitionKey)
        {
            var actorImplementationType = actorRegistation.ImplementationType;

            var partitionId = actorRegistation.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            if (_actorStateProviders.TryGetValue(actorImplementationType, out var stateProvidersForPartition))
                return stateProvidersForPartition.GetValueOrDefault(partitionId);

            return null;
        }

        private ActorService GetMockActorService(
            StatefulServiceContext serviceContext,
            ActorTypeInformation actorTypeInformation,
            IActorStateProvider actorStateProvider,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            return new MockActorService(
                serviceContext.CodePackageActivationContext,
                _fabricRuntime.ServiceProxyFactory,
                _fabricRuntime.ActorProxyFactory,
                _fabricRuntime.BuildNodeContext(),
                serviceContext,
                actorTypeInformation,
                stateManagerFactory: stateManagerFactory,
                stateProvider: actorStateProvider);
        }

        private Type GetMigrationContainerType(Type actorType)
        {
            var versionedStateDeclaration = actorType.GetInterface(typeof(IVersionedState<>).Name);
            if (versionedStateDeclaration == null) return null;
            var migrationContainerType = versionedStateDeclaration.GetGenericArguments().Single();
            return migrationContainerType;
        }

        private async Task<TActorInterface> Create<TActorInterface>(ActorId actorId)
            where TActorInterface : IActor
        {
            var partitionKey = new ServicePartitionKey(actorId.GetPartitionKey());

            var actorInterfaceType = typeof(TActorInterface);
            var instance = _fabricRuntime.Instances.SingleOrDefault(i => i.Equals(actorInterfaceType, partitionKey)) as
                MockActorServiceInstance;

            var actorRegistration = instance?.ActorRegistration;
            if (actorRegistration == null)
                throw new ArgumentException(
                    $"Expected a MockableActorRegistration for the type {typeof(TActorInterface).Name}");

            if (actorRegistration.ImplementationType != null &&
                actorRegistration.ImplementationType.IsSubclassOf(typeof(Actor)))
            {
                var actorService = instance.ServiceInstance as ActorService;
                var firstActivation = true;
                if (instance.Actors.TryGetValue(actorId, out var target))
                {
                    firstActivation = false;
                }
                else if (actorRegistration.Activator != null)
                {
                    target = actorRegistration.Activator(actorService, actorId) as Actor;
                    instance.Actors[actorId] = target;
                }
                else
                {
                    target = ActivateActorWithReflection(actorRegistration.ImplementationType, actorService, actorId);
                    instance.Actors[actorId] = target;
                }

                if (target == null)
                    throw new NotSupportedException(
                        $"Failed to activate an instance of Actor {actorRegistration.ImplementationType.Name} for ActorId {actorId}");

                if (target is ServiceFabric.Actors.Runtime.ActorBase mockableTarget)
                {
                    var applicationName = actorService.Context.CodePackageActivationContext.ApplicationName;
                    var applicationUriBuilder =
                        new ApplicationUriBuilder(
                            _fabricRuntime.GetCodePackageContext(applicationName, instance.ServiceManifest,
                                instance.ServiceConfig), applicationName);

                    mockableTarget.SetPrivateField("_serviceProxyFactoryFactory",
                        (Func<IServiceProxyFactory>)(() => _fabricRuntime.ServiceProxyFactory));
                    mockableTarget.SetPrivateField("_actorProxyFactoryFactory",
                        (Func<IActorProxyFactory>)(() => _fabricRuntime.ActorProxyFactory));
                    mockableTarget.SetPrivateField("_applicationUriBuilder", applicationUriBuilder);
                }

                if (firstActivation)
                {
                    var result = target.CallPrivateMethod("OnActivateAsync");
                    if (result is Task returnTask)
                    {
                        await returnTask;
                    }

                    var actorStateProvider = instance.ActorStateProvider;
                    await actorStateProvider.ActorActivatedAsync(actorId);
                }

                var serviceUri = instance.ActorRegistration.ServiceRegistration.ServiceUri;

                object targetObject = target;

                var actorProxy = new MockActorProxy<TActorInterface>(
                    (TActorInterface)targetObject,
                    typeof(TActorInterface),
                    actorId,
                    serviceUri,
                    ServicePartitionKey.Singleton,
                    TargetReplicaSelector.Default,
                    string.Empty,
                    null);
                var proxy = actorProxy.Proxy;

                _actorProxies.Add(proxy, target);

                return (TActorInterface)proxy;
            }
            else
            {
                var target = actorRegistration.Activator(null, actorId);
                return (TActorInterface)target;
            }
        }

        private TActor GetActor<TActor>(IActor proxy)
            where TActor : class
        {
            if (_actorProxies.TryGetValue(proxy, out var actor))
                return actor as TActor;

            return null;
        }

        private TServiceInterface CreateActorService<TServiceInterface>(Uri serviceUri,
            ServicePartitionKey partitionKey)
            where TServiceInterface : IService
        {
            var serviceInterfaceType = typeof(TServiceInterface);
            var instance =
                _fabricRuntime.Instances.SingleOrDefault(i => i.Equals(serviceUri, serviceInterfaceType, partitionKey))
                    as
                    MockActorServiceInstance;

            var actorRegistration = instance?.ActorRegistration;
            if (actorRegistration == null)
                throw new ArgumentException(
                    $"Expected a MockableActorRegistration with ActorServiceType for the type {typeof(TServiceInterface).Name}");

            var mockServiceProxy = new MockActorServiceProxy<TServiceInterface>((TServiceInterface)instance.ServiceInstance, serviceUri, serviceInterfaceType,
                partitionKey,
                TargetReplicaSelector.Default, string.Empty, null, this);


            return (TServiceInterface)mockServiceProxy.Proxy;
        }

        internal Actor ActivateActorWithReflection(Type actorType, ActorService actorService, ActorId actorId)
        {
            if (!typeof(Actor).IsAssignableFrom(actorType))
                throw new ArgumentException(
                    $"Cannot activate instance of {actorType?.FullName} as it should inherit from {typeof(Actor).FullName}");

            var instance = (Actor)actorType.ActivateCtor(actorService, actorId);
            return instance;
        }
    }
}