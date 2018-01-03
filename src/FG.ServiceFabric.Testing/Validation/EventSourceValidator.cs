using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;

namespace FG.ServiceFabric.Testing.Validation
{
    public static class EventSourceValidator
    {
        public static void Call_from_tests_check_eventsources(Assembly actorOrServiceAssembly)
        {
            foreach (var eventSourceType in actorOrServiceAssembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(EventSource))))
            {
                var eventSource = GetEventSourceAccessor(eventSourceType, "Current") ??
                                  GetEventSourceAccessor(eventSourceType, "Log");
                if (eventSource == null)
                    throw new NotImplementedException(
                        $"Expected EventSource {eventSourceType.Name} to expose a static accessor named 'Current' or 'Log'");
                EventSourceAnalyzer.InspectAll(eventSource);
            }
            foreach (var eventSourceType in actorOrServiceAssembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(Microsoft.Diagnostics.Tracing.EventSource))))
            {
                var eventSource = GetEventSourceAccessor(eventSourceType, "Current") ??
                                  GetEventSourceAccessor(eventSourceType, "Log");
                if (eventSource == null)
                    throw new NotImplementedException(
                        $"Expected EventSource {eventSourceType.Name} to expose a static accessor named 'Current' or 'Log'");
                EventSourceAnalyzer.InspectAll(eventSource);
            }
        }

        private static EventSource GetEventSourceAccessor(Type eventSourceType, string accessorName)
        {
            var accessorMethod =
                eventSourceType.GetField("Current", BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public);
            var accessor = accessorMethod?.GetValue(null) as EventSource;

            return accessor;
        }
    }
}