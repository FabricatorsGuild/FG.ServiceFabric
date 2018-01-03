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
            this._storage = state ?? new ConcurrentDictionary<string, string>();
        }

        protected override TextStateSession CreateSessionInternal
            (StateSessionManagerBase<TextStateSession> manager,
            IStateSessionReadOnlyObject[] stateSessionObjects)
        {
            return new InMemoryStateSession(this, stateSessionObjects);
        }

        protected override TextStateSession CreateSessionInternal
        (StateSessionManagerBase<TextStateSession> manager,
            IStateSessionObject[] stateSessionObjects)
        {
            return new InMemoryStateSession(this, stateSessionObjects);
        }

        private sealed class InMemoryStateSession : TextStateSession, IStateSession
        {
            private readonly InMemoryStateSessionManagerWithTransaction _manager;

            public InMemoryStateSession(
                InMemoryStateSessionManagerWithTransaction manager,
                IStateSessionReadOnlyObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                this._manager = manager;
            }

            public InMemoryStateSession(
                InMemoryStateSessionManagerWithTransaction manager,
                IStateSessionObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                this._manager = manager;
            }

            private IDictionary<string, string> Storage => this._manager._storage;

            protected override async Task<string> ReadAsync(string id, bool checkExistsOnly = false)
            {
                if (this.Storage.TryGetValue(id, out var value))
                {
                    // Quick return not-null value if check for existance only
                    await Task.Delay(1);
                    return checkExistsOnly ? string.Empty : value;
                }

                await Task.Delay(1);
                return null;
            }

            protected override async Task DeleteAsync(string id)
            {
                this.Storage.Remove(id);
                await Task.Delay(1);
            }

            protected override async Task WriteAsync(string id, string content)
            {
                this.Storage[id] = content;
                await Task.Delay(1);
            }

            protected override FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000,
                ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
            {
                var results = new List<string>();
                var nextMarker = continuationToken?.Marker ?? string.Empty;
                var keys = this.Storage.Keys.OrderBy(f => f);
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
                                return new FindByKeyPrefixResult() { ContinuationToken = new ContinuationToken(nextKey), Items = results };
                            }
                        }
                    }
                }

                return new FindByKeyPrefixResult() { ContinuationToken = null, Items = results };
            }

            protected override Task<long> GetCountInternalAsync(string schema, CancellationToken cancellationToken)
            {
                var items = this.Storage.Keys.Where(item => item.StartsWith(schema));
                var resultCount = items.LongCount();
                return Task.FromResult(resultCount);
            }
        }
    }
}