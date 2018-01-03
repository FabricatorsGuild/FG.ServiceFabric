namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using FG.ServiceFabric.Diagnostics;
    using FG.ServiceFabric.Services.Remoting.ExceptionHandler;

    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Builder;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.V1;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V1.FabricTransport.Client;

    public static class FabricTransportServiceRemotingHelpers
    {
        /// <summary>
        /// Creates a new remoting service client factory
        /// </summary>
        /// <param name="serviceInterfaceType">The service interface</param>
        /// <param name="callbackClient"></param>
        /// <param name="logger"></param>
        /// <param name="correlationId"></param>
        /// <param name="serviceMethodDispatcher"></param>
        /// <returns></returns>
        public static IServiceRemotingClientFactory CreateServiceRemotingClientFactory(
            Type serviceInterfaceType,
            IServiceRemotingCallbackClient callbackClient,
            IServiceClientLogger logger,
            string correlationId,
            MethodDispatcherBase serviceMethodDispatcher)
        {
            var fabricTransportSettings = GetDefaultFabricTransportSettings("TransportSettings");
            var exceptionHandlers = ExceptionHandlerFactory.Default.GetExceptionHandlers(serviceInterfaceType);
            return new Client.FabricTransportServiceRemotingClientFactory(
                new FabricTransportServiceRemotingClientFactory(
                    fabricTransportSettings,
                    callbackClient,
                    null,
                    exceptionHandlers,
                    correlationId),
                    logger,
                    serviceMethodDispatcher);
        }

        public static IEnumerable<IExceptionHandler> GetExceptionHandlers(Type actorInterfaceType, params Type[] additionalTypes)
        {
            return ExceptionHandlerFactory.Default.GetExceptionHandlers(actorInterfaceType, additionalTypes);
        }

        internal static FabricTransportRemotingListenerSettings GetDefaultFabricTransportListenerSettings(string sectionName = "TransportSettings")
        {
            FabricTransportRemotingListenerSettings listenerSettings = null;
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
            FabricTransportRemotingSettings settings = null;
            if (!FabricTransportRemotingSettings.TryLoadFrom(sectionName, out settings, null, null))
            {
                settings = new FabricTransportRemotingSettings();
            }

            return settings;
        }
    }
}