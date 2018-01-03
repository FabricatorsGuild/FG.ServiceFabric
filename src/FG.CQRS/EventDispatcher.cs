using System;
using System.Collections.Generic;
using FG.CQRS.Exceptions;

namespace FG.CQRS
{
    public class EventDispatcher<TEvent>
        where TEvent : class, IDomainEvent
    {
        private readonly List<KeyValuePair<Type, Action<object>>> _handlers =
            new List<KeyValuePair<Type, Action<object>>>();

        public RegistrationBuilder RegisterHandlers()
        {
            return new RegistrationBuilder(this);
        }

        private Action<object>[] GetHandlers(Type type)
        {
            var result = new List<Action<object>>();
            foreach (var t in _handlers)
                if (t.Key.IsAssignableFrom(type))
                    result.Add(t.Value);

            return result.ToArray();
        }

        public void Dispatch(TEvent evt)
        {
            var handlers = GetHandlers(evt.GetType());

            if (handlers.Length == 0)
                throw new HandlerNotFoundException($"No handler found for event {evt.GetType()}.");

            for (var i = 0; i < handlers.Length; i++)
                handlers[i](evt);
        }

        public class RegistrationBuilder
        {
            private readonly EventDispatcher<TEvent> _dispather;

            public RegistrationBuilder(EventDispatcher<TEvent> dispather)
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

                _dispather._handlers.Add(
                    new KeyValuePair<Type, Action<object>>(eventType, e => handler((THandledEvent) e)));
                return this;
            }
        }
    }
}