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
    public interface IRestorableStateProvider
    {
    }
    
    public class DatabaseStateProvider : WrappedActorStateProvider
    {
        private readonly Func<IDbSession> _dbSessionFactory;

        public DatabaseStateProvider(ActorTypeInformation actorTypeInfo, Func<IDbSession> dbSessionFactory, IActorStateProvider stateProvider = null)
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

            if (await HasRestorableState(actorId, stateName, cancellationToken))
            {
                await RestoreState(actorId, stateName, cancellationToken);
                return true;
            }

            return false;
        }

        private async Task RestoreState(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            var state = await _dbSessionFactory().GetState(actorId, stateName, cancellationToken);
            await SaveStateAsync(actorId, new[] {new ActorStateChange(stateName, state.GetType(), state, StateChangeKind.Add)},
                cancellationToken);
        }

        private Task<bool> HasRestorableState(ActorId actorId, string stateName, CancellationToken cancellationToken)
        {
            return _dbSessionFactory().ContainsStateAsync(actorId, stateName, cancellationToken);
        }

        public override async Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            //todo: restore external state?
            await base.ActorActivatedAsync(actorId, cancellationToken);
        }
    }
    
    [DataContract]
    public abstract class EventStreamBase : IDomainEventStream
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
    }
}