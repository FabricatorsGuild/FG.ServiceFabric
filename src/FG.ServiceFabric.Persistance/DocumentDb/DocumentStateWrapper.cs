using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace FG.ServiceFabric.DocumentDb
{
    [Serializable]
    [DataContract]
    public class DocumentStateWrapper<T>
    {
        [JsonProperty(PropertyName = "id")]
        [DataMember]
        public string Id { get; set; }
        
        [DataMember]
        public string StateName { get; set; }

        [DataMember]
        public T Payload { get; set; }
    }
}