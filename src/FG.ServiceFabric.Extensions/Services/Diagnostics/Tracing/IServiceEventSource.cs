using System;
using System.Fabric;
using FG.ServiceFabric.Diagnostics.Tracing;

namespace FG.ServiceFabric.Services.Diagnostics.Tracing
{
    //public interface IServiceEventSource : IEventSource
    //{
    //    void ServiceMessage(ServiceContext serviceContext, string message);
    //    void ServiceWarning(ServiceContext serviceContext, string message);
    //    void ServiceError(ServiceContext serviceContext, string message, Exception ex);
    //}

    public interface IServiceEventSource
    {
        void ServiceReplicaListenerMessage(ServiceContext context, string message);
        void ServiceReplicaListenerError(ServiceContext context, string message, Exception exception);
    }
}