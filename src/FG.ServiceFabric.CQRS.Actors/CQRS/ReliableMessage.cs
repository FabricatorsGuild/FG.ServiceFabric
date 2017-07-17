using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.CQRS
{
    [DataContract]
    public class ReliableActorMessage : ReliableMessage
    {
        [Obsolete("Serialization only.")]
        public ReliableActorMessage() { }

        public ReliableActorMessage(ICommand command, ActorReference actorReference) : base(command)
        {
            ActorReference = actorReference;
        }

        [DataMember]
        public ActorReference ActorReference { get; private set; }
    }
    
    [DataContract]
    public abstract class ReliableMessage
    {
        [Obsolete("Serialization only.")]
        protected ReliableMessage() { }

        protected ReliableMessage(object command)
        {
            CLRType = command.GetType().FullName;
            Payload =  command.Serialize();
        }
        
        [DataMember]
        // ReSharper disable once InconsistentNaming
        public string CLRType { get; private set; }
        
        [DataMember]
        public byte[] Payload { get; private set; }

        public virtual object Deserialize()
        {
            return Payload.Deserialize(Type.GetType(this.CLRType));
        }
    }

    public interface IReliableMessageReceiver : IActor
    {
        Task ReceiveMessageAsync(ReliableMessage message);
    }

    //public interface IReliableMessageSender : IActor
    //{
    //    Task SendMessageAsync(ReliableActorMessage message);
    //}
}
