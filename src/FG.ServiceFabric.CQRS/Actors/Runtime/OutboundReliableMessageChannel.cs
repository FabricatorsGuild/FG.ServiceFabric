namespace FG.ServiceFabric.Actors.Runtime
{
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

    using ActorReference = FG.ServiceFabric.Actors.ActorReference;

    public interface IReceiverActorBinder
    {
        IReliableMessageReceiverActor Bind(ActorReference actorReference);
    }

    internal class DefaultActorBinder : IReceiverActorBinder
    {
        public IReliableMessageReceiverActor Bind(ActorReference actorReference)
        {
            return (IReliableMessageReceiverActor)actorReference.Bind(typeof(IReliableMessageReceiverActor));
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
            this.InnerQueue = new Queue<ActorReliableMessage>();
        }

        public ReliableMessageChannelState(ActorReliableMessage message)
            : this()
        {
            this.InnerQueue = new Queue<ActorReliableMessage>(new[] { message });
        }

        public int Depth => this.InnerQueue.Count;

        public bool IsEmpty => this.InnerQueue.Count == 0;

        [DataMember]
        private Queue<ActorReliableMessage> InnerQueue { get; set; }

        public ActorReliableMessage Dequeue()
        {
            var value = this.InnerQueue.Dequeue();
            this.InnerQueue = new Queue<ActorReliableMessage>(this.InnerQueue);
            return value;
        }

        public ReliableMessageChannelState Enqueue(ActorReliableMessage message)
        {
            this.InnerQueue = new Queue<ActorReliableMessage>(this.InnerQueue.Union(new[] { message }));
            return this;
        }

        public IEnumerator<ActorReliableMessage> GetEnumerator()
        {
            return this.InnerQueue.GetEnumerator();
        }

        public ActorReliableMessage Peek()
        {
            return this.InnerQueue.Peek();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class OutboundReliableMessageChannel : IOutboundReliableMessageChannel
    {
        private const string DeadLetterQueue = @"fg__deadletterqueue_state";

        private const string ReliableMessageQueueStateKey = @"fg__reliablemessagequeue_state";

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
            this._stateManager = stateManager;
            this._actorProxyFactory = actorProxyFactory;
            this._messageDrop = messageDrop;
            this._loggerFactory = loggerFactory ?? DefaultLoggerFactory();
            this._actorBinder = actorBinder ?? new DefaultActorBinder();
        }

        public async Task<IEnumerable<DeadLetter>> GetDeadLetters(CancellationToken cancellationToken)
        {
            var channelState = await GetOrAddStateWithRetriesAsync(DeadLetterQueue, this._stateManager, cancellationToken);
            return channelState.Select(message => new DeadLetter(message)).ToList();
        }

        public async Task<ReliableMessage> PeekQueue(CancellationToken cancellationToken)
        {
            var channelState = await GetOrAddStateWithRetriesAsync(ReliableMessageQueueStateKey, this._stateManager, cancellationToken);
            return channelState.Peek().Message;
        }

        public async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            var channelState = await GetOrAddStateWithRetriesAsync(ReliableMessageQueueStateKey, this._stateManager, cancellationToken);
            while (channelState != null && !channelState.IsEmpty)
            {
                var actorMessage = channelState.Dequeue();
                try
                {
                    await this.SendAsync(actorMessage.Message, actorMessage.ActorReference);
                    await this._stateManager.AddOrUpdateStateAsync(ReliableMessageQueueStateKey, channelState, (k, v) => channelState, cancellationToken);

                    this._loggerFactory()
                        ?.MessageSent(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri, actorMessage.Message.Payload, actorMessage.Message.MessageType);
                }
                catch (Exception e)
                {
                    this._loggerFactory()?.FailedToSendMessage(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri, e);
                    await this.DropMessageAsync(actorMessage, cancellationToken);
                }
            }
        }

        public async Task SendMessageAsync<TActorInterface>(
            ReliableMessage message,
            ActorId actorId,
            CancellationToken cancellationToken,
            string applicationName = null,
            string serviceName = null,
            string listerName = null)
            where TActorInterface : IReliableMessageReceiverActor
        {
            var proxy = this._actorProxyFactory.CreateActorProxy<TActorInterface>(actorId, applicationName, serviceName, listerName);

            var channelState = await GetOrAddStateWithRetriesAsync(ReliableMessageQueueStateKey, this._stateManager, cancellationToken);

            channelState.Enqueue(
                new ActorReliableMessage
                    {
                        ActorReference = ActorReference.Get((IActorProxy)proxy),
                        Message = message
                    });
            await AddOrUpdateStateWithRetriesAsync(ReliableMessageQueueStateKey, channelState, this._stateManager, cancellationToken);
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        internal Task SendAsync(ReliableMessage message, ActorReference actorReference)
        {
            return this._actorBinder.Bind(actorReference).ReceiveMessageAsync(message);
        }

        private static Task AddOrUpdateStateWithRetriesAsync(
            string stateName,
            ReliableMessageChannelState state,
            IActorStateManager stateManager,
            CancellationToken cancellationToken)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.AddOrUpdateStateAsync(stateName, state, (k, v) => state, ct),
                3,
                TimeSpan.FromSeconds(1),
                cancellationToken);
        }

        private static Func<IOutboundMessageChannelLogger> DefaultLoggerFactory()
        {
            return () => null;
        }

        private static Task<ReliableMessageChannelState> GetOrAddStateWithRetriesAsync(string stateName, IActorStateManager stateManager, CancellationToken cancellationToken)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.GetOrAddStateAsync(stateName, new ReliableMessageChannelState(), ct),
                3,
                TimeSpan.FromSeconds(1),
                cancellationToken);
        }

        private async Task DropMessageAsync(ActorReliableMessage actorMessage, CancellationToken cancellationToken)
        {
            if (this._messageDrop == null)
            {
                await this.MoveToDeadLetters(actorMessage, cancellationToken);
            }
            else
            {
                await this._messageDrop(actorMessage.Message, actorMessage.ActorReference);
            }
        }

        private async Task MoveToDeadLetters(ActorReliableMessage actorMessage, CancellationToken cancellationToken)
        {
            var deadLetterState = await GetOrAddStateWithRetriesAsync(DeadLetterQueue, this._stateManager, cancellationToken);
            deadLetterState.Enqueue(actorMessage);
            await AddOrUpdateStateWithRetriesAsync(DeadLetterQueue, deadLetterState, this._stateManager, cancellationToken);
            this._loggerFactory()?.MessageMovedToDeadLetterQueue(actorMessage.Message.MessageType, actorMessage.Message.Payload, deadLetterState.Depth);
        }
    }
}