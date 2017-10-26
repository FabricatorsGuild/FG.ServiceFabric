using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;
using ActorReference = FG.ServiceFabric.Actors.ActorReference;

// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
{
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
            await OutboundChannel.SendMessageAsync<IIndexActor>(message, new ActorId("PersonIndex"), CancellationToken.None, this.ApplicationName);
            await OutboundChannel.ProcessQueueAsync(CancellationToken.None);
        }

        [Test]
        public void Then_messsage_is_delivered_to_message_drop_handler()
        {
            _droppedMessages.Count.Should().Be(1);
        }
    }
}