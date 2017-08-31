using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public static class StateSessionHelper
	{

		public static string GetSchemaStateKey(string serviceName, string partitionKey, string schema, string stateName)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(serviceName, partitionKey, schema)}{stateName}";
			return stateKey;
		}
		public static string GetSchemaStateKeyPrefix(string serviceName, string partitionKey, string schema)
		{
			var stateKeyPrefix = $"@@{serviceName}_{partitionKey}_{schema}_";
			return stateKeyPrefix;
		}
		public static string GetSchemaStateQueueInfoKey(string serviceName, string partitionKey, string schema)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(serviceName, partitionKey, schema)}_queue_info";
			return stateKey;
		}
		public static string GetSchemaQueueStateKey(string serviceName, string partitionKey, string schema, long index)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(serviceName, partitionKey, schema)}_{index}";
			return stateKey;
		}

		public static string GetActorStateName(ActorId actorId, string stateName)
		{
			var stateKey = $"{GetActorStateNamePrefix(actorId)}{stateName}";
			return stateKey;
		}

		public static string GetActorStateNamePrefix(ActorId actorId)
		{
			var stateKeyPrefix = $"{actorId}_";
			return stateKeyPrefix;
		}

		public static string GetActorIdStateName(ActorId actorId)
		{
			var stateKey = $"{GetActorIdStateNamePrefix()}{actorId}";
			return stateKey;
		}

		public static string GetActorIdStateNamePrefix()
		{
			var stateKeyPrefix = $"actorid_";
			return stateKeyPrefix;
		}

		public static async Task<string> GetPartitionInfo(ServiceContext serviceContext)
		{
			try
			{
				var serviceUri = serviceContext.ServiceName;

				var fabricClient = new FabricClient();
				var partitionKeys = new List<Partition>();
				
				string continuationToken = null;
				do
				{
					var servicePartitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);
					foreach (var partition in servicePartitionList)
					{
						partitionKeys.Add(partition);
					}
					continuationToken = servicePartitionList.ContinuationToken;
				} while (continuationToken != null);

				foreach (var partition in partitionKeys)
				{
					if (partition.PartitionInformation.Id == serviceContext.PartitionId)
					{
						var namedPartitionInformation = partition.PartitionInformation as NamedPartitionInformation;
						if (namedPartitionInformation != null)
						{
							return namedPartitionInformation.Name;
						}

						var int64PartitionInformation = partition.PartitionInformation as Int64RangePartitionInformation;
						if (int64PartitionInformation != null)
						{
							var int64RangePartitionInformations = partitionKeys
								.Select(p => p.PartitionInformation as Int64RangePartitionInformation)
								.Where(pi => pi != null)
								.OrderBy(pi => pi.LowKey)
								.ToArray();
							for (var i = 0; i < int64RangePartitionInformations.Length; i++)
							{
								if (int64RangePartitionInformations[i].Id == serviceContext.PartitionId)
								{
									return $"range-{i}";
								}
							}
						}

						var singletonPartitionInformation = partition.PartitionInformation as SingletonPartitionInformation;
						if (singletonPartitionInformation != null)
						{
							return $"singleton";
						}
						break;
					}

				}

				return serviceContext.PartitionId.ToString();
			}
			catch (Exception ex)
			{
				throw;
			}
		}


	}
}