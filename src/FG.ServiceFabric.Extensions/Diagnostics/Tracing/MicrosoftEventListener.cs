using System.Collections.Generic;
using System.Linq;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public class MicrosoftEventListener : Microsoft.Diagnostics.Tracing.EventListener
    {
        private readonly GenericEventListener _genericEventListener;

        public MicrosoftEventListener(GenericEventListener genericEventListener)
        {
            _genericEventListener = genericEventListener;
        }

        protected override void OnEventWritten(Microsoft.Diagnostics.Tracing.EventWrittenEventArgs eventData)
        {
            _genericEventListener.OnEvent(
                eventData.Message != null ? string.Format(eventData.Message, eventData.Payload.ToArray()) : null,
                eventData.ActivityId,
                eventData.EventId,
                eventData.EventName,
                (long) eventData.Keywords,
                (System.Diagnostics.Tracing.EventLevel) eventData.Level,
                (System.Diagnostics.Tracing.EventOpcode) eventData.Opcode,
                eventData.Payload.Select((p, i) => new {Index = i, Payload = p}).ToDictionary(p => eventData.PayloadNames[p.Index], p => p.Payload)
            );
        }
    }
}