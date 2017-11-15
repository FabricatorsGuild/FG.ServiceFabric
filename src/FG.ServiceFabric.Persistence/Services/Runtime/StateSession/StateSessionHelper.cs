using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FG.ServiceFabric.Fabric;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public enum StateWrapperType
	{
		ReliableQueueInfo,
		ReliableQueueItem,
		ReliableDictionaryItem,
		ActorState,
		ActorId,
		ActorReminder,
		ActorReminderCompleted,
		Unknown
	}

	public static class StateSessionHelper
	{
		private static readonly Regex RegexActorIdDetector = new Regex(@"(S{.+})|(G{[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}})|(L{[0-9]+})", RegexOptions.Compiled);

		public const string ActorIdStateSchemaName = @"ACTORID";
		public const string ActorStateSchemaName = @"ACTORSTATE";
		public const string ActorReminderSchemaName = @"ACTORREMINDER";
		public const string ActorReminderCompletedSchemaName = @"ACTORREMINDERCOMPLETED";

		public const string ReliableStateQueueInfoName = @"queue-info";


		public class SchemaStateKey
		{
			private static readonly Regex RegexActorSchemaStateKeySplitter = new Regex(@"(?'service'[a-zA-Z0-9\-\.]+)_(?'partition'[a-zA-Z0-9\-]+)_(?'schema'[a-zA-Z0-9]+)_(?'key'.+)", RegexOptions.Compiled);

			public SchemaStateKey(string serviceName, string partitionKey, string schema = null, string key = null)
			{
				ServiceName = serviceName;
				PartitionKey = partitionKey;
				Schema = schema;
				Key = key;
			}

			public static SchemaStateKey Parse(string schemaStateKey)
			{
				var match = RegexActorSchemaStateKeySplitter.Match(schemaStateKey);
				if (!match.Success)
				{
					throw new NotSupportedException($"The key {schemaStateKey} cannot be parsed as a SchemaStateKey");
				}

				var serviceName = match.Groups["service"]?.Value;
				var partititonKey = match.Groups["partition"]?.Value;
				var schema = match.Groups["schema"]?.Value;
				var key = match.Groups["key"]?.Value;

				return new SchemaStateKey(serviceName, partititonKey, schema, key);
			}

			public string ServiceName { get; set; }
			public string PartitionKey { get; set; }
			public string Schema { get; set; }
			public string Key { get; set; }

			public override string ToString()
			{
				var value = new StringBuilder();
				if (ServiceName != null)
				{
					value = value.Append(ServiceName).Append("_");
					if (PartitionKey != null)
					{
						value = value.Append(PartitionKey).Append("_");
						if (Schema != null)
						{
							value = value.Append(Schema).Append("_");
							if (Key != null)
							{
								value = value.Append(Key).Append("_");
							}
						}
					}					
				}
				return value.ToString();
			}
		}

		private static object _lock = new object();
		private static readonly IDictionary<string, IDictionary<Guid, string>> PartitionKeys = new ConcurrentDictionary<string, IDictionary<Guid, string>>();

		public static string GetServiceName(Uri serviceName)
		{
			var components = serviceName.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped).TrimStart('/').Split('/');
			return $"{components[0]}-{components[1]}";
		}

		public static string GetSchemaPrefix(string serviceName, string partitionKey)
		{
			var stateKeyPrefix = $"{serviceName}_{partitionKey}_";
			return stateKeyPrefix;
		}
		public static string GetSchemaStateKey(string serviceName, string partitionKey, string schema, string stateName)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(serviceName, partitionKey, schema)}{stateName}";
			return stateKey;
		}
		public static string GetSchemaStateKeyPrefix(string serviceName, string partitionKey, string schema)
		{
			var stateKeyPrefix = $"{GetSchemaPrefix(serviceName, partitionKey)}{schema}_";
			return stateKeyPrefix;
		}

		public static string GetSchemaStateQueueInfoKey(string serviceName, string partitionKey, string schema)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(serviceName, partitionKey, schema)}{ReliableStateQueueInfoName}";
			return stateKey;
		}		

		public static string GetSchemaQueueStateKey(string serviceName, string partitionKey, string schema, long index)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(serviceName, partitionKey, schema)}{index}";
			return stateKey;
		}

		public static async Task<string> GetPartitionInfo(ServiceContext serviceContext, Func<IPartitionEnumerationManager> partitionEnumerationManagerFactory)
		{
			try
			{
				var serviceUri = serviceContext.ServiceName;
				var serviceUriKey = serviceUri.ToString().ToLowerInvariant();

				var servicePartitionKeys = default(IDictionary<Guid, string>);
				var hasKey = false;
				lock (_lock)
				{
					hasKey = PartitionKeys.ContainsKey(serviceUriKey);
				}
				if (hasKey)
				{
					servicePartitionKeys = PartitionKeys[serviceUriKey];
					if (servicePartitionKeys.ContainsKey(serviceContext.PartitionId))
					{
						return servicePartitionKeys[serviceContext.PartitionId];
					}
				}
				else
				{
					lock (_lock)
					{
						if (PartitionKeys.ContainsKey(serviceUriKey))
						{
							servicePartitionKeys = PartitionKeys[serviceUriKey];
							if (servicePartitionKeys.ContainsKey(serviceContext.PartitionId))
							{
								return servicePartitionKeys[serviceContext.PartitionId];
							}
						}

						servicePartitionKeys = new ConcurrentDictionary<Guid, string>();
						PartitionKeys.Add(serviceUriKey, servicePartitionKeys);
					}
				}

				var partitionKeys = new List<Partition>();
				
				string continuationToken = null;
				do
				{
					var partitionEnumerationManager = partitionEnumerationManagerFactory();
					var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
					foreach (var partition in servicePartitionList)
					{
						partitionKeys.Add(partition);
					}
					continuationToken = servicePartitionList.ContinuationToken;
				} while (continuationToken != null);

				var enumeratedIntPartitions = false;
				foreach (var partition in partitionKeys)
				{
					var partitionId = partition.PartitionInformation.Id;
					var namedPartitionInformation = partition.PartitionInformation as NamedPartitionInformation;
					if (namedPartitionInformation != null)
					{
						lock (_lock)
						{
							if (!servicePartitionKeys.ContainsKey(partitionId))
							{
								servicePartitionKeys.Add(partitionId, namedPartitionInformation.Name);
							}
						}
					}

					var int64PartitionInformation = partition.PartitionInformation as Int64RangePartitionInformation;
					if ((int64PartitionInformation != null) && !enumeratedIntPartitions)
					{
						var int64RangePartitionInformations = partitionKeys
							.Select(p => p.PartitionInformation as Int64RangePartitionInformation)
							.Where(pi => pi != null)
							.OrderBy(pi => pi.LowKey)
							.ToArray();
						for (var i = 0; i < int64RangePartitionInformations.Length; i++)
						{
							lock (_lock)
							{
								var int64RangePartitionInformation = int64RangePartitionInformations[i];
								if (!servicePartitionKeys.ContainsKey(int64RangePartitionInformation.Id))
								{
									servicePartitionKeys.Add(int64RangePartitionInformation.Id, $"range-{i}");
								}
							}
						}
						enumeratedIntPartitions = true;
						break;
					}

					var singletonPartitionInformation = partition.PartitionInformation as SingletonPartitionInformation;
					if (singletonPartitionInformation != null)
					{
						lock (_lock)
						{
							if (!servicePartitionKeys.ContainsKey(partitionId))
							{
								servicePartitionKeys.Add(partitionId, $"singleton");
							}
						}
					}				
				}

				return PartitionKeys[serviceUriKey][serviceContext.PartitionId];
			}
			catch (Exception ex)
			{
				throw new StateSessionException("Failed to enumerate partitions", ex);
			}
		}

		public static string GetActorIdSchemaKey(ActorId actorId)
		{
			var key = actorId.ToString();
			switch (actorId.Kind)
			{
				case ActorIdKind.Long:
					key = $"L{{{actorId.ToString()}}}";
					break;

				case ActorIdKind.Guid:
					key = $"G{{{actorId.GetGuidId()}}}";
					break;

				case ActorIdKind.String:
					key = $"S{{{actorId.GetStringId()}}}";
					break;
			}
			return key;
		}
		
		public static ActorId TryGetActorIdFromSchemaKey(string schemaKey)
		{
			var match = RegexActorIdDetector.Match(schemaKey);
			if (match.Success)
			{
				var value = match.Value;

				var kind = schemaKey.Substring(0, 1);
				var id = schemaKey.Substring(2, value.Length - 3);

				if (kind.Equals("G", StringComparison.OrdinalIgnoreCase))
				{
					return new ActorId(Guid.Parse(id));
				}

				if (kind.Equals("L", StringComparison.OrdinalIgnoreCase))
				{
					return new ActorId(long.Parse(id));
				}

				if (kind.Equals("S", StringComparison.OrdinalIgnoreCase))
				{
					return new ActorId(id);
				}
			}

			return null;
		}

		public static string GetActorReminderSchemaKey(ActorId actorId, string reminderName)
		{
			return $"{StateSessionHelper.GetActorIdSchemaKey(actorId)}-{reminderName}";
		}

		public static string GetActorStateSchemaName(string actorStateName)
		{
			return $"{ActorStateSchemaName}-{actorStateName}";
		}
	}
}