using System;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Diagnostics.Tracing;

namespace FG.ServiceFabric.Tests.Actor
{
    [EventSource(Name = "FFCG-ServiceFabric-Demo-ActorDemo")]
    internal sealed class ActorDemoEventSource : EventSource, IServiceEventSource//, IActorEventSource
    {
        public static readonly ActorDemoEventSource Current = new ActorDemoEventSource();

        static ActorDemoEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { });
        }

        // Instance constructor is private to enforce singleton semantics
        private ActorDemoEventSource() : base() { }

        #region Keywords
        // Event keywords can be used to categorize events. 
        // Each keyword is a bit flag. A single event can be associated with multiple keywords (via EventAttribute.Keywords property).
        // Keywords must be defined as a public class named 'Keywords' inside EventSource that uses them.
        public static class Keywords
        {
            public const EventKeywords HostInitialization = (EventKeywords)0x1L;
            public const EventKeywords ServiceReplicaListener = (EventKeywords)0x2L;
            public const EventKeywords ActorImplementation = (EventKeywords)0x4L;
        }
        #endregion

        #region Events

        [NonEvent]
        public void ServiceMessage(ServiceContext serviceContext, string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                ServiceMessage(serviceContext, finalMessage);
            }
        }

        [NonEvent]
        public void ServiceMessage(ServiceContext serviceContext, string message)
        {
            if (this.IsEnabled())
            {
                ServiceMessage(
                    serviceContext.ServiceName.ToString(),
                    serviceContext.ServiceTypeName,
                    serviceContext.ReplicaOrInstanceId,
                    serviceContext.PartitionId,
                    serviceContext.CodePackageActivationContext.ApplicationName,
                    serviceContext.CodePackageActivationContext.ApplicationTypeName,
                    serviceContext.NodeContext.NodeName,
                    message);
            }
        }

        private const int ServiceMessageEventId = 2;
        [Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
        private void ServiceMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message)
        {
            WriteEvent(
                ServiceMessageEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                message);
        }

        private const int ActorHostInitializationFailedEventId = 3;
        [Event(ActorHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Actor host initialization failed", Keywords = Keywords.HostInitialization)]
        public void ActorHostInitializationFailed(string exception)
        {
            WriteEvent(ActorHostInitializationFailedEventId, exception);
        }

        [NonEvent]
        public void ServiceReplicaListenerMessage(ServiceContext serviceContext, string message)
        {
            if (this.IsEnabled())
            {
                ServiceReplicaListenerMessage(
                    serviceContext.ServiceName.ToString(),
                    serviceContext.ServiceTypeName,
                    serviceContext.ReplicaOrInstanceId,
                    serviceContext.PartitionId,
                    serviceContext.CodePackageActivationContext.ApplicationName,
                    serviceContext.CodePackageActivationContext.ApplicationTypeName,
                    serviceContext.NodeContext.NodeName,
                    message);
            }
        }

        private const int ServiceReplicaListenerMessageEventId = 4;
        [Event(ServiceReplicaListenerMessageEventId, Level = EventLevel.Informational, Message = "{7}", Keywords = Keywords.ServiceReplicaListener)]
        private void ServiceReplicaListenerMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message)
        {
            WriteEvent(
                ServiceReplicaListenerMessageEventId, 
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                message);
        }

        [NonEvent]
        public void ServiceReplicaListenerError(ServiceContext serviceContext, string message, Exception exception)
        {
            if (this.IsEnabled())
            {
                var exceptionMessage = exception.ToString();
                ServiceReplicaListenerError(
                    serviceContext.ServiceName.ToString(),
                    serviceContext.ServiceTypeName,
                    serviceContext.ReplicaOrInstanceId,
                    serviceContext.PartitionId,
                    serviceContext.CodePackageActivationContext.ApplicationName,
                    serviceContext.CodePackageActivationContext.ApplicationTypeName,
                    serviceContext.NodeContext.NodeName,
                    message,
                    exceptionMessage);
            }
        }

        private const int ServiceReplicaListenerErrorEventId = 5;
        [Event(ServiceReplicaListenerErrorEventId, Level = EventLevel.Error, Message = "{7}", Keywords = Keywords.ServiceReplicaListener)]
        private void ServiceReplicaListenerError(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message, 
            string exception)
        {
            WriteEvent(
                ServiceReplicaListenerErrorEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                message,
                exception);
        }


        [NonEvent]
        public void ActorMessage(Microsoft.ServiceFabric.Actors.Runtime.Actor actor, string message)
        {
            if (this.IsEnabled())
            {
                ActorMessage(
                    actor.ActorService.Context.ServiceName.ToString(),
                    actor.ActorService.Context.ServiceTypeName,
                    actor.ActorService.Context.ReplicaOrInstanceId,
                    actor.ActorService.Context.PartitionId,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
                    actor.ActorService.Context.NodeContext.NodeName,
                    actor.Id.ToString(),
                    actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
                    message);
            }
        }

        private const int ActorMessageEventId = 6;
        [Event(ActorMessageEventId, Level = EventLevel.Informational, Message = "{9}", Keywords = Keywords.ActorImplementation)]
        private void ActorMessage(
                string serviceName,
                string serviceTypeName,
                long replicaOrInstanceId,
                Guid partitionId,
                string applicationName,
                string applicationTypeName,
                string nodeName,
                string actorId,
                string actorType,
                string message)
        {
            WriteEvent(
                ActorMessageEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                actorId,
                actorType,
                message);
        }

        [NonEvent]
        public void ActorError(Microsoft.ServiceFabric.Actors.Runtime.Actor actor, string message, Exception exception)
        {
            if (this.IsEnabled())
            {
                var exceptionMessage = exception.ToString();
                ActorError(
                    actor.ActorService.Context.ServiceName.ToString(),
                    actor.ActorService.Context.ServiceTypeName,
                    actor.ActorService.Context.ReplicaOrInstanceId,
                    actor.ActorService.Context.PartitionId,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
                    actor.ActorService.Context.NodeContext.NodeName,
                    actor.Id.ToString(),
                    actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
                    message,
                    exceptionMessage);
            }
        }

        private const int ActorErrorEventId = 7;
        [Event(ActorErrorEventId, Level = EventLevel.Error, Message = "{9} - {10}", Keywords = Keywords.ActorImplementation)]
        private void ActorError(
                string serviceName,
                string serviceTypeName,
                long replicaOrInstanceId,
                Guid partitionId,
                string applicationName,
                string applicationTypeName,
                string nodeName,
                string actorId,
                string actorType,
                string message,
                string exception)
        {
            WriteEvent(
                ActorErrorEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                actorId,
                actorType,
                message,
                exception);
        }

        [NonEvent]
        public void ActorDemoCountSet(Microsoft.ServiceFabric.Actors.Runtime.Actor actor, int count)
        {
            if (this.IsEnabled())
            {
                ActorDemoCountSet(
                    actor.ActorService.Context.ServiceName.ToString(),
                    actor.ActorService.Context.ServiceTypeName,
                    actor.ActorService.Context.ReplicaOrInstanceId,
                    actor.ActorService.Context.PartitionId,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
                    actor.ActorService.Context.NodeContext.NodeName,
                    actor.Id.ToString(),
                    actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
                    $"ActorDemo count set {count}",
                    count);
            }
        }

        private const int ActorDemoCountSetEventId = 8;
        [Event(ActorDemoCountSetEventId, Level = EventLevel.Informational, Message = "{9}", Keywords = Keywords.ActorImplementation)]
        private void ActorDemoCountSet(
                string serviceName,
                string serviceTypeName,
                long replicaOrInstanceId,
                Guid partitionId,
                string applicationName,
                string applicationTypeName,
                string nodeName,
                string actorId,
                string actorType,
                string message,
                int count)
        {
            WriteEvent(
                ActorDemoCountSetEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                actorId,
                actorType,
                message,
                count);
        }

        [NonEvent]
        public void ActorDemoCountUpdated(Microsoft.ServiceFabric.Actors.Runtime.Actor actor, int count)
        {
            if (this.IsEnabled())
            {
                ActorDemoCountUpdated(
                    actor.ActorService.Context.ServiceName.ToString(),
                    actor.ActorService.Context.ServiceTypeName,
                    actor.ActorService.Context.ReplicaOrInstanceId,
                    actor.ActorService.Context.PartitionId,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
                    actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
                    actor.ActorService.Context.NodeContext.NodeName,
                    actor.Id.ToString(),
                    actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
                    $"ActorDemo count updated {count}",
                    count);
            }
        }

        private const int ActorDemoCountUpdatedEventId = 9;
        [Event(ActorDemoCountUpdatedEventId, Level = EventLevel.Informational, Message = "{9}", Keywords = Keywords.ActorImplementation)]
        private void ActorDemoCountUpdated(
                string serviceName,
                string serviceTypeName,
                long replicaOrInstanceId,
                Guid partitionId,
                string applicationName,
                string applicationTypeName,
                string nodeName,
                string actorId,
                string actorType,
                string message,
                int count)
        {
            WriteEvent(
                ActorDemoCountUpdatedEventId,
                serviceName,
                serviceTypeName,
                replicaOrInstanceId,
                partitionId,
                applicationName,
                applicationTypeName,
                nodeName,
                actorId,
                actorType,
                message,
                count);
        }
        #endregion
    }
}
