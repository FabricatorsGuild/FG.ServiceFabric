using System;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics.Tracing;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Services.Diagnostics.Tracing
{
    //public abstract class ServiceEventSourceBase : EventSourceBase//, IServiceEventSource
    //{
    //    static ServiceEventSourceBase()
    //    {
    //        // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
    //        // This problem will be fixed in .NET Framework 4.6.2.
    //        Task.Run(() => { });
    //    }

    //    // Instance constructor is private to enforce singleton semantics
    //    protected ServiceEventSourceBase() : base()
    //    {
    //    }

    //    [NonEvent]
    //    public virtual void ServiceMessage(StatefulService service, string message, params object[] args)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            string finalMessage = string.Format(message, args);
    //            ServiceMessage(service.Context, finalMessage);
    //        }
    //    }

    //    [NonEvent]
    //    public virtual void ServiceMessage(ServiceContext serviceContext, string message)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            ServiceMessage(
    //                serviceContext.ServiceName.ToString(),
    //                serviceContext.ServiceTypeName,
    //                serviceContext.ReplicaOrInstanceId,
    //                serviceContext.PartitionId,
    //                serviceContext.CodePackageActivationContext.ApplicationName,
    //                serviceContext.CodePackageActivationContext.ApplicationTypeName,
    //                serviceContext.NodeContext.NodeName,
    //                message);
    //        }
    //    }

    //    [Event((int) CoreMessageEvents.ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
    //    private void ServiceMessage(
    //        string serviceName,
    //        string serviceTypeName,
    //        long replicaOrInstanceId,
    //        Guid partitionId,
    //        string applicationName,
    //        string applicationTypeName,
    //        string nodeName,
    //        string message)
    //    {
    //        WriteEvent(
    //            (int) CoreMessageEvents.ServiceMessageEventId,
    //            serviceName,
    //            serviceTypeName,
    //            replicaOrInstanceId,
    //            partitionId,
    //            applicationName,
    //            applicationTypeName,
    //            nodeName,
    //            message);
    //    }

    //    [NonEvent]
    //    public virtual void ServiceWarning(ServiceContext serviceContext, string message)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            ServiceWarning(
    //                serviceContext.ServiceName.ToString(),
    //                serviceContext.ServiceTypeName,
    //                serviceContext.ReplicaOrInstanceId,
    //                serviceContext.PartitionId,
    //                serviceContext.CodePackageActivationContext.ApplicationName,
    //                serviceContext.CodePackageActivationContext.ApplicationTypeName,
    //                serviceContext.NodeContext.NodeName,
    //                message);
    //        }
    //    }

    //    [Event((int) CoreMessageEvents.ServiceWarningEventId, Level = EventLevel.Warning, Message = "{7}")]
    //    private void ServiceWarning(
    //        string serviceName,
    //        string serviceTypeName,
    //        long replicaOrInstanceId,
    //        Guid partitionId,
    //        string applicationName,
    //        string applicationTypeName,
    //        string nodeName,
    //        string message)
    //    {
    //        WriteEvent(
    //            (int) CoreMessageEvents.ServiceWarningEventId,
    //            serviceName,
    //            serviceTypeName,
    //            replicaOrInstanceId,
    //            partitionId,
    //            applicationName,
    //            applicationTypeName,
    //            nodeName,
    //            message);
    //    }

    //    [NonEvent]
    //    public virtual void ServiceError(ServiceContext serviceContext, string message, Exception ex)
    //    {
    //        if (this.IsEnabled())
    //        {
    //            ServiceError(
    //                serviceContext.ServiceName.ToString(),
    //                serviceContext.ServiceTypeName,
    //                serviceContext.ReplicaOrInstanceId,
    //                serviceContext.PartitionId,
    //                serviceContext.CodePackageActivationContext.ApplicationName,
    //                serviceContext.CodePackageActivationContext.ApplicationTypeName,
    //                serviceContext.NodeContext.NodeName,
    //                message,
    //                ex.ToString());
    //        }
    //    }

    //    [Event((int) CoreMessageEvents.ServiceErrorEventId, Level = EventLevel.Error, Message = "{7} - {8}")]
    //    private void ServiceError(
    //        string serviceName,
    //        string serviceTypeName,
    //        long replicaOrInstanceId,
    //        Guid partitionId,
    //        string applicationName,
    //        string applicationTypeName,
    //        string nodeName,
    //        string message,
    //        string exception
    //    )
    //    { 
    //        WriteEvent(
    //            (int) CoreMessageEvents.ServiceErrorEventId,
    //            serviceName,
    //            serviceTypeName,
    //            replicaOrInstanceId,
    //            partitionId,
    //            applicationName,
    //            applicationTypeName,
    //            nodeName,
    //            message,
    //            exception);
    //    }
    //}
}