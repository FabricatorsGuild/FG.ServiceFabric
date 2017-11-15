using System;
using System.Collections.Generic;
using System.Linq;
using FG.ServiceFabric.Services.Communication;
using Microsoft.ServiceFabric.Actors.Generator;
using Microsoft.ServiceFabric.Actors.Remoting.V1.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport
{
	public class FabricTransportActorRemotingProviderAttribute : Microsoft.ServiceFabric.Actors.Remoting.FabricTransport.
		FabricTransportActorRemotingProviderAttribute
	{
		private IEnumerable<Type> _exceptionHandlerTypes;

		public FabricTransportActorRemotingProviderAttribute()
		{
		}

		public FabricTransportActorRemotingProviderAttribute(params Type[] exceptionHandlerTypes)
		{
			_exceptionHandlerTypes = exceptionHandlerTypes;
		}

		public IEnumerable<Type> ExceptionHandlerTypes
		{
			get { return _exceptionHandlerTypes; }
			set { _exceptionHandlerTypes = value; }
		}

		private IEnumerable<IExceptionHandler> GetExceptionHandlers()
		{
			var exceptionHandlers =
				_exceptionHandlerTypes?
					.Where(type => type.GetInterface(nameof(IExceptionHandler), false) != null)
					.Select(exceptionHandlerType => (IExceptionHandler) Activator.CreateInstance(exceptionHandlerType))
					.Union(
						_exceptionHandlerTypes?
							.Where(type => type.IsSubclassOf(typeof(Exception)) || type == typeof(Exception))
							.Select(exceptionType => (IExceptionHandler) Activator.CreateInstance(
								typeof(ExceptionHandler<>).MakeGenericType(new Type[] {exceptionType}))))
					.ToArray();
			return exceptionHandlers;
		}

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
			//Microsoft.ServiceFabric.Services.Remoting.FabricTransport.FabricTransportRemotingSettings
			FabricTransportRemotingSettings fabricTransportSettings = GetDefaultFabricTransportSettings("TransportSettings");
			fabricTransportSettings.MaxMessageSize = this.GetAndValidateMaxMessageSize(fabricTransportSettings.MaxMessageSize);
			fabricTransportSettings.OperationTimeout =
				this.GetandValidateOperationTimeout(fabricTransportSettings.OperationTimeout);
			fabricTransportSettings.KeepAliveTimeout =
				this.GetandValidateKeepAliveTimeout(fabricTransportSettings.KeepAliveTimeout);
			var exceptionHandlers = GetExceptionHandlers();
			return (IServiceRemotingClientFactory) new FG.ServiceFabric.Actors.Remoting.FabricTransport.Client.
				FabricTransportActorRemotingClientFactory(
					new Microsoft.ServiceFabric.Actors.Remoting.V1.FabricTransport.Client.FabricTransportActorRemotingClientFactory(
						fabricTransportSettings,
						callbackClient,
						(IServicePartitionResolver) null,
						exceptionHandlers,
						traceId: (string) null), null, null);
		}

		public override IServiceRemotingListener CreateServiceRemotingListener(ActorService actorService)
		{
			FabricTransportRemotingListenerSettings listenerSettings = GetActorListenerSettings(actorService);
			listenerSettings.MaxMessageSize = this.GetAndValidateMaxMessageSize(listenerSettings.MaxMessageSize);
			listenerSettings.OperationTimeout = this.GetandValidateOperationTimeout(listenerSettings.OperationTimeout);
			listenerSettings.KeepAliveTimeout = this.GetandValidateKeepAliveTimeout(listenerSettings.KeepAliveTimeout);

			var serviceRemotingDispatcher = ActorServiceRemotingDispatcherAttribute.GetServiceRemotingDispatcher(actorService);

			return (IServiceRemotingListener) new FabricTransportActorServiceRemotingListener(
				serviceContext: actorService.Context,
				messageHandler: serviceRemotingDispatcher,
				listenerSettings: listenerSettings);
		}

		private long GetAndValidateMaxMessageSize(long maxMessageSize)
		{
			if (this.MaxMessageSize <= 0L)
				return maxMessageSize;
			return this.MaxMessageSize;
		}

		private TimeSpan GetandValidateOperationTimeout(TimeSpan operationTimeout)
		{
			if (this.OperationTimeoutInSeconds <= 0L)
				return operationTimeout;
			return TimeSpan.FromSeconds((double) this.OperationTimeoutInSeconds);
		}

		private TimeSpan GetandValidateKeepAliveTimeout(TimeSpan keepAliveTimeout)
		{
			if (this.KeepAliveTimeoutInSeconds <= 0L)
				return keepAliveTimeout;
			return TimeSpan.FromSeconds((double) this.KeepAliveTimeoutInSeconds);
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
			FabricTransportRemotingSettings settings = (FabricTransportRemotingSettings) null;
			if (!FabricTransportRemotingSettings.TryLoadFrom(sectionName, out settings, (string) null, (string) null))
			{
				settings = new FabricTransportRemotingSettings();
			}
			return settings;
		}

		internal static FabricTransportRemotingListenerSettings GetActorListenerSettings(ActorService actorService)
		{
			FabricTransportRemotingListenerSettings listenerSettings;
			if (!FabricTransportRemotingListenerSettings.TryLoadFrom(
				ActorNameFormat.GetFabricServiceTransportSettingsSectionName(actorService.ActorTypeInformation.ImplementationType),
				out listenerSettings, (string) null))
				listenerSettings = GetDefaultFabricTransportListenerSettings("TransportSettings");
			return listenerSettings;
		}

		internal static FabricTransportRemotingListenerSettings GetDefaultFabricTransportListenerSettings(
			string sectionName = "TransportSettings")
		{
			FabricTransportRemotingListenerSettings listenerSettings = (FabricTransportRemotingListenerSettings) null;
			if (!FabricTransportRemotingListenerSettings.TryLoadFrom(sectionName, out listenerSettings, (string) null))
			{
				listenerSettings = new FabricTransportRemotingListenerSettings();
			}
			return listenerSettings;
		}
	}
}