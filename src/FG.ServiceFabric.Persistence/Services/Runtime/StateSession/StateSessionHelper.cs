using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
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
		public const string ActorIdStateSchemaName = @"ACTORID";
		public const string ActorStateSchemaName = @"ACTORSTATE";
		public const string ActorReminderSchemaName = @"ACTORREMINDER";
		public const string ActorReminderCompletedSchemaName = @"ACTORREMINDERCOMPLETED";

		public const string ReliableStateQueueInfoName = @"QUEUEINFO";
		public const string ReliableStateQueueItemName = @"QUEUE-";

		private static readonly Regex RegexActorIdDetector = new Regex(@"(S{.+})|(G{[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}})|(L{[0-9]+})", RegexOptions.Compiled);

		private static readonly object _lock = new object();

		private static readonly IDictionary<string, IDictionary<Guid, string>> PartitionKeys =
			new ConcurrentDictionary<string, IDictionary<Guid, string>>();

		public static string GetServiceName(Uri serviceName)
		{
			var components = serviceName.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped).TrimStart('/').Split('/');
			return $"{components[0]}-{components[1]}";
		}

		public static async Task<string> GetPartitionInfo(ServiceContext serviceContext,
			Func<IPartitionEnumerationManager> partitionEnumerationManagerFactory)
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
					if (partition.PartitionInformation is NamedPartitionInformation namedPartitionInformation)
					{
						lock (_lock)
						{
							if (!servicePartitionKeys.ContainsKey(partitionId))
							{
								servicePartitionKeys.Add(partitionId, namedPartitionInformation.Name);
							}
						}
					}

					if ((partition.PartitionInformation is Int64RangePartitionInformation int64PartitionInformation) && !enumeratedIntPartitions)
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

					if (partition.PartitionInformation is SingletonPartitionInformation singletonPartitionInformation)
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


		public static string GetQueueKey(long index)
		{
			return $"{ReliableStateQueueItemName}{index}";
		}		
	}
}