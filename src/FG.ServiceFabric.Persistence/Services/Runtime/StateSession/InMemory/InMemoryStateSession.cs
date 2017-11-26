using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Services.Runtime.StateSession.InMemory
{
	public class InMemoryStateSessionManager : TextStateSessionManager
	{
		private readonly IDictionary<string, string> _storage;


		public InMemoryStateSessionManager(
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
			private readonly InMemoryStateSessionManager _manager;

			public InMemoryStateSession(
				InMemoryStateSessionManager manager,
				IStateSessionObject[] stateSessionObjects)
				: base(manager, stateSessionObjects)
			{
				_manager = manager;
			}

			private IDictionary<string, string> Storage => _manager._storage;


			protected override string GetEscapedKey(string id)
			{
				return base.GetEscapedKey(id);
			}

			public override long Count(string idPrefix)
			{
				var items = Storage.Keys.Where(item => item.StartsWith(idPrefix));
				var resultCount = items.LongCount();
				return resultCount;
			}

			protected override bool Contains(string id)
			{
				return Storage.ContainsKey(id);
			}

			protected override string Read(string id)
			{
				return Storage[id];
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
				var items = Storage.Keys.OrderBy(f => f);
				var resultCount = 0;
				foreach (var item in items)
				{
					if (item.CompareTo(nextMarker) > 0)
					{
						if (item.StartsWith(idPrefix) && ((key == null) || item.Contains(key)))
						{
							results.Add(item);
							resultCount++;
							if (resultCount >= maxNumResults)
							{
								return new FindByKeyPrefixResult() {ContinuationToken = new ContinuationToken(item), Items = results};
							}
						}
					}
				}
				return new FindByKeyPrefixResult() {ContinuationToken = null, Items = results};
			}
		}
	}
}