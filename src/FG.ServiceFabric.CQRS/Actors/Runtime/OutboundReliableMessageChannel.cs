using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
	public interface IReceiverActorBinder
	{
		IReliableMessageReceiverActor Bind(ActorReference actorReference);
	}

	internal class DefaultActorBinder : IReceiverActorBinder
	{
		public IReliableMessageReceiverActor Bind(ActorReference actorReference)
		{
			return ((IReliableMessageReceiverActor) actorReference
				.Bind(typeof(IReliableMessageReceiverActor)));
		}
	}

	[DataContract]
	internal sealed class ActorReliableMessage
	{
		[DataMember]
		public Microsoft.ServiceFabric.Actors.ActorReference ActorReference { get; internal set; }

		[DataMember]
		public ReliableMessage Message { get; set; }
	}

	[DataContract]
	internal sealed class ReliableMessageChannelState : IEnumerable<ActorReliableMessage>
	{
		public ReliableMessageChannelState()
		{
			InnerQueue = new Queue<ActorReliableMessage>();
		}

		public ReliableMessageChannelState(ActorReliableMessage message) : this()
		{
			InnerQueue = new Queue<ActorReliableMessage>(new[] {message});
		}

		[DataMember]
		private Queue<ActorReliableMessage> InnerQueue { get; set; }

		public bool IsEmpty => InnerQueue.Count == 0;
		public int Depth => InnerQueue.Count;

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<ActorReliableMessage> GetEnumerator()
		{
			return InnerQueue.GetEnumerator();
		}

		public ReliableMessageChannelState Enqueue(ActorReliableMessage message)
		{
			InnerQueue = new Queue<ActorReliableMessage>(InnerQueue.Union(new[] {message}));
			return this;
		}

		public ActorReliableMessage Dequeue()
		{
			var value = InnerQueue.Dequeue();
			InnerQueue = new Queue<ActorReliableMessage>(InnerQueue);
			return value;
		}

		public ActorReliableMessage Peek()
		{
			return InnerQueue.Peek();
		}
	}

	public class OutboundReliableMessageChannel : IOutboundReliableMessageChannel
	{
		private const string ReliableMessageQueueStateKey = @"fg__reliablemessagequeue_state";
		private const string DeadLetterQueue = @"fg__deadletterqueue_state";
		private readonly IReceiverActorBinder _actorBinder;
		private readonly IActorProxyFactory _actorProxyFactory;
		private readonly Func<IOutboundMessageChannelLogger> _loggerFactory;
		private readonly Func<ReliableMessage, ActorReference, Task> _messageDrop;
		private readonly IActorStateManager _stateManager;

		public OutboundReliableMessageChannel(
			IActorStateManager stateManager,
			IActorProxyFactory actorProxyFactory,
			Func<IOutboundMessageChannelLogger> loggerFactory = null,
			Func<ReliableMessage, ActorReference, Task> messageDrop = null,
			IReceiverActorBinder actorBinder = null)
		{
			_stateManager = stateManager;
			_actorProxyFactory = actorProxyFactory;
			_messageDrop = messageDrop;
			_loggerFactory = loggerFactory ?? DefaultLoggerFactory();
			_actorBinder = actorBinder ?? new DefaultActorBinder();
		}

		public async Task SendMessageAsync<TActorInterface>(ReliableMessage message, ActorId actorId,
			CancellationToken cancellationToken,
			string applicationName = null, string serviceName = null, string listerName = null)
			where TActorInterface : IReliableMessageReceiverActor
		{
			var proxy = _actorProxyFactory.CreateActorProxy<TActorInterface>(actorId, applicationName, serviceName,
				listerName);

			var channelState =
				await GetOrAddStateWithRetriesAsync(ReliableMessageQueueStateKey, _stateManager, cancellationToken);

			channelState.Enqueue(new ActorReliableMessage
			{
				ActorReference = ActorReference.Get((IActorProxy) proxy),
				Message = message
			});
			await AddOrUpdateStateWithRetriesAsync(ReliableMessageQueueStateKey, channelState, _stateManager, cancellationToken);
		}

		public async Task ProcessQueueAsync(CancellationToken cancellationToken)
		{
			var channelState =
				await GetOrAddStateWithRetriesAsync(ReliableMessageQueueStateKey, _stateManager, cancellationToken);
			while (channelState != null && !channelState.IsEmpty)
			{
				var actorMessage = channelState.Dequeue();
				try
				{
					await SendAsync(actorMessage.Message, actorMessage.ActorReference);
					await _stateManager.AddOrUpdateStateAsync(ReliableMessageQueueStateKey, channelState,
						(k, v) => channelState, cancellationToken);

					_loggerFactory()?
						.MessageSent(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri,
							actorMessage.Message.Payload, actorMessage.Message.MessageType);
				}
				catch (Exception e)
				{
					_loggerFactory()
						?.FailedToSendMessage(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri, e);
					await DropMessageAsync(actorMessage, cancellationToken);
				}
			}
		}

		private static Func<IOutboundMessageChannelLogger> DefaultLoggerFactory()
		{
			return () => null;
		}

		public async Task<ReliableMessage> PeekQueue(CancellationToken cancellationToken)
		{
			var channelState =
				await GetOrAddStateWithRetriesAsync(ReliableMessageQueueStateKey, _stateManager, cancellationToken);
			return channelState.Peek().Message;
		}

		public async Task<IEnumerable<DeadLetter>> GetDeadLetters(CancellationToken cancellationToken)
		{
			var channelState = await GetOrAddStateWithRetriesAsync(DeadLetterQueue, _stateManager, cancellationToken);
			return channelState.Select(message => new DeadLetter(message)).ToList();
		}

		private async Task DropMessageAsync(ActorReliableMessage actorMessage, CancellationToken cancellationToken)
		{
			if (_messageDrop == null)
			{
				await MoveToDeadLetters(actorMessage, cancellationToken);
			}
			else
			{
				await _messageDrop(actorMessage.Message, actorMessage.ActorReference);
			}
		}

		private async Task MoveToDeadLetters(ActorReliableMessage actorMessage, CancellationToken cancellationToken)
		{
			var deadLetterState = await GetOrAddStateWithRetriesAsync(DeadLetterQueue, _stateManager, cancellationToken);
			deadLetterState.Enqueue(actorMessage);
			await AddOrUpdateStateWithRetriesAsync(DeadLetterQueue, deadLetterState, _stateManager, cancellationToken);
			_loggerFactory()?
				.MessageMovedToDeadLetterQueue(actorMessage.Message.MessageType, actorMessage.Message.Payload,
					deadLetterState.Depth);
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		internal Task SendAsync(ReliableMessage message, ActorReference actorReference)
		{
			return _actorBinder.Bind(actorReference).ReceiveMessageAsync(message);
		}

		private static Task AddOrUpdateStateWithRetriesAsync(string stateName, ReliableMessageChannelState state,
			IActorStateManager stateManager, CancellationToken cancellationToken)
		{
			return ExecutionHelper.ExecuteWithRetriesAsync(
				ct => stateManager.AddOrUpdateStateAsync(stateName, state, (k, v) => state, ct), 3,
				TimeSpan.FromSeconds(1), cancellationToken);
		}

		private static Task<ReliableMessageChannelState> GetOrAddStateWithRetriesAsync(string stateName,
			IActorStateManager stateManager, CancellationToken cancellationToken)
		{
			return ExecutionHelper.ExecuteWithRetriesAsync(
				ct => stateManager.GetOrAddStateAsync(stateName, new ReliableMessageChannelState(), ct),
				3, TimeSpan.FromSeconds(1), cancellationToken);
		}
	}
}