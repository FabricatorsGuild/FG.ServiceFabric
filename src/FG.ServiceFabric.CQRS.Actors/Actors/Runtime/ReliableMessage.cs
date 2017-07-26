using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    public class ReliableMessage
    {
        public static IReliableMessageSerializer Serializer { get; set; } = new JsonReliableMessageSerializer();

        private ReliableMessage()
        {
        }

        public ReliableMessage(string payload, string messageType)
        {
            Payload = payload;
            MessageType = messageType;
        }
        
        [DataMember]
        public string Payload { get; private set; }
        [DataMember]
        public string MessageType { get; private set; }
        [DataMember]
        public MessageHeader[] MessageHeaders { get; set; }

        internal object Deserialize()
        {
            return Serializer.Deserialize(Payload);
        }

        public static ReliableMessage Create<T>(T message)
        {
            return new ReliableMessage(Serializer.Serialize(message), message.GetType().FullName);
        }
    }

    public class MessageHeader
    {
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }

    public interface IReliableMessageSerializer
    {
        object Deserialize(string data);
        string Serialize<T>(T message);
    }
    
    public class JsonReliableMessageSerializer : IReliableMessageSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonReliableMessageSerializer()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        public object Deserialize(string data)
        {
            return JsonConvert.DeserializeObject(data, _settings);
        }

        public string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(message, Formatting.Indented, _settings);
        }
    }
}