using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
{
    public class MockFailingActorBinder : IReceiverActorBinder
    {
        public IReliableMessageReceiverActor Bind(ActorReference actorReference)
        {
            throw new Exception("Boom!");
        }
    }

    public class When_message_is_dropped : ReliableMessgeTestBase
    {
        [SetUp]
        public void SetupChanelWithFailingActorBinder()
        {
            OutboundChannel = new OutboundReliableMessageChannel(new MockActorStateManager(),
                FabricRuntime.ActorProxyFactory, null, HandleDroppedMessage , new MockFailingActorBinder());
        }

        private readonly List<ReliableMessage> _droppedMessages = new List<ReliableMessage>();
        private Task HandleDroppedMessage(ReliableMessage message, ActorReference actorReference)
        {
            _droppedMessages.Add(message);
            return Task.FromResult(true);
        }

        [SetUp]
        public async Task SendMessage()
        {
            var message = ReliableMessage.Create(new IndexCommand { PersonId = Guid.NewGuid() });
            await OutboundChannel.SendMessageAsync<IIndexActor>(message, new ActorId("PersonIndex"), CancellationToken.None, FabricRuntime.ApplicationName);
            await OutboundChannel.ProcessQueueAsync(CancellationToken.None);
        }

        [Test]
        public void Then_messsage_is_delivered_to_dead_letter_queue()
        {
            _droppedMessages.Count.Should().Be(1);
        }
    }
}