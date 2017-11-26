using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Services.Runtime.StateSession.InMemory
{
	public class InMemoryStateSessionManagerWithTransaction : TextStateSessionManagerWithTransaction
	{
		private readonly IDictionary<string, string> _storage;


		public InMemoryStateSessionManagerWithTransaction(
			string serviceName,
			Guid partitionId,
			string partitionKey,
			IDictionary<string, string> state = null) :
			base(serviceName, partitionId, partitionKey)
		{
			_storage = state ?? new ConcurrentDictionary<string, string>();
		}

		protected override TextStateSession CreateSessionInternal(StateSessionManagerBase<TextStateSession> manager,
			IStateSessionObject[] stateSessionObjects)
		{
			return new InMemoryStateSession(this, stateSessionObjects);
		}

		private sealed class InMemoryStateSession : TextStateSession, IStateSession
		{
			private readonly InMemoryStateSessionManagerWithTransaction _manager;

			public InMemoryStateSession(
				InMemoryStateSessionManagerWithTransaction manager,
				IStateSessionObject[] stateSessionObjects)
				: base(manager, stateSessionObjects)
			{
				_manager = manager;
			}

			private IDictionary<string, string> Storage => _manager._storage;

			protected override string Read(string id, bool checkExistsOnly = false)
			{
				if (Storage.ContainsKey(id))
				{
					// Quick return not-null value if check for existance only
					return checkExistsOnly ? "" : Storage[id];
				}
				return null;
			}

			protected override void Delete(string id)
			{
				Storage.Remove(id);
			}

			protected override void Write(string id, string content)
			{
				Storage[id] = content;
			}

			protected override FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000,
				ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				var results = new List<string>();
				var nextMarker = continuationToken?.Marker ?? "";
				var keys = Storage.Keys.OrderBy(f => f);
				var resultCount = 0;
				foreach (var nextKey in keys)
				{
					if (nextKey.CompareTo(nextMarker) > 0)
					{
						if (nextKey.StartsWith(idPrefix))
						{
							results.Add(nextKey);
							resultCount++;
							if (resultCount >= maxNumResults)
							{
								return new FindByKeyPrefixResult() {ContinuationToken = new ContinuationToken(nextKey), Items = results};
							}
						}
					}
				}
				return new FindByKeyPrefixResult() {ContinuationToken = null, Items = results};
			}

			protected override Task<long> GetCountInternalAsync(string schema, CancellationToken cancellationToken)
			{
				var items = Storage.Keys.Where(item => item.StartsWith(schema));
				var resultCount = items.LongCount();
				return Task.FromResult(resultCount);
			}
		}
	}
}