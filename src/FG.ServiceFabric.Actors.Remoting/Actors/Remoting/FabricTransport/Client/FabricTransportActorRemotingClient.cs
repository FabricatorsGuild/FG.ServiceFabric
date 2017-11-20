using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Remoting.Runtime;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Remoting.FabricTransport.Client;
using FG.Common.Utils;
using FG.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport.Client
{
	public class FabricTransportActorRemotingClient : FabricTransportServiceRemotingClient
	{
		private static readonly ConcurrentDictionary<long, string> ActorMethodMap = new ConcurrentDictionary<long, string>();

		private readonly IActorClientLogger _logger;

		public FabricTransportActorRemotingClient(IServiceRemotingClient innerClient, Uri serviceUri,
			IActorClientLogger logger,
			MethodDispatcherBase[] serviceMethodDispatchers)
			: base(innerClient, serviceUri, logger, serviceMethodDispatchers)
		{
			_logger = logger;
		}

		internal IServiceRemotingClient InnerClient => base.InnerClient;

		private string GetActorMethodName(ActorMessageHeaders actorMessageHeaders)
		{
			if (actorMessageHeaders == null) return null;

			return base.GetServiceMethodName(actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);
		}

		~FabricTransportActorRemotingClient()
		{
			if (this.InnerClient == null) return;
			// ReSharper disable once SuspiciousTypeConversion.Global
			var disposable = this.InnerClient as IDisposable;
			disposable?.Dispose();
		}

		protected override Task<byte[]> RequestServiceResponseAsync(ServiceRemotingMessageHeaders messageHeaders,
			CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
		{
			var actorMessageHeaders = GetActorMessageHeaders(messageHeaders);
			if (actorMessageHeaders != null)
			{
				return RequestActorResponseAsync(messageHeaders, actorMessageHeaders, customServiceRequestHeader, requestBody);
			}
			return base.RequestServiceResponseAsync(messageHeaders, customServiceRequestHeader, requestBody);
		}

		private Task<byte[]> RequestActorResponseAsync(ServiceRemotingMessageHeaders messageHeaders,
			ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
		{
			var methodName = GetActorMethodName(actorMessageHeaders);
			using (_logger?.CallActor(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader) ??
			       new SafeDisposable())
			{
				try
				{
					var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
					return result;
				}
				catch (Exception ex)
				{
					_logger?.CallActorFailed(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader, ex);
					throw;
				}
			}
		}

		protected override Task<byte[]> SendServiceOneWay(ServiceRemotingMessageHeaders messageHeaders,
			CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
		{
			var actorMessageHeaders = GetActorMessageHeaders(messageHeaders);
			if (actorMessageHeaders != null)
			{
				return SendActorOneWay(messageHeaders, actorMessageHeaders, customServiceRequestHeader, requestBody);
			}
			return base.SendServiceOneWay(messageHeaders, customServiceRequestHeader, requestBody);
		}

		private Task<byte[]> SendActorOneWay(ServiceRemotingMessageHeaders messageHeaders,
			ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
		{
			var methodName = GetActorMethodName(actorMessageHeaders);
			using (_logger?.CallActor(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader) ??
			       new SafeDisposable())
			{
				try
				{
					var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
					return result;
				}
				catch (Exception ex)
				{
					_logger?.CallActorFailed(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader, ex);
					throw;
				}
			}
		}

		private static ActorMessageHeaders GetActorMessageHeaders(ServiceRemotingMessageHeaders messageHeaders)
		{
			ActorMessageHeaders actorMessageHeaders = null;
			if (ActorMessageHeaders.TryFromServiceMessageHeaders(messageHeaders, out actorMessageHeaders))
			{
			}
			return actorMessageHeaders;
		}
	}
}