using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    using System.Threading;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Query;

    public class SimpleMockActorService : IActorService
    {


        public async Task<PagedResult<ActorInformation>> GetActorsAsync(ContinuationToken continuationToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteActorAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
