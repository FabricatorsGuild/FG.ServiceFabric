using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public class EventMap
    {
        public EventMap(MethodInfo eventMethod, System.Diagnostics.Tracing.EventAttribute eventAttribute)
        {
            EventId = eventAttribute.EventId;

            EventName = eventMethod.Name;

            var eventParameters = eventMethod.GetParameters().Select((p, i) => new EventParameterMap { Index = i, Name = p.Name, Type = p.ParameterType });

            PayloadNames = new ConcurrentDictionary<int, EventParameterMap>();
            foreach (var eventParameterMap in eventParameters)
            {
                PayloadNames.Add(eventParameterMap.Index, eventParameterMap);
            }
        }

        public int EventId { get; private set; }
        public string EventName { get; private set; }
        public IDictionary<int, EventParameterMap> PayloadNames { get; private set; }
    }
}