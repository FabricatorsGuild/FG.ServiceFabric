using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public class EventSourceMapper
    {
        private readonly IDictionary<Guid, EventSourceMap> _eventSourceMaps = new ConcurrentDictionary<Guid, EventSourceMap>();

        public EventSourceMap GetEventSourceMap(System.Diagnostics.Tracing.EventSource eventSource)
        {
            if (_eventSourceMaps.ContainsKey(eventSource.Guid))
            {
                return _eventSourceMaps[eventSource.Guid];
            }

            var eventSourceMap = new EventSourceMap(eventSource);
            _eventSourceMaps.Add(eventSource.Guid, eventSourceMap);

            return eventSourceMap;
        }
    }
}