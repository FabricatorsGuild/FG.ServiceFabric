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
    public class When_processing_message_queue : ReliableMessgeTestBase
    {
        private IndexCommand _message1;
        private IndexCommand _message2;
        private IndexCommand _message3;
        
        [SetUp]
        public async Task SendMessage()
        {
            _message1 = new IndexCommand { PersonId = Guid.NewGuid() };
            _message2 = new IndexCommand { PersonId = Guid.NewGuid() };
            _message3 = new IndexCommand { PersonId = Guid.NewGuid() };

            await SendMessage(ReliableMessage.Create(_message1));
            await SendMessage(ReliableMessage.Create(_message2));
            await SendMessage(ReliableMessage.Create(_message3));
            
            await OutboundChannel.ProcessQueueAsync();
        }

        private async Task SendMessage(ReliableMessage message)
        {
            await OutboundChannel.SendMessageAsync<IPersonIndexActor>(
                message, new ActorId("PersonIndex"),
                FabricRuntime.ApplicationName);
        }

        [Test]
        public async Task Then_receiver_gets_message()
        {
            var proxy = ActorProxyFactory.CreateActorProxy<IPersonIndexActor>(new ActorId("PersonIndex"),
                FabricRuntime.ApplicationName);

            var list = await proxy.ListCommandsAsync();
            list.Should().Contain(_message1.CommandId);
        }

        [Test]
        public async Task Then_all_messages_are_delivered_in_order()
        {
            var proxy = ActorProxyFactory.CreateActorProxy<IPersonIndexActor>(new ActorId("PersonIndex"),
                FabricRuntime.ApplicationName);

            var list = await proxy.ListCommandsAsync();
            list.ShouldBeEquivalentTo(new [] {_message1.CommandId, _message2.CommandId, _message3.CommandId});
        }
    }
}