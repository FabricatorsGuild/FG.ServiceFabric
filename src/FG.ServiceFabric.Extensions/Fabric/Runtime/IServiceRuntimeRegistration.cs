using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Fabric.Runtime
{
    public interface IServiceRuntimeRegistration
    {
        /// <summary>
        ///     Registers a reliable stateless service with Service Fabric runtime.
        /// </summary>
        /// <param name="serviceTypeName">The service type name as provied in service manifest.</param>
        /// <param name="serviceFactory">A factory method to create stateless service objects.</param>
        /// <param name="timeout">The timeout for the register operation.</param>
        /// <para>
        ///     The default timeout for this operation is taken from ServiceFactoryRegistrationTimeout in Hosting section of the
        ///     cluster manifest. Default value for ServiceFactoryRegistrationTimeout is 120 seconds.
        /// </para>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     A task that represents the asynchronous register operation.
        /// </returns>
        Task RegisterServiceAsync(
            string serviceTypeName,
            Func<StatelessServiceContext, StatelessService> serviceFactory,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Registers a reliable stateful service with Service Fabric runtime.
        /// </summary>
        /// <param name="serviceTypeName">The service type name as provied in service manifest.</param>
        /// <param name="serviceFactory">A factory method to create stateful service objects.</param>
        /// <param name="timeout">The timeout for the register operation.</param>
        /// <para>
        ///     The default timeout for this operation is taken from ServiceFactoryRegistrationTimeout in Hosting section of the
        ///     cluster manifest. Default value for ServiceFactoryRegistrationTimeout is 120 seconds.
        /// </para>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     A task that represents the asynchronous register operation.
        /// </returns>
        Task RegisterServiceAsync(
            string serviceTypeName,
            Func<StatefulServiceContext, StatefulServiceBase> serviceFactory,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken));
    }
}