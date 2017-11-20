using System;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{
	public static class ServiceRemotingMessageHeadersExtensions
	{
		internal static ActorMessageHeaders GetActorMessageHeaders(this ServiceRemotingMessageHeaders messageHeaders,
			IActorServiceCommunicationLogger logger)
		{
			try
			{
				ActorMessageHeaders actorMessageHeaders;
				if (ActorMessageHeaders.TryFromServiceMessageHeaders(messageHeaders, out actorMessageHeaders))
				{
					return actorMessageHeaders;
				}
			}
			catch (Exception ex)
			{
				// ignored
				logger?.FailedToReadActorMessageHeaders(messageHeaders, ex);
			}
			return null;
		}
	}
}