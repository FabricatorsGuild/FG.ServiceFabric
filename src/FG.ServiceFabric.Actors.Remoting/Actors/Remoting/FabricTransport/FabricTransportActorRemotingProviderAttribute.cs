using System;
using System.Collections.Generic;
using System.Linq;
using FG.ServiceFabric.Services.Communication;
using Microsoft.ServiceFabric.Actors.Generator;
using Microsoft.ServiceFabric.Actors.Remoting.V1.FabricTransport.Client;
using Microsoft.ServiceFabric.Actors.Remoting.V1.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport
{
    public class FabricTransportActorRemotingProviderAttribute : Microsoft.ServiceFabric.Actors.Remoting.FabricTransport
        .FabricTransportActorRemotingProviderAttribute
    {
        public FabricTransportActorRemotingProviderAttribute()
        {
        }

        public FabricTransportActorRemotingProviderAttribute(params Type[] exceptionHandlerTypes)
        {
            ExceptionHandlerTypes = exceptionHandlerTypes;
        }

        public IEnumerable<Type> ExceptionHandlerTypes { get; set; }

        /// <summary>
        ///     Creates a service remoting client factory to connect to the remoted actor interfaces.
        /// </summary>
        /// <param name="callbackClient">
        ///     Client implementation where the callbacks should be dispatched.
        /// </param>
        /// <returns>
        ///     A
        ///     <see cref="T:Microsoft.ServiceFabric.Actors.Remoting.FabricTransport.FabricTransportActorRemotingClientFactory" />
        ///     as <see cref="T:Microsoft.ServiceFabric.Services.Remoting.Client.IServiceRemotingClientFactory" />
        ///     that can be used with <see cref="T:Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory" /> to
        ///     generate actor proxy to talk to the actor over remoted actor interface.
        /// </returns>
        public override IServiceRemotingClientFactory CreateServiceRemotingClientFactory(
            IServiceRemotingCallbackClient callbackClient)
        {
            // Microsoft.ServiceFabric.Services.Remoting.FabricTransport.FabricTransportRemotingSettings
            var fabricTransportSettings = GetDefaultFabricTransportSettings("TransportSettings");
            fabricTransportSettings.MaxMessageSize =
                GetAndValidateMaxMessageSize(fabricTransportSettings.MaxMessageSize);
            fabricTransportSettings.OperationTimeout =
                GetandValidateOperationTimeout(fabricTransportSettings.OperationTimeout);
            fabricTransportSettings.KeepAliveTimeout =
                GetandValidateKeepAliveTimeout(fabricTransportSettings.KeepAliveTimeout);
            var exceptionHandlers = GetExceptionHandlers();
            return new Client.FabricTransportActorRemotingClientFactory(
                new FabricTransportActorRemotingClientFactory(fabricTransportSettings, callbackClient, null,
                    exceptionHandlers, null),
                null,
                null);
        }

        public override IServiceRemotingListener CreateServiceRemotingListener(ActorService actorService)
        {
            var listenerSettings = GetActorListenerSettings(actorService);
            listenerSettings.MaxMessageSize = GetAndValidateMaxMessageSize(listenerSettings.MaxMessageSize);
            listenerSettings.OperationTimeout = GetandValidateOperationTimeout(listenerSettings.OperationTimeout);
            listenerSettings.KeepAliveTimeout = GetandValidateKeepAliveTimeout(listenerSettings.KeepAliveTimeout);

            var serviceRemotingDispatcher =
                ActorServiceRemotingDispatcherAttribute.GetServiceRemotingDispatcher(actorService);

            return new FabricTransportActorServiceRemotingListener(actorService.Context, serviceRemotingDispatcher,
                listenerSettings);
        }

        internal static FabricTransportRemotingListenerSettings GetActorListenerSettings(ActorService actorService)
        {
            FabricTransportRemotingListenerSettings listenerSettings;
            if (!FabricTransportRemotingListenerSettings.TryLoadFrom(
                ActorNameFormat.GetFabricServiceTransportSettingsSectionName(actorService.ActorTypeInformation
                    .ImplementationType),
                out listenerSettings,
                null))
                listenerSettings = GetDefaultFabricTransportListenerSettings("TransportSettings");

            return listenerSettings;
        }

        internal static FabricTransportRemotingListenerSettings GetDefaultFabricTransportListenerSettings(
            string sectionName = "TransportSettings")
        {
            FabricTransportRemotingListenerSettings listenerSettings = null;
            if (!FabricTransportRemotingListenerSettings.TryLoadFrom(sectionName, out listenerSettings, null))
                listenerSettings = new FabricTransportRemotingListenerSettings();

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
        private static FabricTransportRemotingSettings GetDefaultFabricTransportSettings(
            string sectionName = "TransportSettings")
        {
            FabricTransportRemotingSettings settings = null;
            if (!FabricTransportRemotingSettings.TryLoadFrom(sectionName, out settings, null, null))
                settings = new FabricTransportRemotingSettings();

            return settings;
        }

        private TimeSpan GetandValidateKeepAliveTimeout(TimeSpan keepAliveTimeout)
        {
            if (KeepAliveTimeoutInSeconds <= 0L)
                return keepAliveTimeout;

            return TimeSpan.FromSeconds(KeepAliveTimeoutInSeconds);
        }

        private long GetAndValidateMaxMessageSize(long maxMessageSize)
        {
            if (MaxMessageSize <= 0L)
                return maxMessageSize;

            return MaxMessageSize;
        }

        private TimeSpan GetandValidateOperationTimeout(TimeSpan operationTimeout)
        {
            if (OperationTimeoutInSeconds <= 0L)
                return operationTimeout;

            return TimeSpan.FromSeconds(OperationTimeoutInSeconds);
        }

        private IEnumerable<IExceptionHandler> GetExceptionHandlers()
        {
            var exceptionHandlers = ExceptionHandlerTypes
                ?.Where(type => type.GetInterface(nameof(IExceptionHandler), false) != null)
                .Select(exceptionHandlerType => (IExceptionHandler) Activator.CreateInstance(exceptionHandlerType))
                .Union(
                    ExceptionHandlerTypes
                        ?.Where(type => type.IsSubclassOf(typeof(Exception)) || type == typeof(Exception))
                        .Select(exceptionType =>
                            (IExceptionHandler) Activator.CreateInstance(
                                typeof(ExceptionHandler<>).MakeGenericType(exceptionType))))
                .ToArray();
            return exceptionHandlers;
        }
    }
}