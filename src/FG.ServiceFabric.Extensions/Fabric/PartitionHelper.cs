using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Fabric
{
	public class PartitionHelper
	{
		private readonly Func<IPartitionEnumerationManager> _partitionEnumerationManagerFactory;
		private readonly IDictionary<Uri, Int64RangePartitionInformation[]> _int64Partitions;
		private readonly object _lock = new object();
		private readonly IDictionary<Uri, NamedPartitionInformation[]> _namedPartitions;

		public PartitionHelper(Func<IPartitionEnumerationManager> partitionEnumerationManagerFactory)
		{
			this._partitionEnumerationManagerFactory = partitionEnumerationManagerFactory;
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
				var partitionEnumerationManager = _partitionEnumerationManagerFactory();
				var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
				IList<NamedPartitionInformation> partitionKeys = new List<NamedPartitionInformation>(servicePartitionList.Count);
				foreach (var partition in servicePartitionList)
				{
					if (!(partition.PartitionInformation is NamedPartitionInformation partitionInfo))
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
				var partitionEnumerationManager = _partitionEnumerationManagerFactory();
				var servicePartitionList = await partitionEnumerationManager.GetPartitionListAsync(serviceUri);
				IList<Int64RangePartitionInformation> partitionKeys =
					new List<Int64RangePartitionInformation>(servicePartitionList.Count);
				foreach (var partition in servicePartitionList)
				{
					if (!(partition.PartitionInformation is Int64RangePartitionInformation partitionInfo))
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