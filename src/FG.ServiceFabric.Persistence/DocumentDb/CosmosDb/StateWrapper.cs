using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
    [Serializable]
    [DataContract]
    public class StateWrapper<T> where T : IPersistedIdentity
    {
        public StateWrapper(T state, IStateMetadata metadata)
        {
            State = state;
            StateName = metadata.StateName;
            PartitionKey = metadata.PartitionKey.ToString();
        }

        [JsonProperty(PropertyName = "id")]
        [DataMember]
        public string Id => State.Id;

        [JsonProperty(PropertyName = "partitionKey")]
        [DataMember]
        public string PartitionKey { get; private set; }

        [JsonProperty(PropertyName = "stateName")]
        [DataMember]
        public string StateName { get; private set; }
        
        [JsonProperty(PropertyName = "state")]
        [DataMember]
        public T State { get; set; }
    }
}