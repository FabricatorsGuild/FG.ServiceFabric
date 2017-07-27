using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace FG.ServiceFabric.Tests.CQRS
{
    public class When_sending_a_reliable_message : ReliableMessgeTestBase
    {
        private ReliableMessage _message;

        protected override void SetupRuntime()
        {
            SetupPersonIndexActor(FabricRuntime);
        }

        [SetUp]
        public async Task SendMessage()
        {
            _message = ReliableMessage.Create(new IndexCommand {PersonId = Guid.NewGuid()});

            await OutboundChannel.SendMessageAsync<IPersonIndexActor>(
                _message, new ActorId("PersonIndex"),
                FabricRuntime.ApplicationName);
        }

        [Test]
        public async Task Then_message_gets_put_on_queue()
        {
            var message = await OutboundChannel.PeekQueue();
            message.ShouldBeEquivalentTo(_message);
        }
    }
}
