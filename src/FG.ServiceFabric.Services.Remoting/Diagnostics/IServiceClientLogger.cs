namespace FG.ServiceFabric.Diagnostics
{
    using System;

    using FG.ServiceFabric.Services.Remoting.FabricTransport;

    using Microsoft.ServiceFabric.Services.Remoting.V1;

    /// <summary>
    /// Provides logging for the service client
    /// </summary>
    public interface IServiceClientLogger : IServiceRemotingLogger
    {
        /// <summary>
        /// Logs a service call
        /// </summary>
        /// <param name="requestUri">The service uri</param>
        /// <param name="serviceMethodName">The service method name</param>
        /// <param name="serviceMessageHeaders">The message headers</param>
        /// <param name="customServiceRequestHeader">The custom service request header</param>
        /// <returns>A disposable</returns>
        IDisposable CallService(
            Uri requestUri,
            string serviceMethodName,
            ServiceRemotingMessageHeaders serviceMessageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader);

        /// <summary>
        /// Logs a failed service call
        /// </summary>
        /// <param name="requestUri">The service uri</param>
        /// <param name="serviceMethodName">The service method name</param>
        /// <param name="serviceMessageHeaders">The message headers</param>
        /// <param name="customServiceRequestHeader">The custom service request header</param>
        /// <param name="ex">The exception</param>
        void CallServiceFailed(
            Uri requestUri,
            string serviceMethodName,
            ServiceRemotingMessageHeaders serviceMessageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader,
            Exception ex);

        /// <summary>
        /// Logs a when the service client fails
        /// </summary>
        /// <param name="requestUri">The service uri</param>
        /// <param name="customServiceRequestHeader">The custom service request header</param>
        /// <param name="ex">The exception</param>
        void ServiceClientFailed(Uri requestUri, CustomServiceRequestHeader customServiceRequestHeader, Exception ex);
    }
}