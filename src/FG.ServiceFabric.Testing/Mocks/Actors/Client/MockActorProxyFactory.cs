using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.State;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Fabric;
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
	public class MockActorProxyFactory : IActorProxyFactory, IMockActorProxyManager, IMockServiceProxyManager
	{
		private readonly IDictionary<object, object> _actorProxies;
		private readonly IDictionary<Type, IDictionary<Guid, IActorStateProvider>> _actorStateProviders;
		private readonly MockFabricRuntime _fabricRuntime;

		internal MockActorProxyFactory(
			MockFabricRuntime fabricRuntime,
			params IMockableActorRegistration[] actorRegistrations
		)
		{
			_fabricRuntime = fabricRuntime;
			_actorStateProviders = new Dictionary<Type, IDictionary<Guid, IActorStateProvider>>();
			_actorProxies = new Dictionary<object, object>();
		}

		public TActorInterface CreateActorProxy<TActorInterface>(ActorId actorId, string applicationName = null,
			string serviceName = null, string listenerName = null)
			where TActorInterface : IActor
		{
			var task = Create<TActorInterface>(actorId);
			task.Wait();
			return task.Result;
		}

		public TActorInterface CreateActorProxy<TActorInterface>(Uri serviceUri, ActorId actorId, string listenerName = null)
			where TActorInterface : IActor
		{
			var task = Create<TActorInterface>(actorId);
			task.Wait();
			return task.Result;
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

		void IMockActorProxyManager.BeforeMethod(IActor proxy, MethodInfo method)
		{
			var actor = GetActor<Actor>(proxy);
			if (actor == null) return;

			var actorMethodContext =
				ReflectionUtils.ActivateInternalCtor<ActorMethodContext>(method.Name, ActorCallType.ActorInterfaceMethod);
			actor.CallPrivateMethod("OnPreActorMethodAsync", actorMethodContext);

			if (!_fabricRuntime.DisableMethodCallOutput)
			{
				Console.WriteLine();
				var color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Green;
				var message = $"Actor {actor?.GetType().Name} '{actor?.Id}'({actor?.GetHashCode()}) {method} activating";
				Console.WriteLine($"{message.PadRight(80, '=')}");
				Console.ForegroundColor = color;
			}
		}

		void IMockActorProxyManager.AfterMethod(IActor proxy, MethodInfo method)
		{
			var actor = GetActor<Actor>(proxy);
			if (actor == null) return;

			var actorMethodContext =
				ReflectionUtils.ActivateInternalCtor<ActorMethodContext>(method.Name, ActorCallType.ActorInterfaceMethod);
			actor.CallPrivateMethod("OnPostActorMethodAsync", actorMethodContext);

			if (!_fabricRuntime.DisableMethodCallOutput)
			{
				var color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				var message = $"Actor {actor?.GetType().Name} '{actor?.Id}'({actor?.GetHashCode()}) {method} terminating";
				Console.WriteLine($"{message.PadRight(80, '=')}");
				Console.ForegroundColor = color;
				Console.WriteLine();
			}

			var saveStateTask = actor.StateManager.SaveStateAsync(CancellationToken.None);
			saveStateTask.Wait(CancellationToken.None);
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
				var migrationContainer = (IMigrationContainer) Activator.CreateInstance(migrationContainerType);
				actorStateProvider = new VersionedStateProvider(actorStateProvider, migrationContainer);
			}

			var partitionId = actorRegistation.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);
			IDictionary<Guid, IActorStateProvider> stateProvidersForPartition = null;
			if (_actorStateProviders.ContainsKey(actorImplementationType))
			{
				stateProvidersForPartition = _actorStateProviders[actorImplementationType];
			}
			else
			{
				stateProvidersForPartition = new Dictionary<Guid, IActorStateProvider>();
				_actorStateProviders.Add(actorImplementationType, stateProvidersForPartition);
			}

			if (stateProvidersForPartition.ContainsKey(partitionId))
			{
				return stateProvidersForPartition[partitionId];
			}

			stateProvidersForPartition.Add(partitionId, actorStateProvider);
			return actorStateProvider;
		}

		private IActorStateProvider GetActorStateProvider(IMockableActorRegistration actorRegistation,
			ServicePartitionKey partitionKey)
		{
			var actorImplementationType = actorRegistation.ImplementationType;

			var partitionId = actorRegistation.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);
			if (_actorStateProviders.ContainsKey(actorImplementationType))
			{
				var stateProvidersForPartition = _actorStateProviders[actorImplementationType];
				if (stateProvidersForPartition.ContainsKey(partitionId))
				{
					return stateProvidersForPartition[partitionId];
				}
			}
			return null;
		}

		private ActorService GetMockActorService(
			StatefulServiceContext serviceContext,
			ActorTypeInformation actorTypeInformation,
			IActorStateProvider actorStateProvider,
			Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
		{
			return (ActorService) new MockActorService(
				codePackageActivationContext: serviceContext.CodePackageActivationContext,
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

		private async Task<TActorInterface> Create<TActorInterface>(ActorId actorId)
			where TActorInterface : IActor
		{
			var partitionKey = new ServicePartitionKey(actorId.GetPartitionKey());

			var actorInterfaceType = typeof(TActorInterface);
			var instance =
				_fabricRuntime.Instances.SingleOrDefault(i => i.Equals(actorInterfaceType, partitionKey)) as
					MockActorServiceInstance;

			var actorRegistration = instance?.ActorRegistration;
			if (actorRegistration == null)
			{
				throw new ArgumentException($"Expected a MockableActorRegistration for the type {typeof(TActorInterface).Name}");
			}

			if (actorRegistration.ImplementationType != null && actorRegistration.ImplementationType.IsSubclassOf(typeof(Actor)))
			{
				var actorService = instance.ServiceInstance as ActorService;
				object target = null;
				var firstActivation = true;
				if (instance.Actors.ContainsKey(actorId))
				{
					target = instance.Actors[actorId];
					firstActivation = false;
				}
				else if (actorRegistration.Activator != null)
				{
					target = actorRegistration.Activator(actorService, actorId);
					instance.Actors[actorId] = (Actor) target;
				}
				else
				{
					target = ActivateActorWithReflection(actorRegistration.ImplementationType, actorService, actorId);
					instance.Actors[actorId] = (Actor) target;
				}

				if (target == null)
				{
					throw new NotSupportedException(
						$"Failed to activate an instance of Actor {actorRegistration.ImplementationType.Name} for ActorId {actorId}");
				}

				var mockableTarget =
					(FG.ServiceFabric.Actors.Runtime.ActorBase) (target as FG.ServiceFabric.Actors.Runtime.ActorBase);
				if (mockableTarget != null)
				{
					var applicationName = actorService.Context.CodePackageActivationContext.ApplicationName;
					var applicationUriBuilder =
						new ApplicationUriBuilder(_fabricRuntime.GetCodePackageContext(applicationName), applicationName);

					mockableTarget.SetPrivateField("_serviceProxyFactoryFactory",
						(Func<IServiceProxyFactory>) (() => _fabricRuntime.ServiceProxyFactory));
					mockableTarget.SetPrivateField("_actorProxyFactoryFactory",
						(Func<IActorProxyFactory>) (() => _fabricRuntime.ActorProxyFactory));
					mockableTarget.SetPrivateField("_applicationUriBuilder", applicationUriBuilder);
				}

				if (firstActivation)
				{
					target.CallPrivateMethod("OnActivateAsync");

					var actorStateProvider = instance.ActorStateProvider;
					await actorStateProvider.ActorActivatedAsync(actorId);
				}

				var serviceUri = instance.ActorRegistration.ServiceRegistration.ServiceUri;
				var actorProxy = new MockActorProxy(
					target, typeof(TActorInterface), actorId, serviceUri, ServicePartitionKey.Singleton, TargetReplicaSelector.Default,
					"", null, this);
				var proxy = actorProxy.Proxy;

				_actorProxies.Add(proxy.GetHashCode(), target);

				return (TActorInterface) proxy;
			}
			else
			{
				var target = actorRegistration.Activator(null, actorId);
				return (TActorInterface) target;
			}
		}

		private TActor GetActor<TActor>(IActor proxy)
			where TActor : class
		{
			if (_actorProxies.ContainsKey(proxy.GetHashCode()))
			{
				return _actorProxies[proxy.GetHashCode()] as TActor;
			}
			return null;
		}

		private TServiceInterface CreateActorService<TServiceInterface>(Uri serviceUri, ServicePartitionKey partitionKey)
			where TServiceInterface : IService
		{
			var serviceInterfaceType = typeof(TServiceInterface);
			var instance =
				_fabricRuntime.Instances.SingleOrDefault(i => i.Equals(serviceUri, serviceInterfaceType, partitionKey)) as
					MockActorServiceInstance;

			var actorRegistration = instance?.ActorRegistration;
			if (actorRegistration == null)
			{
				throw new ArgumentException(
					$"Expected a MockableActorRegistration with ActorServiceType for the type {typeof(TServiceInterface).Name}");
			}

			var mockServiceProxy = new MockServiceProxy(instance.ServiceInstance, serviceUri, serviceInterfaceType, partitionKey,
				TargetReplicaSelector.Default, "", null, this);
			return (TServiceInterface) mockServiceProxy.Proxy;
		}

		internal Actor ActivateActorWithReflection(Type actorType, ActorService actorService, ActorId actorId)
		{
			if (!typeof(Actor).IsAssignableFrom(actorType))
			{
				throw new ArgumentException(
					$"Cannot activate instance of {actorType?.FullName} as it should inherit from {typeof(Actor).FullName}");
			}

			var instance = (Actor) actorType.ActivateCtor(new object[] {actorService, actorId});
			return instance;
		}
	}
}