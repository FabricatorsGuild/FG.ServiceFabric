using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class FileSystemDocumentStorageSession : IDocumentStorageSession
	{
		private readonly object _lock = new object();

		// TODO: This should go
		private const string CommonPathDefault = @"c:\temp\servicefabricpeople";
		private readonly string _commonPath;

		public FileSystemDocumentStorageSession(ServiceContext context, string commonPath = null)
		{
			_commonPath = commonPath ?? CommonPathDefault;
		}

		public Task UpsertAsync(string key, ExternalState value)
		{
			lock (_lock)
			{
				var stringValue = Newtonsoft.Json.JsonConvert.SerializeObject(value, new JsonSerializerSettings() { Formatting = Formatting.Indented });

				var fileName = $"{_commonPath}\\{key}.json";
				if (value == null)
				{
					if (System.IO.File.Exists(fileName))
					{
						System.IO.File.Delete(fileName);
					}
				}
				else
				{
					System.IO.File.WriteAllText(fileName, stringValue);
				}
			}

			return Task.FromResult(true);
		}

		public Task<ExternalState> ReadAsync(string key)
		{
			lock (_lock)
			{
				var fileName = $"{_commonPath}\\{key}.json";
				var stringValue = System.IO.File.ReadAllText(fileName);

				var value = Newtonsoft.Json.JsonConvert.DeserializeObject<ExternalState>(stringValue);
				return Task.FromResult(value);
			}
		}

		public Task<IEnumerable<string>> FindByKeyAsync(string keyPrefix, int numOfItemsToReturn = -1, object marker = null)
		{
			lock (_lock)
			{
				var fileNamePattern = $"{keyPrefix}*.json";
				var fileNames = System.IO.Directory.GetFiles(_commonPath, fileNamePattern, SearchOption.TopDirectoryOnly);

				if (numOfItemsToReturn > -1)
				{
					if (marker != null)
					{
						var markerValue = marker?.ToString() ?? "";
						return Task.FromResult(fileNames
							.Select(System.IO.Path.GetFileNameWithoutExtension)
							.OrderBy(item => item)
							.Where(item => item.StartsWith(keyPrefix) && string.Compare(keyPrefix, markerValue, StringComparison.Ordinal) > 0)
							.Take(numOfItemsToReturn));
					}
					return Task.FromResult(fileNames
						.Select(System.IO.Path.GetFileNameWithoutExtension)
						.OrderBy(item => item)
						.Take(numOfItemsToReturn));

				}

				return Task.FromResult(fileNames
					.Select(System.IO.Path.GetFileNameWithoutExtension));
			}
		}

		public Task<bool> ContainsAsync(string key)
		{
			lock (_lock)
			{
				var fileName = $"{_commonPath}\\{key}.json";
				return Task.FromResult(System.IO.File.Exists(fileName));
			}
		}

		public Task DeleteAsync(string key)
		{
			lock (_lock)
			{
				var fileName = $"{_commonPath}\\{key}.json";
				if (System.IO.File.Exists(fileName))
				{
					System.IO.File.Delete(fileName);
				}
			}

			return Task.FromResult(true);
		}
	}
}