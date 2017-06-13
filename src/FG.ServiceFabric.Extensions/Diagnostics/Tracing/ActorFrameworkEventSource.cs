using System;
using System.Diagnostics.Tracing;
using System.Fabric;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Diagnostics.Tracing
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ActorFrameworkEventSource : EventSource
    {
        public static EventSource Current => GetFrameWorkEventSourceUsingReflection();

        public static Type GetEventSourceType()
        {
            var eventSourceType = typeof(Microsoft.ServiceFabric.Actors.IActor).Assembly.GetType("Microsoft.ServiceFabric.Actors.Diagnostics.ActorFrameworkEventSource");
            return eventSourceType;
        }

        private static EventSource GetFrameWorkEventSourceUsingReflection()
        {

            var eventSourceType = GetEventSourceType();
            var actorFrameworkEventSource = eventSourceType.GetPrivateStaticField<EventSource>("Writer");

            return actorFrameworkEventSource;
        }
    }
}