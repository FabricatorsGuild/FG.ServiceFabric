namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;

    using FG.ServiceFabric.Services.Remoting.Runtime;

    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V1;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Runtime;

    public class FabricTransportServiceRemotingProviderAttribute : Microsoft.ServiceFabric.Services.Remoting.FabricTransport.FabricTransportServiceRemotingProviderAttribute
    {
        public FabricTransportServiceRemotingProviderAttribute(params Type[] exceptionHandlerTypes)
        {
            this.ExceptionHandlerTypes = exceptionHandlerTypes;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IEnumerable<Type> ExceptionHandlerTypes { get; set; }

        /// <summary>
        ///     Creates a service remoting client factory for connecting to the service over remoted service interfaces.
        /// </summary>
        /// <param name="callbackClient">
        ///     Client implementation where the callbacks should be dispatched.
        /// </param>
        /// <returns>
        ///     A
        ///     <see
        ///         cref="T:Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Client.FabricTransportServiceRemotingClientFactory" />
        ///     as <see cref="T:Microsoft.ServiceFabric.Services.Remoting.Client.IServiceRemotingClientFactory" />
        ///     that can be used with <see cref="T:Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory" /> to
        ///     generate service proxy to talk to a stateless or stateful service over remoted actor interface.
        /// </returns>
        public override IServiceRemotingClientFactory CreateServiceRemotingClientFactory(IServiceRemotingCallbackClient callbackClient)
        {
            var fabricTransportSettings = GetDefaultFabricTransportSettings();
            fabricTransportSettings.MaxMessageSize = this.GetAndValidateMaxMessageSize(fabricTransportSettings.MaxMessageSize);
            fabricTransportSettings.OperationTimeout = this.GetAndValidateOperationTimeout(fabricTransportSettings.OperationTimeout);
            fabricTransportSettings.KeepAliveTimeout = this.GetKeepAliveTimeout(fabricTransportSettings.KeepAliveTimeout);
            var exceptionHandlers = this.ExceptionHandlerTypes?.Where(exceptionHandlerType => exceptionHandlerType.GetInterface(nameof(IExceptionHandler), false) != null)
                .Select(exceptionHandlerType => (IExceptionHandler)Activator.CreateInstance(exceptionHandlerType))
                .ToArray();
            return new FabricTransportServiceRemotingClientFactory(fabricTransportSettings, callbackClient, null, exceptionHandlers, null);
        }

        /// <summary>
        ///     Creates a service remoting listener for remoting the service interface.
        ///     Uses the <see cref="T:FG.CodeEffect.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingDispatcher" /> to
        ///     dispatch
        ///     service method invocations.
        ///     Exception Handlers <see cref="T:Microsoft.ServiceFabric.Services.Communication.Client.IExceptionHandler" /> can be
        ///     attached by
        ///     adding them to the the constructor of this attribute
        /// </summary>
        /// <param name="serviceContext">
        ///     The context of the service for which the remoting listener is being constructed.
        /// </param>
        /// <param name="serviceImplementation">
        ///     The service implementation object.
        /// </param>
        /// <returns>
        ///     A
        ///     <see
        ///         cref="T:Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime.FabricTransportServiceRemotingListener" />
        ///     as <see cref="T:Microsoft.ServiceFabric.Services.Remoting.Runtime.IServiceRemotingListener" />
        ///     for the specified service implementation.
        /// </returns>
        public override IServiceRemotingListener CreateServiceRemotingListener(ServiceContext serviceContext, IService serviceImplementation)
        {
            var listenerSettings = GetDefaultFabricTransportListenerSettings();
            listenerSettings.MaxMessageSize = this.GetAndValidateMaxMessageSize(listenerSettings.MaxMessageSize);
            listenerSettings.OperationTimeout = this.GetAndValidateOperationTimeout(listenerSettings.OperationTimeout);
            listenerSettings.KeepAliveTimeout = this.GetKeepAliveTimeout(listenerSettings.KeepAliveTimeout);
            var messageHandler = ServiceRemotingDispatcherAttribute.GetServiceRemotingDispatcher(serviceContext, serviceImplementation);

            return new FabricTransportServiceRemotingListener(serviceContext, messageHandler, listenerSettings);
        }

        private static FabricTransportRemotingListenerSettings GetDefaultFabricTransportListenerSettings(string sectionName = "TransportSettings")
        {
            var listenerSettings = (FabricTransportRemotingListenerSettings)null;
            if (!FabricTransportRemotingListenerSettings.TryLoadFrom(sectionName, out listenerSettings, null))
            {
                listenerSettings = new FabricTransportRemotingListenerSettings();
            }

            return listenerSettings;
        }

        /// <summary>
        ///     FabricTransportSettings returns the default Settings .Loads the configuration file from default Config
        ///     Package"Config" , if not found then try to load from  default config file "ClientExeName.Settings.xml"  from Client
        ///     Exe directory.
        /// </summary>
        /// <param name="sectionName">
        ///     Name of the section within the configuration file. If not found section in configuration
        ///     file, it will return the default Settings
        /// </param>
        /// <returns></returns>
        private static FabricTransportRemotingSettings GetDefaultFabricTransportSettings(string sectionName = "TransportSettings")
        {
            var settings = (FabricTransportRemotingSettings)null;
            if (!FabricTransportRemotingSettings.TryLoadFrom(sectionName, out settings, null, null))
            {
                settings = new FabricTransportRemotingSettings();
            }

            return settings;
        }

        private long GetAndValidateMaxMessageSize(long maxMessageSize)
        {
            return this.MaxMessageSize <= 0L ? maxMessageSize : this.MaxMessageSize;
        }

        private TimeSpan GetAndValidateOperationTimeout(TimeSpan operationTimeout)
        {
            return this.OperationTimeoutInSeconds <= 0L ? operationTimeout : TimeSpan.FromSeconds(this.OperationTimeoutInSeconds);
        }

        private TimeSpan GetKeepAliveTimeout(TimeSpan keepAliveTimeout)
        {
            return this.KeepAliveTimeoutInSeconds <= 0L ? keepAliveTimeout : TimeSpan.FromSeconds(this.KeepAliveTimeoutInSeconds);
        }
    }
}