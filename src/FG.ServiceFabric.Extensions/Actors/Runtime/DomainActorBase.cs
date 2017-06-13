//using System.Threading;
//using System.Threading.Tasks;
//using FG.Common.Domain;
//using Microsoft.ServiceFabric.Actors;

//namespace FG.ServiceFabric.Actors.Runtime
//{
//    public abstract class DomainActorBase<TState> : ActorBase, IDomainEventController
//    {
//        protected const string CoreStateName = @"state";

//        protected DomainActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId) : base(actorService, actorId)
//        {
//        }

//        protected async Task<TState> GetState()
//        {
//            return await this.StateManager.GetOrAddStateAsync(CoreStateName, default(TState), CancellationToken.None);
//        }

//        protected async Task SetState(TState state)
//        {
//            await this.StateManager.SetStateAsync(CoreStateName, state, CancellationToken.None);
//        }

//        public Task RaiseDomainEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent
//        {
//            // ReSharper disable once SuspiciousTypeConversion.Global
//            var handleDomainEvent = this as IHandleDomainEvent<TDomainEvent>;
//            if (handleDomainEvent == null)
//            {
//                throw DomainEventException.UnhandledDomainEvent(domainEvent);
//            }

//            handleDomainEvent.Handle(domainEvent);
//            return Task.FromResult(true);
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