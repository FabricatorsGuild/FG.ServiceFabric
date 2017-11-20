using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.CQRS;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
	public interface IEventStoredActorService : IActorService
	{
		Task<IEnumerable<EventWrapper>> GetAllEventHistoryAsync(Guid aggregateRootId);
	}

	public abstract class EventStoredActorService<TAggregateRoot, TEventStream> : ActorService, IEventStoredActorService
		where TEventStream : IDomainEventStream, new()
		where TAggregateRoot : class, IEventStored, new()
	{
		protected readonly IEventStreamReader<TEventStream> StateProviderEventStreamReader;

		protected EventStoredActorService
		(StatefulServiceContext context,
			ActorTypeInformation actorTypeInfo,
			Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorId, ActorBase> actorFactory = null,
			Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
				stateManagerFactory = null,
			IActorStateProvider stateProvider = null,
			ActorServiceSettings settings = null,
			IReliableStateManagerReplica reliableStateManagerReplica = null,
			IEventStreamReader<TEventStream> eventStreamReader = null)
			: base(
				context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings,
				reliableStateManagerReplica)
		{
			StateProviderEventStreamReader = eventStreamReader ??
			                                 new EventStreamReader<TEventStream>(StateProvider,
				                                 EventStoredActor<TAggregateRoot, TEventStream>.EventStreamStateKey);
		}

		public async Task<IEnumerable<EventWrapper>> GetAllEventHistoryAsync(Guid aggregateRootId)
		{
			var events = await StateProviderEventStreamReader.GetEventStreamAsync(aggregateRootId,
				CancellationToken.None);


			return events.DomainEvents.OfType<IAggregateRootEvent>().Select(
				@event => new EventWrapper(@event)).ToList();
		}
	}

	[DataContract]
	public class EventWrapper : IAggregateRootEvent
	{
		public EventWrapper(IAggregateRootEvent @event)
		{
			EventId = @event.EventId;
			UtcTimeStamp = @event.UtcTimeStamp;
			AggregateRootId = @event.AggregateRootId;
			Version = @event.Version;
			EventType = @event.GetType().FullName;
			JsonPayload = JsonConvert.SerializeObject(@event);
		}

		[DataMember]
		public string EventType { get; set; }

		[DataMember]
		public string JsonPayload { get; set; }

		[DataMember]
		public Guid EventId { get; }

		[DataMember]
		public DateTime UtcTimeStamp { get; set; }

		[DataMember]
		public Guid AggregateRootId { get; set; }

		[DataMember]
		public int Version { get; set; }
	}
}