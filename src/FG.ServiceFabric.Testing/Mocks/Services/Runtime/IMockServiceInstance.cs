using System;
using System.Fabric.Query;
using System.Threading;
using FG.ServiceFabric.Testing.Setup;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    public interface IMockServiceInstance
    {
        MockActorServiceInstanceStatus Status { get; }
        Uri ServiceUri { get; }
        Partition Partition { get; }
        Replica Replica { get; }

        IServiceManifest ServiceManifest { get; }
        IServiceConfig ServiceConfig { get; }


        DateTime? RunAsyncStarted { get; }
        DateTime? RunAsyncEnded { get; }

        CancellationTokenSource CancellationTokenSource { get; }
    }
}