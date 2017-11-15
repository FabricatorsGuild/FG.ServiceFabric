using System;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using FG.Common.Utils;

namespace FG.ServiceFabric.Actors
{
	public class ActorReference
	{
		/// <summary>
		///     Gets Uri of the actor service that hosts the actor in service fabric cluster.
		/// </summary>
		/// <value>Service Uri which hosts the actor in service fabric cluster.</value>
		[DataMember(IsRequired = true, Name = "ServiceUri", Order = 0)]
		public Uri ServiceUri { get; set; }

		/// <summary>
		///     Gets or sets the <see cref="T:Microsoft.ServiceFabric.Actors.ActorId" /> of the actor.
		/// </summary>
		/// <value><see cref="T:Microsoft.ServiceFabric.Actors.ActorId" /> of the actor.</value>
		[DataMember(IsRequired = true, Name = "ActorId", Order = 1)]
		public ActorId ActorId { get; set; }

		/// <summary>
		///     Gets or sets the name of the listener in the actor service to use when communicating with the actor service.
		/// </summary>
		/// <value>The name of the listener</value>
		[DataMember(IsRequired = false, Name = "ListenerName", Order = 2)]
		public string ListenerName { get; set; }

		/// <summary>
		///     Creates an <see cref="T:Microsoft.ServiceFabric.Actors.Client.ActorProxy" /> that implements an actor interface for
		///     the actor using the
		///     <see
		///         cref="M:Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory.CreateActorProxy(System.Type,System.Uri,Microsoft.ServiceFabric.Actors.ActorId,System.String)" />
		///     method.
		/// </summary>
		/// <param name="actorInterfaceType">
		///     Actor interface for the created
		///     <see cref="T:Microsoft.ServiceFabric.Actors.Client.ActorProxy" /> to implement.
		/// </param>
		/// <returns>
		///     An actor proxy object that implements <see cref="T:Microsoft.ServiceFabric.Actors.Client.IActorProxy" /> and
		///     TActorInterface.
		/// </returns>
		public object Bind(Type actorInterfaceType)
		{
			return this.CallGenericMethod(nameof(BindInternal), new[] {actorInterfaceType}, this.ServiceUri, this.ActorId,
				this.ListenerName);
		}

		public static implicit operator ActorReference(Microsoft.ServiceFabric.Actors.ActorReference actorReference)
		{
			return new ActorReference()
			{
				ActorId = actorReference.ActorId,
				ListenerName = actorReference.ListenerName,
				ServiceUri = actorReference.ServiceUri
			};
		}

		private object BindInternal<TActorInterfaceType>(Uri serviceUri, ActorId actorId, string listenerName)
			where TActorInterfaceType : IActor
		{
			return ActorProxy.Create<TActorInterfaceType>(actorId, serviceUri, listenerName);
		}

		public static Microsoft.ServiceFabric.Actors.ActorReference Get(object actor)
		{
			if ((actor is IActorProxy actorProxy) && !(actorProxy is ActorProxy))
			{
				// that means it's one of our internal proxies for testing
				return new Microsoft.ServiceFabric.Actors.ActorReference()
				{
					ActorId = actorProxy.ActorId,
					ListenerName = actorProxy.ActorServicePartitionClient.ListenerName,
					ServiceUri = actorProxy.ActorServicePartitionClient.ServiceUri,
				};
			}

			return Microsoft.ServiceFabric.Actors.ActorReference.Get(actor);
		}
	}
}