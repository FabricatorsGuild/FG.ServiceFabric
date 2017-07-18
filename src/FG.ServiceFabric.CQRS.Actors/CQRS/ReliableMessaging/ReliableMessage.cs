using System;
using System.Runtime.Serialization;
using FG.Common.Utils;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    [DataContract]
    public class ReliableMessage
    {
        [Obsolete("Serialization only.")]
        protected ReliableMessage() { }
        
        protected ReliableMessage(byte[] payload, Type type)
        {
            AssemblyQualifiedName = type.AssemblyQualifiedName;
            Payload = payload;
        }

        [DataMember]
        // ReSharper disable once InconsistentNaming
        public string AssemblyQualifiedName { get; private set; }
        
        [DataMember]
        public byte[] Payload { get; private set; }

        [DataMember]
        public object Payload2 { get; private set; }

        public virtual object Deserialize()
        {
            var type = Type.GetType(this.AssemblyQualifiedName);
            return Payload.Deserialize(type);
        }

        public virtual T Deserialize<T>()
        {
            return Payload.Deserialize<T>();
        }

        public static ReliableMessage Create<T>(T message)
        {
            return new ReliableMessage(message.Serialize(), message.GetType());
        }
    }
}
