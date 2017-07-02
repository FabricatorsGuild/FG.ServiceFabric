using System;
using System.Collections.Generic;
using FG.ServiceFabric.CQRS.Exceptions;

namespace FG.ServiceFabric.CQRS
{
    public class DomainEventDispatcher<TEvent>
        where TEvent : class, IDomainEvent
    {
        private readonly List<KeyValuePair<Type, Action<object>>> _handlers = new List<KeyValuePair<Type, Action<object>>>();
        
        public RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder(this);
        }
        
        public class RegistrationBuilder : IEventHandlerRegistrar<TEvent>
        {
            private readonly DomainEventDispatcher<TEvent> _dispather;

            public RegistrationBuilder(DomainEventDispatcher<TEvent> dispather)
            {
                _dispather = dispather;
            }
            
            public RegistrationBuilder For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TEvent
            {
                return ForGenericEvent(handler);
            }
            
            private RegistrationBuilder ForGenericEvent<THandledEvent>(Action<THandledEvent> handler)
            {
                var eventType = typeof(THandledEvent);

                _dispather._handlers.Add(new KeyValuePair<Type, Action<object>>(eventType, e => handler((THandledEvent)e)));
                return this;
            }
            
            IEventHandlerRegistrar<TEvent> IEventHandlerRegistrar<TEvent>.For<THandledEvent>(Action<THandledEvent> handler)
            {
                return For(handler);
            }
        }
        
        private Action<object>[] GetHandlers(Type type)
        {
            var result = new List<Action<object>>();
            foreach(KeyValuePair<Type, Action<object>> t in _handlers) {
                if (t.Key.IsAssignableFrom(type))
                {
                    result.Add(t.Value);
                }
            }
            
            return result.ToArray();
        }

        public void Dispatch(TEvent evt)
        {
            var handlers = GetHandlers(evt.GetType());

            if(handlers.Length == 0)
            {
                throw new EventHandlerNotFoundException($"No handler found for event {evt.GetType()}.");
            }

            for (var i = 0; i < handlers.Length; i++)
            {
                handlers[i](evt);
            }
        }
    }
    
    public interface IEventHandlerRegistrar<in TBaseEvent>
        where TBaseEvent : class
    {
        IEventHandlerRegistrar<TBaseEvent> For<THandledEvent>(Action<THandledEvent> handler) where THandledEvent : TBaseEvent;
    }
}
