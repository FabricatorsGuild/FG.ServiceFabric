using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public class EventSourceMap
    {
        public Guid EventSourceGuid { get; private set; }

        private IDictionary<int, EventMap> Events { get; set; }

        public EventSourceMap(System.Diagnostics.Tracing.EventSource eventSource)
        {
            EventSourceGuid = eventSource.Guid;
            Events = new Dictionary<int, EventMap>();
            var eventMethods = eventSource.GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Select(mi => new { EventAttribute = CustomAttributeExtensions.GetCustomAttribute<System.Diagnostics.Tracing.EventAttribute>((MemberInfo) mi), Method = mi })
                .Where(m => m.EventAttribute != null);

            foreach (var eventMethod in eventMethods)
            {
                Events.Add(eventMethod.EventAttribute.EventId, new EventMap(eventMethod.Method, eventMethod.EventAttribute));
            }
        }

        public EventMap GetEvent(int eventId)
        {
            return Events[eventId];
        }
    }
}