using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Services.Remoting.Runtime.Client
{
	public class PartitionHelper
	{
		private readonly IDictionary<Uri, Int64RangePartitionInformation[]> _int64Partitions;
		private readonly object _lock = new object();
		private readonly IDictionary<Uri, NamedPartitionInformation[]> _namedPartitions;

		public PartitionHelper()
		{
			_int64Partitions = new ConcurrentDictionary<Uri, Int64RangePartitionInformation[]>();
			_namedPartitions = new ConcurrentDictionary<Uri, NamedPartitionInformation[]>();
		}

		// ReSharper disable once UnusedMember.Global - Used for logging!
		public static string ToString(IEnumerable<ServicePartitionInformation> partitions)
		{
			var partitionsString = new StringBuilder();
			var delimiter = "";
			foreach (var partition in partitions)
			{
				partitionsString.Append(delimiter);
				var int64Partition = partition as Int64RangePartitionInformation;
				if (int64Partition != null)
				{
					partitionsString.Append($"{int64Partition.LowKey}-{int64Partition.HighKey}");
				}
				var namedPartition = partition as NamedPartitionInformation;
				if (namedPartition != null)
				{
					partitionsString.Append($"{namedPartition.Name}");
				}
				delimiter = ",";
			}
			return partitionsString.ToString();
		}

		public async Task<IEnumerable<NamedPartitionInformation>> GetNamedPartitions(Uri serviceUri,
			IPartitionHelperLogger logger)
		{
			logger.EnumeratingPartitions(serviceUri);

			lock (_lock)
			{
				if (_namedPartitions.ContainsKey(serviceUri))
				{
					var partitions = _namedPartitions[serviceUri];
					logger.EnumeratedExistingPartitions(serviceUri, partitions);
					return partitions;
				}
			}

			try
			{
				var fabricClient = new FabricClient();
				var servicePartitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);
				IList<NamedPartitionInformation> partitionKeys = new List<NamedPartitionInformation>(servicePartitionList.Count);
				foreach (var partition in servicePartitionList)
				{
					var partitionInfo = partition.PartitionInformation as NamedPartitionInformation;
					if (partitionInfo == null)
					{
						throw new InvalidOperationException(
							$"The service {serviceUri} should have a Named partition. Instead: {partition.PartitionInformation.Kind}");
					}
					partitionKeys.Add(partitionInfo);
				}
				lock (_lock)
				{
					if (!_namedPartitions.ContainsKey(serviceUri))
					{
						_namedPartitions.Add(serviceUri, partitionKeys.ToArray());
					}
				}

				logger.EnumeratedAndCachedPartitions(serviceUri, partitionKeys);
				return partitionKeys;
			}
			catch (Exception ex)
			{
				logger.FailedToEnumeratePartitions(serviceUri, ex);
				throw new PartitionEnumerationException(serviceUri, ex);
			}
		}

		public async Task<IEnumerable<Int64RangePartitionInformation>> GetInt64Partitions(Uri serviceUri,
			IPartitionHelperLogger logger)
		{
			logger.EnumeratingPartitions(serviceUri);

			lock (_lock)
			{
				if (_int64Partitions.ContainsKey(serviceUri))
				{
					var partitions = _int64Partitions[serviceUri];
					logger.EnumeratedExistingPartitions(serviceUri, partitions);
					return partitions;
				}
			}

			try
			{
				var fabricClient = new FabricClient();
				var servicePartitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);
				IList<Int64RangePartitionInformation> partitionKeys =
					new List<Int64RangePartitionInformation>(servicePartitionList.Count);
				foreach (var partition in servicePartitionList)
				{
					var partitionInfo = partition.PartitionInformation as Int64RangePartitionInformation;
					if (partitionInfo == null)
					{
						throw new InvalidOperationException(
							$"The service {serviceUri} should have a uniform Int64 partition. Instead: {partition.PartitionInformation.Kind}");
					}
					partitionKeys.Add(partitionInfo);
				}
				lock (_lock)
				{
					if (!_int64Partitions.ContainsKey(serviceUri))
					{
						_int64Partitions.Add(serviceUri, partitionKeys.ToArray());
					}
				}

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