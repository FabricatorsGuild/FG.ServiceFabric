using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class InMemoryDocumentStorageSession : IDocumentStorageSession
	{
		private static readonly IDictionary<string, ExternalState> Database = new ConcurrentDictionary<string, ExternalState>();

		public Task UpsertAsync(string key, ExternalState value)
		{
			if (Database.ContainsKey(key))
			{
				if (value == null)
				{
					Database.Remove(key);
				}
				else
				{
					Database[key] = value;
				}
			}
			else
			{
				Database.Add(key, value);
			}

			return Task.FromResult(true);
		}

		public Task<ExternalState> ReadAsync(string key)
		{
			return Task.FromResult(Database.ContainsKey(key) ? Database[key] : null);
		}

		public Task<IEnumerable<string>> FindByKeyAsync(string keyPrefix, int numOfItemsToReturn = -1, object marker = null)
		{
			if (numOfItemsToReturn > -1)
			{
				if (marker != null)
				{
					var markerValue = marker?.ToString() ?? "";
					return Task.FromResult(Database
						.OrderBy(item => item.Key)
						.Where(item => item.Key.StartsWith(keyPrefix) && string.Compare(keyPrefix, markerValue, StringComparison.Ordinal) > 0)
						.Take(numOfItemsToReturn)
						.Select(item => item.Key));
				}
				return Task.FromResult(Database
					.OrderBy(item => item.Key)
					.Take(numOfItemsToReturn)
					.Select(item => item.Key));
			}
			return Task.FromResult(Database
				.Where(item => item.Key.StartsWith(keyPrefix))
				.Select(item => item.Key));
		}

		public static void PrintDatabase()
		{
			foreach (var item in Database)
			{
				System.Diagnostics.Debug.WriteLine($"{item.Key}");
				System.Diagnostics.Debug.WriteLine($"\t{item.Value}");
			}
		}

		public Task<bool> ContainsAsync(string key)
		{
			return Task.FromResult(Database.ContainsKey(key));
		}

		public Task DeleteAsync(string key)
		{
			if (Database.ContainsKey(key))
			{
				Database.Remove(key);
			}

			return Task.FromResult(true);
		}
	}
}