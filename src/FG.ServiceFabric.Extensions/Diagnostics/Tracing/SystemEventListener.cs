using System.Collections.Generic;
using System.Linq;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public class SystemEventListener : System.Diagnostics.Tracing.EventListener
    {
        private static readonly EventSourceMapper EventSourceMaps = new EventSourceMapper();
        private readonly GenericEventListener _genericEventListener;

        public SystemEventListener(GenericEventListener genericEventListener)
        {
            _genericEventListener = genericEventListener;
        }

        protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs eventData)
        {
            var eventSourceMap = EventSourceMaps.GetEventSourceMap(eventData.EventSource);
            var eventMap = eventSourceMap.GetEvent(eventData.EventId);

            _genericEventListener.OnEvent(
                eventData.Message != null ? string.Format(eventData.Message, eventData.Payload.ToArray()) : null,
                eventData.ActivityId,
                eventData.EventId,
                eventMap.EventName,
                (long) eventData.Keywords,
                eventData.Level,
                eventData.Opcode,
                eventData.Payload.Select((p, i) => new { Index = i, Payload = p }).ToDictionary(p => eventMap.PayloadNames[p.Index].Name, p => p.Payload)
            );
        }
    }
}