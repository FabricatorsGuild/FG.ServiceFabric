using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Services.Runtime.StateSession.FileSystem
{
	public class FileSystemStateSessionManager : TextStateSessionManagerWithTransaction
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
				.Select((c, i) => new {InvalidChar = c.ToString(), Replacement = $"%{i}%"})
				.ToDictionary(c => c.InvalidChar, c => c.Replacement);
			_replacementsToInvalidChars = System.IO.Path.GetInvalidFileNameChars()
				.Select((c, i) => new {InvalidChar = c.ToString(), Replacement = $"%{i}%"})
				.ToDictionary(c => c.Replacement, c => c.InvalidChar);
		}

		private string EscapeFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName)) return fileName;

			var stringBuilder = new StringBuilder(fileName);
			foreach (var replacer in _invalidCharsReplacement)
			{
				stringBuilder.Replace(replacer.Key, replacer.Value);
			}
			return stringBuilder.ToString();
		}

		private string UnescapeFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName)) return fileName;

			var stringBuilder = new StringBuilder(fileName);
			foreach (var replacer in _replacementsToInvalidChars)
			{
				stringBuilder.Replace(replacer.Key, replacer.Value);
			}
			return stringBuilder.ToString();
		}

		protected override string GetEscapedKeyInternal(string key)
		{
			return EscapeFileName(key);
		}

		protected override string GetUnescapedKeyInternal(string key)
		{
			return UnescapeFileName(key);
		}

		protected override TextStateSession CreateSessionInternal(
			StateSessionManagerBase<TextStateSession> manager,
			IStateSessionObject[] stateSessionObjects)
		{
			return new FileSystemStateSession(this, stateSessionObjects);
		}


		protected override TextStateSession CreateSessionInternal(
			StateSessionManagerBase<TextStateSession> manager,
			IStateSessionReadOnlyObject[] stateSessionObjects)
		{
			return new FileSystemStateSession(this, stateSessionObjects);
		}

		private class FileSystemStateSession : TextStateSession, IStateSession
		{
			private readonly object _lock = new object();

			private readonly FileSystemStateSessionManager _manager;

			public FileSystemStateSession(
				FileSystemStateSessionManager manager,
				IStateSessionReadOnlyObject[] stateSessionObjects)
				: base(manager, stateSessionObjects)
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
			public FileSystemStateSession(
				FileSystemStateSessionManager manager,
				IStateSessionObject[] stateSessionObjects)
				: base(manager, stateSessionObjects)
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

			private string CommonPath => _manager._commonPath;

			private string GetFilePath(string id)
			{
				var fileName = $"{_manager.EscapeFileName(id)}.json";
				var filePath = System.IO.Path.Combine(CommonPath, fileName);
				return filePath;
			}

			protected override Task<string> ReadAsync(string id, bool checkExistsOnly = false)
			{
				var filePath = GetFilePath(id);
				if (System.IO.File.Exists(filePath))
				{
					if (checkExistsOnly)
					{
						return Task.FromResult("");
					}
					
					// Quick return not-null value if check for existance only
					return Task.FromResult(System.IO.File.ReadAllText(filePath));
				}
				return null;
			}

			protected override Task DeleteAsync(string id)
			{
				var filePath = GetFilePath(id);
				System.IO.File.Delete(filePath);

				return Task.FromResult(true);
			}

			protected override Task WriteAsync(string id, string content)
			{
				var filePath = GetFilePath(id);
				System.IO.File.WriteAllText(filePath, content);

				return Task.FromResult(true);
			}

			protected override FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000,
				ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				var results = new List<string>();
				var nextMarker = continuationToken?.Marker ?? "";
				var files = System.IO.Directory.GetFiles(CommonPath, $"{idPrefix}*{key}*").OrderBy(f => f);
				var resultCount = 0;
				foreach (var file in files)
				{
					var fileName = _manager.UnescapeFileName(System.IO.Path.GetFileNameWithoutExtension(file));
					if (fileName.CompareTo(nextMarker) > 0)
					{
						results.Add(fileName);
						resultCount++;
						if (resultCount >= maxNumResults)
						{
							return new FindByKeyPrefixResult() {ContinuationToken = new ContinuationToken(fileName), Items = results};
						}
					}
				}
				return new FindByKeyPrefixResult() {ContinuationToken = null, Items = results};
			}

			protected override Task<long> GetCountInternalAsync(string schema, CancellationToken cancellationToken)
			{
				var files = System.IO.Directory.GetFiles(CommonPath, $"{schema}*").OrderBy(f => f);
				return Task.FromResult(files.LongCount());
			}
		}
	}
}