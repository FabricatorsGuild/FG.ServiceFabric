using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Fabric;

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
        private static readonly object _lock = new object();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, string>> PartitionKeys =
            new ConcurrentDictionary<string, ConcurrentDictionary<Guid, string>>();

        public static string GetServiceName(Uri serviceName)
        {
            var components = serviceName.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped).TrimStart('/')
                .Split('/');
            return $"{components[0]}-{components[1]}";
        }

        public static async Task<string> GetPartitionInfoUncached(ServiceContext serviceContext,
            Func<IPartitionEnumerationManager> partitionEnumerationManagerFactory)
        {
            try
            {
                var partitionId = serviceContext.PartitionId;
                var serviceUri = serviceContext.ServiceName;

                var partitionKeys = new List<Partition>();

                string continuationToken = null;
                do
                {
                    var partitionEnumerationManager = partitionEnumerationManagerFactory();
                    var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
                    foreach (var partition in servicePartitionList)
                        partitionKeys.Add(partition);
                    continuationToken = servicePartitionList.ContinuationToken;
                } while (continuationToken != null);


                var singletonPartition = partitionKeys
                    .Select(p => p.PartitionInformation as SingletonPartitionInformation)
                    .FirstOrDefault(pi => pi?.Id == partitionId);
                if (singletonPartition != null)
                    return $"singleton";

                var int64RangePartition = partitionKeys
                    .Select(p => p.PartitionInformation as Int64RangePartitionInformation)
                    .Where(pi => pi != null)
                    .OrderBy(pi => pi.LowKey)
                    .Select((pi, i) => new {PartitionId = pi.Id, Name = $"range-{i}"})
                    .FirstOrDefault(pi => pi.PartitionId == partitionId);
                if (int64RangePartition != null)
                    return int64RangePartition.Name;

                var namedPartition = partitionKeys
                    .Select(p => p.PartitionInformation as NamedPartitionInformation)
                    .Where(pi => pi != null)
                    .Select((pi, i) => new {PartitionId = pi.Id, pi.Name})
                    .FirstOrDefault(pi => pi.PartitionId == partitionId);
                if (namedPartition != null)
                    return namedPartition.Name;

                throw new StateSessionException(
                    $"Could not find a matching partition for {partitionId} for service {serviceContext.ServiceName}");
            }
            catch (Exception ex)
            {
                throw new StateSessionException("tFailed to enumerate partitions", ex);
            }
        }

        public static async Task<string> GetPartitionInfo(ServiceContext serviceContext,
            Func<IPartitionEnumerationManager> partitionEnumerationManagerFactory)
        {
            try
            {
                var serviceUri = serviceContext.ServiceName;
                var serviceUriKey = serviceUri.ToString().ToLowerInvariant();

                if (PartitionKeys.TryGetValue(serviceUriKey, out var servicePartitionKeys))
                {
                    if (servicePartitionKeys.TryGetValue(serviceContext.PartitionId, out var servicePartitionKey))
                        return servicePartitionKey;
                }
                else
                {
                    servicePartitionKeys =
                        PartitionKeys.GetOrAdd(serviceUriKey, new ConcurrentDictionary<Guid, string>());
                }

                var partitionKeys = new List<Partition>();

                string continuationToken = null;
                do
                {
                    var partitionEnumerationManager = partitionEnumerationManagerFactory();
                    var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
                    partitionKeys.AddRange(servicePartitionList);

                    continuationToken = servicePartitionList.ContinuationToken;
                } while (continuationToken != null);

                var enumeratedIntPartitions = false;
                foreach (var partition in partitionKeys)
                {
                    var partitionId = partition.PartitionInformation.Id;
                    if (partition.PartitionInformation is NamedPartitionInformation namedPartitionInformation)
                        servicePartitionKeys.TryAdd(partitionId, namedPartitionInformation.Name);

                    if (partition.PartitionInformation is Int64RangePartitionInformation int64PartitionInformation &&
                        !enumeratedIntPartitions)
                    {
                        var int64RangePartitionInformations = partitionKeys
                            .Select(p => p.PartitionInformation as Int64RangePartitionInformation)
                            .Where(pi => pi != null)
                            .OrderBy(pi => pi.LowKey)
                            .ToArray();
                        for (var i = 0; i < int64RangePartitionInformations.Length; i++)
                        {
                            var int64RangePartitionInformation = int64RangePartitionInformations[i];
                            servicePartitionKeys.TryAdd(int64RangePartitionInformation.Id, $"range-{i}");
                        }

                        enumeratedIntPartitions = true;
                        break;
                    }

                    if (partition.PartitionInformation is SingletonPartitionInformation singletonPartitionInformation)
                        servicePartitionKeys.TryAdd(partitionId, $"singleton");
                }

                return PartitionKeys[serviceUriKey][serviceContext.PartitionId];
            }
            catch (Exception ex)
            {
                throw new StateSessionException("Failed to enumerate partitions", ex);
            }
        }
    }
}