using System;
using System.Runtime.Serialization;
using FG.Common.Utils;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	[Serializable]
	[DataContract]
	public class IdWrapper
	{
		public IdWrapper()
		{
		}

		public IdWrapper(string id)
		{
			Id = id;
		}

		[JsonProperty(PropertyName = "id")]
		[DataMember]
		public string Id { get; private set; }
	}

	[Serializable]
	[DataContract]
	public class StateWrapper<T> : StateWrapper
	{
		public StateWrapper()
		{
		}

		public StateWrapper(string id, T state, IServiceMetadata serviceMetadata, IValueMetadata valueMetadata)
			: base(id, serviceMetadata, valueMetadata)
		{
			State = state;
		}

		[JsonProperty(PropertyName = "state")]
		[DataMember]
		public T State { get; set; }
	}

	[Serializable]
	[DataContract]
	public class StateWrapper : IdWrapper
	{
		public StateWrapper()
		{
		}

		public StateWrapper(string id, IServiceMetadata serviceMetadata, IValueMetadata valueMetadata)
			: base(id)
		{
			Key = valueMetadata.Key;
			Schema = valueMetadata.Schema;
			Type = valueMetadata.Type;
			ServiceTypeName = serviceMetadata.ServiceName;
			PartitionKey = serviceMetadata.PartitionKey;
		}

		[JsonProperty(PropertyName = "serviceTypeName")]
		[DataMember]
		public string ServiceTypeName { get; private set; }

		[JsonProperty(PropertyName = "partitionKey")]
		[DataMember]
		public string PartitionKey { get; private set; }

		[JsonProperty(PropertyName = "schema")]
		[DataMember]
		public string Schema { get; private set; }

		[JsonProperty(PropertyName = "key")]
		[DataMember]
		public string Key { get; private set; }

		[JsonProperty(PropertyName = "type")]
		[DataMember]
		public string Type { get; private set; }
	}
}