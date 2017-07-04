using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using FG.ServiceFabric.Fabric;

namespace FG.ServiceFabric.Services.Runtime
{
    public static class ServiceContextExtensions
    {
        private const int MaxQueryRetryCount = 20;
        private static readonly TimeSpan BackoffQueryDelay = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Returns a list of service partition clients pointing to one key in each of the WordCount service partitions.
        /// The returned representative key is the min key served by each partition.
        /// </summary>
        /// <returns>The service partition clients pointing at a key in each of the WordCount service partitions.</returns>
        public static Task<IList<Int64RangePartitionInformation>> GetServicePartitionKeysAsync(IPartitionEnumerationManager partitionEnumerationManager, ServiceContext context)
        {
            return GetServicePartitionKeysAsync(partitionEnumerationManager, context.ServiceName);
        }

        public static async Task<IList<Int64RangePartitionInformation>> GetServicePartitionKeysAsync(IPartitionEnumerationManager partitionEnumerationManager, Uri serviceName)
        {
            for (var i = 0; i < MaxQueryRetryCount; i++)
            {
                try
                {
                    // Get the list of partitions up and running in the service.
                    var partitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceName);

                    // For each partition, build a service partition client used to resolve the low key served by the partition.
                    IList<Int64RangePartitionInformation> partitionKeys = new List<Int64RangePartitionInformation>(partitionList.Count);
                    foreach (var partition in partitionList)
                    {
                        var partitionInfo = partition.PartitionInformation as Int64RangePartitionInformation;
                        if (partitionInfo == null)
                        {
                            throw new InvalidOperationException(
                                $"The service {serviceName} should have a uniform Int64 partition. Instead: {partition.PartitionInformation.Kind}");
                        }

                        partitionKeys.Add(partitionInfo);
                    }

                    return partitionKeys;
                }
                catch (FabricTransientException)
                {
                    //ServiceEventSource.Current.OperationFailed(ex.Message, "create representative partition clients");
                    if (i == MaxQueryRetryCount - 1)
                    {
                        throw;
                    }
                }

                await Task.Delay(BackoffQueryDelay);
            }

            throw new TimeoutException("Retry timeout is exhausted and creating representative partition clients wasn't successful");
        }
    }
}