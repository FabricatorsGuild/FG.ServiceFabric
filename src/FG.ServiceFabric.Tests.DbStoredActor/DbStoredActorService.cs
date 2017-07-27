using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    internal class DbStoredActorService : ActorService, IActorService
    {
        private readonly DocumentDbActorStateProvider _stateProvider;

        public DbStoredActorService(
            StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            DocumentDbActorStateProvider stateProvider,
            Func<ActorService, ActorId, Actors.Runtime.ActorBase> actorFactory = null,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
                stateManagerFactory = null, 
            ActorServiceSettings settings = null) :
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            _stateProvider = stateProvider;
            _stateProvider
                .Configure()
                .Replicate<CountState>()
                .Replicate<CountReadModel>();
        }
        
        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            _stateProvider.Dispose();
            return base.OnCloseAsync(cancellationToken);
        }
    }
}