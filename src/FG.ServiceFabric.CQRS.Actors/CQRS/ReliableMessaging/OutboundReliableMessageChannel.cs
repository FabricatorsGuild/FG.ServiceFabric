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

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    [DataContract]
    internal sealed class ReliableMessageChannelState
    {
        public ReliableMessageChannelState()
        {
            InnerQueue = new Queue<ActorReliableMessage>();
        }

        public ReliableMessageChannelState(ActorReliableMessage message) : this()
        {
            InnerQueue = new Queue<ActorReliableMessage>(new[] { message });
        }

        [DataMember]
        private Queue<ActorReliableMessage> InnerQueue { get; set; }

        public bool IsEmpty => InnerQueue.Count == 0;
        public int Depth => InnerQueue.Count;

        public ReliableMessageChannelState Enqueue(ActorReliableMessage message)
        {
            InnerQueue = new Queue<ActorReliableMessage>(InnerQueue.Union(new[] { message }));
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
        private readonly IReliableMessageChannelLogger _logger;
        private string _reliableMessageQueueStateKey = @"fg__reliablemessagequeue_state";
        private string _deadLetterQueue = @"fg__deadLetterqueue_state";

        public OutboundReliableMessageChannel(IActorStateManager stateManager, IActorProxyFactory actorProxyFactory, IReliableMessageChannelLogger logger)
        {
            _stateManager = stateManager;
            _actorProxyFactory = actorProxyFactory;
            _logger = logger;
        }
        
        public async Task SendMessageAsync<TActorInterface>(ReliableMessage message, ActorId actorId, string applicationName = null, string serviceName = null, string listerName = null) 
            where TActorInterface : IReliableMessageReceiverActor
        {
            var proxy = _actorProxyFactory.CreateActorProxy<TActorInterface>(actorId, applicationName, serviceName, listerName);

            var channelState = await GetOrAddStateAsync(_reliableMessageQueueStateKey, _stateManager);
            channelState.Enqueue(new ActorReliableMessage { ActorReference = ActorReference.Get(proxy), Message = message});
            await AddOrUpdateState(_reliableMessageQueueStateKey, channelState, _stateManager);
        }
        
        public async Task ProcessQueueAsync()
        {
            var channelState = await GetOrAddStateAsync(_reliableMessageQueueStateKey, _stateManager);
            while (channelState != null && !channelState.IsEmpty)
            {
                var actorMessage = channelState.Dequeue();
                try
                {
                    await SendMessageAsync(actorMessage.Message, actorMessage.ActorReference);
                    await AddOrUpdateState(_reliableMessageQueueStateKey, channelState, _stateManager);
                }
                catch (Exception e)
                {
                    //_logger.FailedToSendMessage(actorMessage.ActorReference.ActorId, actorMessage.ActorReference.ServiceUri, e);
                    var deadLetterState = await GetOrAddStateAsync(_deadLetterQueue, _stateManager);
                    deadLetterState.Enqueue(actorMessage);
                    await AddOrUpdateState(_deadLetterQueue, channelState, _stateManager);
                    //_logger.MovedToDeadLetters(deadLetterState.Depth);
                }
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static Task SendMessageAsync(ReliableMessage message, ActorReference actorReference)
        {
            return ((IReliableMessageReceiverActor)actorReference
                .Bind(typeof(IReliableMessageReceiverActor)))
                .ReceiveMessageAsync(message);
        }
        
        private static Task AddOrUpdateState(string stateName, ReliableMessageChannelState state, IActorStateManager stateManager)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.AddOrUpdateStateAsync(stateName, state, (k, v) => state, ct), 3,
                TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        private static Task<ReliableMessageChannelState> GetOrAddStateAsync(string stateName, IActorStateManager stateManager)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
                ct => stateManager.GetOrAddStateAsync(stateName, new ReliableMessageChannelState(), ct),
                3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
    }
}