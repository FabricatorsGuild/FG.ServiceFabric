namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Query;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public class MockActorServiceExtension : IActorService
    {
        private readonly ActorService _actorService;

        public MockActorServiceExtension(ActorService actorService)
        {
            this._actorService = actorService;
        }

        public Task<PagedResult<ActorInformation>> GetActorsAsync(ContinuationToken continuationToken,
                                                                  CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteActorAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            return this._actorService.StateProvider.RemoveActorAsync(actorId, cancellationToken);
        }
    }
}