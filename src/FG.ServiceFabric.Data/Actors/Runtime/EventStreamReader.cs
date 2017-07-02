using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.Exceptions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class EventStreamReader<TEventStream> : IEventStreamReader<TEventStream> where TEventStream : IEventStream
    {
        private readonly IActorStateProvider _stateProvider;
        private readonly string _stateKey;

        public EventStreamReader(IActorStateProvider stateProvider, string stateKey)
        {
            _stateProvider = stateProvider;
            _stateKey = stateKey;
        }

        protected async Task<TEventStream> LoadEventStream(Guid id, CancellationToken cancellationToken)
        {
            if(!await _stateProvider.ContainsStateAsync(new ActorId(id), _stateKey, cancellationToken))
            {
                throw new AggregateRootNotFoundException(id);
            }

            return await _stateProvider.LoadStateAsync<TEventStream>(new ActorId(id), _stateKey, cancellationToken);
        }

        public virtual async Task<TEventStream> GetEventStreamAsync(Guid id, CancellationToken cancellationToken)
        {
            return await LoadEventStream(id, cancellationToken);
        }
    }
}