using System;
using System.Runtime.Serialization;
using FG.Common.Utils;
using Newtonsoft.Json;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
	public static class StateWrapperBuilder
	{
		public static object CreateStateWrapper(string key, object state, Type stateType, IStateMetadata metadata)
		{
			var stateWrapperType = typeof(StateWrapper<>).MakeGenericType(stateType);
			var stateWrapper = stateWrapperType.ActivateCtor(key, state, metadata);
			return stateWrapper;
		}
	}

    [Serializable]
    [DataContract]
    public class StateWrapper<T>
    {
	    public StateWrapper()
	    {		    
	    }
		public StateWrapper(string key, T state, IStateMetadata metadata)
        {
	        Id = key;
            State = state;
            StateName = metadata.StateName;
            PartitionKey = metadata.PartitionKey.ToString();
        }

        [JsonProperty(PropertyName = "id")]
        [DataMember]
        public string Id { get; private set; }

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