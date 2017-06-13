using System;
using System.Collections.Generic;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public abstract class GenericEventListener
    {
        public abstract void OnEvent(
            string message,
            Guid activityId,
            int eventId,
            string eventName,
            long keywords,
            System.Diagnostics.Tracing.EventLevel level,
            System.Diagnostics.Tracing.EventOpcode opCode,
            IDictionary<string, object> payload);
    }
}