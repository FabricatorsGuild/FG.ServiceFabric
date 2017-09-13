using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class FileSystemStateSessionManager : TextStateSessionManager
	{
		private const string CommonPathDefault = @"c:\temp\servicefabric";
		private readonly string _commonPath;

		private readonly IDictionary<string, string> _invalidCharsReplacement;
		private readonly IDictionary<string, string> _replacementsToInvalidChars;

		public FileSystemStateSessionManager(
			string serviceName,
			Guid partitionId,
			string partitionKey,
			string storagePath) :
			base(serviceName, partitionId, partitionKey)
		{
			_commonPath = storagePath ?? CommonPathDefault;

			_invalidCharsReplacement = System.IO.Path.GetInvalidFileNameChars()
				.Select((c, i) => new { InvalidChar = c.ToString(), Replacement = $"%{i}%" })
				.ToDictionary(c => c.InvalidChar, c => c.Replacement);
			_replacementsToInvalidChars = System.IO.Path.GetInvalidFileNameChars()
				.Select((c, i) => new { InvalidChar = c.ToString(), Replacement = $"%{i}%" })
				.ToDictionary(c => c.Replacement, c => c.InvalidChar);
		}

		private string EscapeFileName(string fileName)
		{
			var stringBuilder = new StringBuilder(fileName);
			foreach (var replacer in _invalidCharsReplacement)
			{
				stringBuilder.Replace(replacer.Key, replacer.Value);
			}
			return stringBuilder.ToString();
		}

		private string UnescapeFileName(string fileName)
		{
			var stringBuilder = new StringBuilder(fileName);
			foreach (var replacer in _replacementsToInvalidChars)
			{
				stringBuilder.Replace(replacer.Key, replacer.Value);
			}
			return stringBuilder.ToString();
		}
		
		protected override string GetEscapedKey(string key)
		{
			return EscapeFileName(key);
		}

		protected override string GetUnescapedKey(string key)
		{
			return UnescapeFileName(key);
		}

		protected override TextStateSession CreateSessionInternal(StateSessionManagerBase<TextStateSession> manager)
		{
			return new FileSystemStateSession(this);
		}


		private class FileSystemStateSession : TextStateSession, IStateSession
		{
			private readonly object _lock = new object();
			private string CommonPath => _manager._commonPath;

			private readonly FileSystemStateSessionManager _manager;

			public FileSystemStateSession(
				FileSystemStateSessionManager manager) : base(manager)
			{
				_manager = manager;

				lock (_lock)
				{
					if (!System.IO.Directory.Exists(CommonPath))
					{
						System.IO.Directory.CreateDirectory(CommonPath);
					}
				}
			}			

			protected override string GetEscapedKey(string id)
			{
				return $"{CommonPath}{System.IO.Path.DirectorySeparatorChar}{_manager.EscapeFileName(id)}";
			}

			protected override bool Contains(string id)
			{
				var fileName = $"{id}.json";
				return System.IO.File.Exists(fileName);
			}

			protected override string Read(string id)
			{
				var fileName = $"{id}.json";
				return System.IO.File.ReadAllText(fileName);
			}

			protected override void Delete(string id)
			{
				var fileName = $"{id}.json";
				System.IO.File.Delete(fileName);
			}

			protected override void Write(string id, string content)
			{
				var fileName = $"{id}.json";
				System.IO.File.WriteAllText(fileName, content);
			}
			
			protected override FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000, ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				var results = new List<string>();
				var nextMarker = continuationToken?.Marker ?? "";
				var files = System.IO.Directory.GetFiles(CommonPath, $"{idPrefix}*").OrderBy(f => f);
				var resultCount = 0;
				foreach (var file in files)
				{
					var fileName = _manager.UnescapeFileName(System.IO.Path.GetFileNameWithoutExtension(file));
					if (fileName.CompareTo(nextMarker) > 0)
					{
						results.Add(fileName);
						if (resultCount > maxNumResults)
						{
							return new FindByKeyPrefixResult() {ContinuationToken = new ContinuationToken(fileName), Items = results};
						}
					}
					resultCount++;
				}
				return new FindByKeyPrefixResult() {ContinuationToken = null, Items = results};
			}			

		}
	}
}