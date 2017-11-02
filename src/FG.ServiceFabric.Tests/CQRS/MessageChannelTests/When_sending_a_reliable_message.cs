using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace FG.ServiceFabric.Tests.CQRS.MessageChannelTests
{
    public class When_sending_a_reliable_message : ReliableMessgeTestBase
    {
        private ReliableMessage _message;
        
        [SetUp]
        public async Task SendMessage()
        {
            _message = ReliableMessage.Create(new IndexCommand {PersonId = Guid.NewGuid()});

            await OutboundChannel.SendMessageAsync<IIndexActor>(
                _message, new ActorId("PersonIndex"), 
                CancellationToken.None,
	            _fabricApplication.ApplicationInstanceName);
        }

        [Test]
        public async Task Then_message_gets_put_on_queue()
        {
            var message = await OutboundChannel.PeekQueue(CancellationToken.None);
            message.ShouldBeEquivalentTo(_message);
        }
    }
}
