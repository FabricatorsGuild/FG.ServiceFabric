using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    public abstract class OutputEventListener : GenericEventListener
    {
        protected abstract void WriteLine(string line);

        protected abstract ConsoleColor Color { get; set; }

        public override void OnEvent(string message, Guid activityId, int eventId, string eventName, long keywords, System.Diagnostics.Tracing.EventLevel level, EventOpcode opCode, IDictionary<string, object> payload)
        {
            var color = Color;
            Color = ((int)level < 3) ? ConsoleColor.Red : (((int)level < 4) ? ConsoleColor.Yellow : ConsoleColor.Green);

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"________________________________");
            stringBuilder.AppendLine($"Message: {message}");
            stringBuilder.AppendLine($"ActivityId: {activityId}");
            stringBuilder.AppendLine($"EventId: {eventId}");
            stringBuilder.AppendLine($"EventName: {eventName}");
            stringBuilder.AppendLine($"Keywords: {keywords}");
            stringBuilder.AppendLine($"Level: {level}");
            stringBuilder.AppendLine($"Opcode: {opCode}");

            stringBuilder.AppendLine($"Payload:");
            foreach (var keyValuePair in payload)
            {
                stringBuilder.AppendLine($"\t{keyValuePair.Key}: {keyValuePair.Value}");
            }
            stringBuilder.AppendLine($"________________________________");

            WriteLine(stringBuilder.ToString());
            Color = color;
        }
    }
}