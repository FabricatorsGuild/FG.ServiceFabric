using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.State;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    public class MockActorProxyFactory : IActorProxyFactory
    {
        private readonly MockFabricRuntime _fabricRuntime;
        private readonly IDictionary<Type, IActorStateProvider> _actorStateProviders;

        private readonly IList<IMockableActorRegistration> _actorRegistrations;
        private readonly IDictionary<object, object> _actorProxies;

        public MockActorProxyFactory(
            MockFabricRuntime fabricRuntime,
            params IMockableActorRegistration[] actorRegistrations 
            )
        {
            _fabricRuntime = fabricRuntime;
            _actorRegistrations = new List<IMockableActorRegistration>(actorRegistrations);
            _actorStateProviders = new Dictionary<Type, IActorStateProvider>();
            _actorProxies = new Dictionary<object, object>();
        }

        private IActorStateProvider GetOrCreateActorStateProvider(Type actorImplementationType,
            Func<IActorStateProvider> createStateProvider)
        {
            var actorStateProvider = GetActorStateProvider(actorImplementationType);
            if (actorStateProvider != null) return actorStateProvider;

            actorStateProvider =  createStateProvider();

            var migrationContainerType = GetMigrationContainerType(actorImplementationType);
            if (migrationContainerType != null)
            {
                var migrationContainer = (IMigrationContainer)Activator.CreateInstance(migrationContainerType);
                actorStateProvider = new VersionedStateProvider(actorStateProvider, migrationContainer);
            }

            _actorStateProviders.Add(actorImplementationType, actorStateProvider);

            return actorStateProvider;
        }

        public IActorStateProvider GetActorStateProvider(Type actorImplementationType)
        {
            if (_actorStateProviders.ContainsKey(actorImplementationType))
            {
                return _actorStateProviders[actorImplementationType];
            }
            return null;
        }

        private ActorService GetMockActorService(
            StatefulServiceContext serviceContext,
            ActorTypeInformation actorTypeInformation,
            IActorStateProvider actorStateProvider, 
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        {
            return (ActorService)new MockActorService(
                    codePackageActivationContext: _fabricRuntime.CodePackageContext,
                    serviceProxyFactory: _fabricRuntime.ServiceProxyFactory,
                    actorProxyFactory: _fabricRuntime.ActorProxyFactory,
                    nodeContext: _fabricRuntime.BuildNodeContext(),
                    statefulServiceContext: serviceContext,
                    actorTypeInfo: actorTypeInformation,
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

        private object TryCreateDynamicProxy(Type actorInterface, object target)
        {
            var methodInfo = this.GetType().GetMethod(nameof(CreateDynamicProxy), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethodInfo = methodInfo?.MakeGenericMethod(actorInterface);
            var result = genericMethodInfo?.Invoke(this, new[] {target});
            return result;
        }

        private object CreateDynamicProxy<TActorInterface>(object target) where TActorInterface : class, IActor
        {
            var specificTarget = target as TActorInterface;
            var proxy = DynamicObjectProxy.ObjectProxyFactory.Configure<TActorInterface>(specificTarget)
                       .AddPreDecoration(ctx => { if (ctx.CallCtx.MethodBase.DeclaringType != typeof(object)) PreActorMethod(ctx.Target, ctx.CallCtx.MethodName); })
                       .AddPostDecoration(ctx => { if (ctx.CallCtx.MethodBase.DeclaringType != typeof(object)) PostActorMethod(ctx.Target, ctx.CallCtx.MethodName); })
                       .CreateProxy();
            return proxy;
        }

        private async Task<TActorInterface> Create<TActorInterface>(ActorId actorId)
            where TActorInterface : IActor 
        {
            var actorRegistration = _actorRegistrations.FirstOrDefault(ar => ar.InterfaceType == typeof(TActorInterface));
            if (actorRegistration == null)
            {
                throw new ArgumentException($"Expected a MockableActorRegistration for the type {typeof(TActorInterface).Name}");
            }

            if (actorRegistration.ImplementationType != null && actorRegistration.ImplementationType.IsSubclassOf(typeof(Actor)))
            {
                var actorServiceName = $"{actorRegistration.ImplementationType.Name}Service";

                var createStateProvider = actorRegistration.CreateStateProvider ?? (() => (IActorStateProvider)new MockActorStateProvider(_fabricRuntime));
                var actorStateProvider = GetOrCreateActorStateProvider(actorRegistration.ImplementationType, () => createStateProvider());

                var actorTypeInformation = ActorTypeInformation.Get(actorRegistration.ImplementationType);
                var statefulServiceContext = _fabricRuntime.BuildStatefulServiceContext(actorServiceName);
                var stateManagerFactory = actorRegistration.CreateStateManager != null ? (Func<ActorBase, IActorStateProvider, IActorStateManager>) (
                    (actor, stateProvider) => actorRegistration.CreateStateManager(actor, stateProvider)) : null;
                var actorServiceFactory = actorRegistration.CreateActorService ?? GetMockActorService;

                var actorService = actorServiceFactory(statefulServiceContext, actorTypeInformation, actorStateProvider, stateManagerFactory);
                if (actorService is FG.ServiceFabric.Actors.Runtime.ActorService)
                {
                    actorService.SetPrivateField("_serviceProxyFactory", _fabricRuntime.ServiceProxyFactory);
                    actorService.SetPrivateField("_actorProxyFactory", _fabricRuntime.ActorProxyFactory);
                    actorService.SetPrivateField("_applicationUriBuilder", _fabricRuntime.ApplicationUriBuilder);
                }

                var target = actorRegistration.Activator(actorService, actorId);

                var mockableTarget = (FG.ServiceFabric.Actors.Runtime.ActorBase)(target as FG.ServiceFabric.Actors.Runtime.ActorBase);
                if (mockableTarget != null)
                {
                    mockableTarget.SetPrivateField("_serviceProxyFactory", _fabricRuntime.ServiceProxyFactory);
                    mockableTarget.SetPrivateField("_actorProxyFactory", _fabricRuntime.ActorProxyFactory);
                    mockableTarget.SetPrivateField("_applicationUriBuilder", _fabricRuntime.ApplicationUriBuilder);
                }

                target.CallPrivateMethod("OnActivateAsync");

                await actorStateProvider.ActorActivatedAsync(actorId);

                var proxy = TryCreateDynamicProxy(typeof(TActorInterface), target);
                _actorProxies.Add(proxy.GetHashCode(), target);

                return (TActorInterface)proxy;
            }
            else
            {
                var target = actorRegistration.Activator(null, actorId);
                return (TActorInterface)target;
            }
        }

        public TActor GetActor<TActor>(IActor proxy)
            where TActor : class 
        {
            if (_actorProxies.ContainsKey(proxy.GetHashCode()))
            {
                return _actorProxies[proxy.GetHashCode()] as TActor;
            }
            return null;
        }        

        private void PreActorMethod(IActor proxy, string method)
        {
            var actor = GetActor<Actor>(proxy);
            if (actor == null) return;

            Console.WriteLine();
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            var message = $"Actor {actor?.GetType().Name} '{actor?.Id}'({actor?.GetHashCode()}) {method} activating";
            Console.WriteLine($"{message.PadRight(80, '=')}");
            Console.ForegroundColor = color;
        }

        private void PostActorMethod(IActor proxy, string method)
        {
            var actor = GetActor<Actor>(proxy);
            if (actor == null) return;

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            var message = $"Actor {actor?.GetType().Name} '{actor?.Id}'({actor?.GetHashCode()}) {method} terminating";
            Console.WriteLine($"{message.PadRight(80, '=')}");
            Console.ForegroundColor = color;
            Console.WriteLine();

            var saveStateTask = actor.StateManager.SaveStateAsync(CancellationToken.None);
            saveStateTask.Wait(CancellationToken.None);
        }

        public void AddActorRegistration(IMockableActorRegistration actorRegistration)
        {
            _actorRegistrations.Add(actorRegistration);
        }

        public TActorInterface CreateActorProxy<TActorInterface>(ActorId actorId, string applicationName = null, string serviceName = null, string listenerName = null) where TActorInterface : IActor
        {
            var task = Create<TActorInterface>(actorId);
            task.Wait();
            return task.Result;
        }

        public TActorInterface CreateActorProxy<TActorInterface>(Uri serviceUri, ActorId actorId, string listenerName = null) where TActorInterface : IActor
        {
            var task = Create<TActorInterface>(actorId);
            task.Wait();
            return task.Result;
        }

        private TServiceInterface CreateActorService<TServiceInterface>() where TServiceInterface : IService
        {
            var actorRegistration = _actorRegistrations.FirstOrDefault(
                registration => GetActorServiceImplementationType<TServiceInterface>(registration).Any());
            
            if (actorRegistration == null)
            {
                throw new ArgumentException(
                    $"Expected a MockableActorRegistration with ActorServiceType for the type {typeof(TServiceInterface).Name}");
            }

            var createStateProvider = actorRegistration.CreateStateProvider ??
                                      (() => (IActorStateProvider) new MockActorStateProvider(_fabricRuntime));
            
            var actorServiceName = GetActorServiceImplementationType<TServiceInterface>(actorRegistration).First().Name; //todo: actor service can be named differently
            var actorStateProvider = GetOrCreateActorStateProvider(actorRegistration.ImplementationType,
                () => createStateProvider());

            var actorTypeInformation = ActorTypeInformation.Get(actorRegistration.ImplementationType);
            var statefulServiceContext = _fabricRuntime.BuildStatefulServiceContext(actorServiceName);
            var stateManagerFactory = actorRegistration.CreateStateManager != null
                ? (Func<ActorBase, IActorStateProvider, IActorStateManager>) (
                    (actor, stateProvider) => actorRegistration.CreateStateManager(actor, stateProvider))
                : null;
            var actorServiceFactory = actorRegistration.CreateActorService ?? GetMockActorService;

            var actorService = actorServiceFactory(statefulServiceContext, actorTypeInformation, actorStateProvider,
                stateManagerFactory);
            if (actorService is FG.ServiceFabric.Actors.Runtime.ActorService)
            {
                actorService.SetPrivateField("_serviceProxyFactory", _fabricRuntime.ServiceProxyFactory);
                actorService.SetPrivateField("_actorProxyFactory", _fabricRuntime.ActorProxyFactory);
                actorService.SetPrivateField("_applicationUriBuilder", _fabricRuntime.ApplicationUriBuilder);
            }

            return (TServiceInterface) (object) actorService;
        }

        private static IEnumerable<Type> GetActorServiceImplementationType<TServiceInterface>(IMockableActorRegistration registration) where TServiceInterface : IService
        {
            return registration.GetType().GetGenericArguments().Where(a => a.ImplementsInterface(typeof(TServiceInterface)));
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri actorServiceUri, ActorId actorId, string listenerName = null) where TServiceInterface : IService
        {
            return CreateActorService<TServiceInterface>();
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, long partitionKey,
            string listenerName = null) where TServiceInterface : IService
        {
            return CreateActorService<TServiceInterface>();
        }
    }
}