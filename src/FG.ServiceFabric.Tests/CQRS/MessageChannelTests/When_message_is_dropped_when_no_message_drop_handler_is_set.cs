using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
{
    public class When_message_is_dropped_when_no_message_drop_handler_is_set : ReliableMessgeTestBase
    {
        [SetUp]
        public void SetupChanelWithFailingActorBinder()
        {
            OutboundChannel = new OutboundReliableMessageChannel(new MockActorStateManager(),
                FabricRuntime.ActorProxyFactory, null, null , new MockFailingActorBinder());
        }
        
        [SetUp]
        public async Task SendMessage()
        {
            var message = ReliableMessage.Create(new IndexCommand { PersonId = Guid.NewGuid() });
            await OutboundChannel.SendMessageAsync<IIndexActor>(message, new ActorId("PersonIndex"), CancellationToken.None, _fabricApplication.ApplicationInstanceName);
            await OutboundChannel.ProcessQueueAsync(CancellationToken.None);
        }

        [Test]
        public async Task Then_messsage_is_delivered_to_dead_letter_queue()
        {
            var deadLetters = await OutboundChannel.GetDeadLetters(CancellationToken.None);
            deadLetters.Should().HaveCount(1);
        }
    }
}