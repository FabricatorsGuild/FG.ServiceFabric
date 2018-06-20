using System;
using System.Threading;
using System.Threading.Tasks;
using FG.CQRS;
using FG.CQRS.Exceptions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    using System.Collections.Generic;

    public class EventStreamReader<TEventStream> : IEventStreamReader<TEventStream>
        where TEventStream : IDomainEventStream
    {
        private readonly string _stateKey;
        private readonly IActorStateProvider _stateProvider;

        public EventStreamReader(IActorStateProvider stateProvider, string stateKey)
        {
            _stateProvider = stateProvider;
            _stateKey = stateKey;
        }

        public virtual Task<TEventStream> GetEventStreamAsync(Guid id, CancellationToken cancellationToken)
        {
            return this.LoadEventStream(id, cancellationToken);
        }

        protected async Task<TEventStream> LoadEventStream(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                return await _stateProvider.LoadStateAsync<TEventStream>(new ActorId(id), _stateKey, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (KeyNotFoundException exception)
            {
                throw new AggregateRootNotFoundException(
                    $"Trying to read (state for) nonexisting aggregate (actor) with id: {id}.\r\n This basically means actor state for actor id {id} could not be found.\r\nThe most likely cause of this is invalid partitioning.",
                    exception);
            }
            catch (Exception exception)
            {
                throw new LoadEventStreamException($"Actor state for actor id {id} exists but could not be loaded.", exception);
            }
        }
    }
}