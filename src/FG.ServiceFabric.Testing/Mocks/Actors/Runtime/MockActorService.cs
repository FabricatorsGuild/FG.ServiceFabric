using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    public class MockActorService : ActorService
    {
        private readonly IActorProxyFactory _actorProxyFactory;
        private readonly ICodePackageActivationContext _codePackageActivationContext;
        private readonly NodeContext _nodeContext;
        private readonly IServiceProxyFactory _serviceProxyFactory;

        public MockActorService(
            ICodePackageActivationContext codePackageActivationContext,
            IServiceProxyFactory serviceProxyFactory,
            IActorProxyFactory actorProxyFactory,
            NodeContext nodeContext,
            StatefulServiceContext statefulServiceContext,
            ActorTypeInformation actorTypeInfo,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider,
                IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null
        ) :
            base(
                statefulServiceContext,
                actorTypeInfo,
                actorFactory,
                stateManagerFactory,
                stateProvider,
                settings)
        {
            _codePackageActivationContext = codePackageActivationContext;
            _serviceProxyFactory = serviceProxyFactory;
            _actorProxyFactory = actorProxyFactory;
            _nodeContext = nodeContext;
        }

        private Task DeleteActorAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            return StateProvider.RemoveActorAsync(actorId, cancellationToken);
        }
    }

    public class MockActorServiceExtension : IActorService
    {
        private readonly ActorService _actorService;

        public MockActorServiceExtension(ActorService actorService)
        {
            _actorService = actorService;
        }

        public Task<PagedResult<ActorInformation>> GetActorsAsync(ContinuationToken continuationToken,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteActorAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            return _actorService.StateProvider.RemoveActorAsync(actorId, cancellationToken);
        }
    }
}