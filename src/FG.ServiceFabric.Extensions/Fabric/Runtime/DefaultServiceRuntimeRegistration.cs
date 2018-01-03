using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Fabric.Runtime
{
    public class DefaultServiceRuntimeRegistration : IServiceRuntimeRegistration
    {
        public Task RegisterServiceAsync
        (string serviceTypeName,
            Func<StatelessServiceContext, StatelessService> serviceFactory,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return ServiceRuntime.RegisterServiceAsync(serviceTypeName, serviceFactory, timeout, cancellationToken);
        }

        public Task RegisterServiceAsync
        (string serviceTypeName,
            Func<StatefulServiceContext, StatefulServiceBase> serviceFactory,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return ServiceRuntime.RegisterServiceAsync(serviceTypeName, serviceFactory, timeout, cancellationToken);
        }
    }
}