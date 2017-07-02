using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Persistance;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class TempDatabaseStateProvider : WrappedActorStateProvider, IRestorableActorStateProvider, IActorStateProvider
    {
        private readonly Func<IDocumentDbSession> _dbSessionFactory;

        public TempDatabaseStateProvider(ActorTypeInformation actorTypeInfo, Func<IDocumentDbSession> dbSessionFactory, IActorStateProvider stateProvider = null)
            : base(actorTypeInfo, stateProvider)
        {
            _dbSessionFactory = dbSessionFactory;
        }
        
        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //Store unit of work.
            await base.SaveStateAsync(actorId, stateChanges, cancellationToken);
            await _dbSessionFactory().SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        public override async Task<bool> ContainsStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            if (await base.ContainsStateAsync(actorId, stateName, cancellationToken))
                return true;

            if (await HasRestorableStateAsync(actorId, stateName, cancellationToken))
            {
                await RestoreStateAsync(actorId, stateName, cancellationToken);
                return true;
            }

            return false;
        }

        public async Task RestoreStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            var state = await _dbSessionFactory().GetState(actorId, stateName, cancellationToken);
            await SaveStateAsync(actorId, new[] {new ActorStateChange(stateName, state.GetType(), state, StateChangeKind.Add)}, cancellationToken);
        }

        public Task<bool> HasRestorableStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            return _dbSessionFactory().ContainsStateAsync(actorId, stateName, cancellationToken);
        }
    }
    
    [DataContract]
    public abstract class EventStreamBase : IEventStream
    {
        protected EventStreamBase()
        {
            DomainEvents = new IDomainEvent[] {};
        }

        [DataMember]
        public IDomainEvent[] DomainEvents { get; private set; }

        public void Append(IDomainEvent domainEvent)
        {
            DomainEvents = DomainEvents.Union(new[] {domainEvent}).ToArray();
        }

        public void Append(IDomainEvent[] domainEvents)
        {
            DomainEvents = DomainEvents.Union(domainEvents).ToArray();
        }
    }
}