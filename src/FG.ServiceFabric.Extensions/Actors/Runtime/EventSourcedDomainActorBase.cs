//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using FG.Common.Async;
//using FG.Common.Domain;
//using Microsoft.ServiceFabric.Actors;

//namespace FG.ServiceFabric.Actors.Runtime
//{
//    public abstract class EventSourcedDomainActorBase<TDomainAggregateRoot, TDomainEventStream> : ActorBase, IDomainEventController
//        where TDomainEventStream : IDomainEventStream, new()
//        where TDomainAggregateRoot : class, IDomainAggregateRoot, new()
//    {
//        protected const string CoreStateName = @"state";
//        protected TDomainAggregateRoot DomainState = null;

//        protected EventSourcedDomainActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId) : base(actorService, actorId)
//        {
//        }

//        protected Task<TDomainAggregateRoot> GetAndSetDomainAsync()
//        {
//            if (DomainState != null) return Task.FromResult(DomainState);

//            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
//            {
//                var eventStream = await this.StateManager.GetOrAddStateAsync<TDomainEventStream>(CoreStateName, new TDomainEventStream(), ct);
//                DomainState = new TDomainAggregateRoot();
//                DomainState.Initialize(this, eventStream.DomainEvents);
//                return DomainState;
//            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
//        }

//        protected Task<TDomainAggregateRoot> GetDomainAsync(DateTime latestPointInTime)
//        {
//            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
//            {
//                var eventStream = await this.StateManager.GetOrAddStateAsync<TDomainEventStream>(CoreStateName, new TDomainEventStream(), ct);
//                var domainStateAtPointInTime = new TDomainAggregateRoot();
//                domainStateAtPointInTime.Initialize(this, eventStream.DomainEvents.Where(e => e.UtcTimeStamp <= latestPointInTime).ToArray());
//                return domainStateAtPointInTime;
//            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
//        }

//        protected Task StoreDomainEventAsync(IDomainEvent domainEvent)
//        {
//            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
//            {
//                var eventStream = await this.StateManager.GetOrAddStateAsync<TDomainEventStream>(CoreStateName, new TDomainEventStream(), ct);
//                eventStream.Append(domainEvent);
//                await this.StateManager.SetStateAsync(CoreStateName, eventStream, ct);
//                return Task.FromResult(true);
//            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
//        }

//        public async Task RaiseDomainEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent
//        {
//            // ReSharper disable once SuspiciousTypeConversion.Global
//            var handleDomainEvent = this as IHandleDomainEvent<TDomainEvent>;
//            if (handleDomainEvent == null)
//            {
//                throw DomainEventException.UnhandledDomainEvent(domainEvent);
//            }

//            await handleDomainEvent.Handle(domainEvent);
//        }

//        public Task<TRequestValue> Request<TRequestValue, TDomainRequest>(TDomainRequest domainRequest) where TDomainRequest : IDomainRequest<TRequestValue>
//        {
//            // ReSharper disable once SuspiciousTypeConversion.Global
//            var handleDomainRequest = this as IHandleDomainRequest<TRequestValue, TDomainRequest>;
//            if (handleDomainRequest == null)
//            {
//                throw DomainEventException.UnhandledDomainRequest(domainRequest);
//            }

//            var result = handleDomainRequest.Handle(domainRequest);
//            return result;
//        }
//    }
//}