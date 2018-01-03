namespace FG.ServiceFabric.Fabric
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PartitionHelper
    {
        private readonly ConcurrentDictionary<Uri, Int64RangePartitionInformation[]> _int64Partitions;

        private readonly ConcurrentDictionary<Uri, NamedPartitionInformation[]> _namedPartitions;

        private readonly Func<IPartitionEnumerationManager> _partitionEnumerationManagerFactory;

        public PartitionHelper(Func<IPartitionEnumerationManager> partitionEnumerationManagerFactory)
        {
            this._partitionEnumerationManagerFactory = partitionEnumerationManagerFactory;
            this._int64Partitions = new ConcurrentDictionary<Uri, Int64RangePartitionInformation[]>();
            this._namedPartitions = new ConcurrentDictionary<Uri, NamedPartitionInformation[]>();
        }

        // ReSharper disable once UnusedMember.Global - Used for logging!
        public static string ToString(IEnumerable<ServicePartitionInformation> partitions)
        {
            var partitionsString = new StringBuilder();
            var delimiter = string.Empty;
            foreach (var partition in partitions)
            {
                partitionsString.Append(delimiter);
                if (partition is Int64RangePartitionInformation int64Partition)
                {
                    partitionsString.Append($"{int64Partition.LowKey}-{int64Partition.HighKey}");
                }

                if (partition is NamedPartitionInformation namedPartition)
                {
                    partitionsString.Append($"{namedPartition.Name}");
                }

                delimiter = ",";
            }

            return partitionsString.ToString();
        }

        public async Task<IEnumerable<Int64RangePartitionInformation>> GetInt64Partitions(Uri serviceUri, IPartitionHelperLogger logger)
        {
            logger.EnumeratingPartitions(serviceUri);

            if (this._int64Partitions.TryGetValue(serviceUri, out var partitions))
            {
                logger.EnumeratedExistingPartitions(serviceUri, partitions);
                return partitions;
            }

            try
            {
                var partitionEnumerationManager = this._partitionEnumerationManagerFactory();
                var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
                IList<Int64RangePartitionInformation> partitionKeys = new List<Int64RangePartitionInformation>(servicePartitionList.Count);
                foreach (var partition in servicePartitionList)
                {
                    if (!(partition.PartitionInformation is Int64RangePartitionInformation partitionInfo))
                    {
                        throw new InvalidOperationException($"The service {serviceUri} should have a uniform Int64 partition. Instead: {partition.PartitionInformation.Kind}");
                    }

                    partitionKeys.Add(partitionInfo);
                }

                this._int64Partitions.GetOrAdd(serviceUri, su => partitionKeys.ToArray());

                logger.EnumeratedAndCachedPartitions(serviceUri, partitionKeys);
                return partitionKeys;
            }
            catch (Exception ex)
            {
                logger.FailedToEnumeratePartitions(serviceUri, ex);
                throw new PartitionEnumerationException(serviceUri, ex);
            }
        }

        public async Task<IEnumerable<NamedPartitionInformation>> GetNamedPartitions(Uri serviceUri, IPartitionHelperLogger logger)
        {
            logger.EnumeratingPartitions(serviceUri);

            if (this._namedPartitions.TryGetValue(serviceUri, out var partitions))
            {
                logger.EnumeratedExistingPartitions(serviceUri, partitions);
                return partitions;
            }

            try
            {
                var partitionEnumerationManager = this._partitionEnumerationManagerFactory();
                var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
                IList<NamedPartitionInformation> partitionKeys = new List<NamedPartitionInformation>(servicePartitionList.Count);
                foreach (var partition in servicePartitionList)
                {
                    if (!(partition.PartitionInformation is NamedPartitionInformation partitionInfo))
                    {
                        throw new InvalidOperationException($"The service {serviceUri} should have a Named partition. Instead: {partition.PartitionInformation.Kind}");
                    }

                    partitionKeys.Add(partitionInfo);
                }

                this._namedPartitions.GetOrAdd(serviceUri, su => partitionKeys.ToArray());

                logger.EnumeratedAndCachedPartitions(serviceUri, partitionKeys);
                return partitionKeys;
            }
            catch (Exception ex)
            {
                logger.FailedToEnumeratePartitions(serviceUri, ex);
                throw new PartitionEnumerationException(serviceUri, ex);
            }
        }
    }
}