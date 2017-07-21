using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    internal sealed class ActorReliableMessage
    {
        [DataMember]
        public ActorReference ActorReference { get; set; }

        [DataMember]
        public ReliableMessage Message { get; set; }
    }

    [DataContract]
    internal sealed class ReliableMessageChannelState
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
    }

    public class OutboundReliableMessageChannel : IOutboundReliableMessageChannel
    {
        private readonly IActorStateManager _stateManager;
        private readonly IActorProxyFactory _actorProxyFactory;
        private readonly Func<IOutboundMessageChannelLogger> _loggerFactory;

        private readonly string _reliableMessageQueueStateKey = @"fg__reliablemessagequeue_state";
        private readonly string _deadLetterQueue = @"fg__deadletterqueue_state";

        public OutboundReliableMessageChannel(IActorStateManager stateManager, IActorProxyFactory actorProxyFactory,
            Func<IOutboundMessageChannelLogger> loggerFactory = null)
        {
            _stateManager = stateManager;
            _actorProxyFactory = actorProxyFactory;
            _loggerFactory = loggerFactory ?? DefaultLoggerFactory();
        }

        private static Func<IOutboundMessageChannelLogger> DefaultLoggerFactory()
        {
            return () => new NullOpOutboundMessageChannelLogger();
        }

        // TODO: Use JSON as default serializer instead?
        public async Task SendMessageAsync<TActorInterface>(ReliableMessage message, ActorId actorId,
            string applicationName = null, string serviceName = null, string listerName = null)
            where TActorInterface : IReliableMessageReceiverActor
        {
            var proxy = _actorProxyFactory.CreateActorProxy<TActorInterface>(actorId, applicationName, serviceName,
                listerName);

            var channelState = await GetOrAddStateWithRetriesAsync(_reliableMessageQueueStateKey, _stateManager);
            channelState.Enqueue(new ActorReliableMessage
            {
                ActorReference = ActorReference.Get(proxy),
                Message = message
            });
            await AddOrUpdateStateWithRetriesAsync(_reliableMessageQueueStateKey, channelState, _stateManager);
        }

        public async Task ProcessQueueAsync()
        {
            var channelState = await GetOrAddStateWithRetriesAsync(_reliableMessageQueueStateKey, _stateManager);
            while (channelState != null && !channelState.IsEmpty)
            {
                var actorMessage = channelState.Dequeue();
                try
                {
                    await SendAsync(actorMessage.Message, actorMessage.ActorReference);
                    await AddOrUpdateStateWithRetriesAsync(_reliableMessageQueueStateKey, channelState, _stateManager);

                    _loggerFactory().SentMessage(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri, actorMessage.Message.AssemblyQualifiedName);
                }
                catch (Exception e)
                {
                    // TODO: Retry logic?
                    // TODO: Make it possible to modify persisted commands (through api) before retrying?
                    _loggerFactory().FailedToSendMessage(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri, e);

                    var deadLetterState = await GetOrAddStateWithRetriesAsync(_deadLetterQueue, _stateManager);
                    deadLetterState.Enqueue(actorMessage);
                    await AddOrUpdateStateWithRetriesAsync(_deadLetterQueue, channelState, _stateManager);

                    // TODO: Manual retries?
                    _loggerFactory().MovedToDeadLetters(deadLetterState.Depth);
                }
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static Task SendAsync(ReliableMessage message, ActorReference actorReference)
        {
            return ((IReliableMessageReceiverActor) actorReference
                    .Bind(typeof(IReliableMessageReceiverActor)))
                .ReceiveMessageAsync(message);
        }

        private static Task AddOrUpdateStateWithRetriesAsync(string stateName, ReliableMessageChannelState state,
            IActorStateManager stateManager)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.AddOrUpdateStateAsync(stateName, state, (k, v) => state, ct), 3,
                TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        private static Task<ReliableMessageChannelState> GetOrAddStateWithRetriesAsync(string stateName,
            IActorStateManager stateManager)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.GetOrAddStateAsync(stateName, new ReliableMessageChannelState(), ct),
                3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
    }
}