using System;
using System.Runtime.Serialization;
using FG.Common.Utils;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    public class ReliableMessage
    {
        public static IReliableMessageSerializer Serializer { get; set; } = new DefaultReliableMessageSerializer();

        [Obsolete("Serialization only.")]
        protected ReliableMessage()
        {
        }

        protected ReliableMessage(byte[] payload, Type type)
        {
            // TODO: As AssemblyQualifiedName would be included when the JSON serializer does it work it might be something related to the data contract serializer instead. Break out into a PayloadWrapper?
            AssemblyQualifiedName = type.AssemblyQualifiedName;
            Payload = payload;
        }

        [DataMember]
        public string AssemblyQualifiedName { get; private set; }

        [DataMember]
        public byte[] Payload { get; private set; }

        internal object Deserialize()
        {
            var type = Type.GetType(this.AssemblyQualifiedName);
            return Serializer.Deserialize(type, Payload);
        }

        public static ReliableMessage Create<T>(T message)
        {
            return new ReliableMessage(Serializer.Serialize(message), message.GetType());
        }
    }

    public interface IReliableMessageSerializer
    {
        object Deserialize(Type type, byte[] data);
        byte[] Serialize<T>(T message);
    }

    // TODO: Use JSON as default serializer instead?
    public class DefaultReliableMessageSerializer : IReliableMessageSerializer
    {
        public object Deserialize(Type type, byte[] data)
        {
            return data.Deserialize(type);
        }

        public byte[] Serialize<T>(T message)
        {
            return message.Serialize();
        }
    }
}