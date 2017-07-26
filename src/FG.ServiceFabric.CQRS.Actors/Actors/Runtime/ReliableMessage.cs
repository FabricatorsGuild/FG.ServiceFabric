using System;
using System.Runtime.Serialization;
using FG.Common.Utils;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    public class ReliableMessage
    {
        public static IReliableMessageSerializer Serializer { get; set; } = new JsonReliableMessageSerializer();

        [Obsolete("Serialization only.")]
        protected ReliableMessage()
        {
        }

        protected ReliableMessage(string payload, string messageType)
        {
            Payload = payload;
            MessageType = messageType;
        }
        
        [DataMember]
        public string Payload { get; private set; }
        [DataMember]
        public string MessageType { get; private set; }

        internal object Deserialize()
        {
            return Serializer.Deserialize(Payload);
        }

        public static ReliableMessage Create<T>(T message)
        {
            return new ReliableMessage(Serializer.Serialize(message), message.GetType().FullName);
        }
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
            return JsonConvert.DeserializeObject<PayloadWrapper>(data, _settings);
        }

        public string Serialize<T>(T message)
        {
            return JsonConvert.SerializeObject(new PayloadWrapper(message), Formatting.Indented, _settings);
        }

        internal sealed class PayloadWrapper
        {
            [Obsolete("Serialization only", true)]
            public PayloadWrapper()
            {
            }

            public PayloadWrapper(object payload)
            {
                Payload = payload;
            }

            public object Payload { get; set; }
        }
    }
}