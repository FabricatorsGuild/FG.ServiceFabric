using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Diagnostics.Tracing;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Diagnostics.Tracing
{
    //public abstract class ActorEventSourceBase : ServiceEventSourceBase, IActorEventSource
    //{
    //    static ActorEventSourceBase()
    //    {
    //        // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
    //        // This problem will be fixed in .NET Framework 4.6.2.
    //        Task.Run(() => { });
    //    }

    //    // Instance constructor is private to enforce singleton semantics
    //    protected ActorEventSourceBase() : base()
    //    {
    //    }

    //    [NonEvent]
    //    public virtual void ActorMessage(Actor actor, string message)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            ActorMessage(
    //                actor.ActorService.Context.ServiceName.ToString(),
    //                actor.ActorService.Context.ServiceTypeName,
    //                actor.ActorService.Context.ReplicaOrInstanceId,
    //                actor.ActorService.Context.PartitionId,
    //                actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
    //                actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
    //                actor.ActorService.Context.NodeContext.NodeName,
    //                actor.Id.ToString(),
    //                actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
    //                message);
    //        }
    //    }

    //    [Event((int)CoreMessageEvents.ActorMessageEventId, Level = EventLevel.Informational, Message = "{9}")]
    //    private void ActorMessage(
    //        string serviceName,
    //        string serviceTypeName,
    //        long replicaOrInstanceId,
    //        Guid partitionId,
    //        string applicationName,
    //        string applicationTypeName,
    //        string nodeName,
    //        string actorId,
    //        string actorType,
    //        string message)
    //    {
    //        WriteEvent(
    //            (int)CoreMessageEvents.ActorMessageEventId,
    //            serviceName,
    //            serviceTypeName,
    //            replicaOrInstanceId,
    //            partitionId,
    //            applicationName,
    //            applicationTypeName,
    //            nodeName,
    //            actorId,
    //            actorType,
    //            message);
    //    }

    //    [NonEvent]
    //    public virtual void ActorWarning(Actor actor, string message)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            ActorWarning(
    //                actor.ActorService.Context.ServiceName.ToString(),
    //                actor.ActorService.Context.ServiceTypeName,
    //                actor.ActorService.Context.ReplicaOrInstanceId,
    //                actor.ActorService.Context.PartitionId,
    //                actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
    //                actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
    //                actor.ActorService.Context.NodeContext.NodeName,
    //                actor.Id.ToString(),
    //                actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
    //                message);
    //        }
    //    }

    //    [Event((int)CoreMessageEvents.ActorWarningEventId, Level = EventLevel.Warning, Message = "{9}")]
    //    private void ActorWarning(
    //        string serviceName,
    //        string serviceTypeName,
    //        long replicaOrInstanceId,
    //        Guid partitionId,
    //        string applicationName,
    //        string applicationTypeName,
    //        string nodeName,
    //        string actorId,
    //        string actorType,
    //        string message)
    //    {
    //        WriteEvent(
    //            (int) CoreMessageEvents.ActorWarningEventId,
    //            serviceName,
    //            serviceTypeName,
    //            replicaOrInstanceId,
    //            partitionId,
    //            applicationName,
    //            applicationTypeName,
    //            nodeName,
    //            actorId,
    //            actorType,
    //            message);
    //    }

    //    [NonEvent]
    //    public virtual void ActorError(Actor actor, string message, Exception ex)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            ActorError(
    //                actor.ActorService.Context.ServiceName.ToString(),
    //                actor.ActorService.Context.ServiceTypeName,
    //                actor.ActorService.Context.ReplicaOrInstanceId,
    //                actor.ActorService.Context.PartitionId,
    //                actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
    //                actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
    //                actor.ActorService.Context.NodeContext.NodeName,
    //                actor.Id.ToString(),
    //                actor.ActorService.ActorTypeInformation.ImplementationType.ToString(),
    //                message,
    //                ex.ToString());
    //        }
    //    }

    //    [Event((int)CoreMessageEvents.ActorErrorEventId, Level = EventLevel.Error, Message = "{9} - {10}")]
    //    private void ActorError(
    //        string serviceName,
    //        string serviceTypeName,
    //        long replicaOrInstanceId,
    //        Guid partitionId,
    //        string applicationName,
    //        string applicationTypeName,
    //        string nodeName,
    //        string actorId,
    //        string actorType,
    //        string message,
    //        string exception
    //    )
    //    {
    //        WriteEvent(
    //            (int)CoreMessageEvents.ActorErrorEventId,
    //            serviceName,
    //            serviceTypeName,
    //            replicaOrInstanceId,
    //            partitionId,
    //            applicationName,
    //            applicationTypeName,
    //            nodeName,
    //            actorId,
    //            actorType,
    //            message,
    //            exception);
    //    }
    //}
}