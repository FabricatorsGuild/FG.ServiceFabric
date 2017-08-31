using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.CosmosDb;

namespace FG.ServiceFabric.DocumentDb.Testing
{
    public class InMemoryStateSession : IDocumentDbStateWriter, IDocumentDbStateReader
    {
        private readonly IDictionary<string, object> _db = new ConcurrentDictionary<string, object>();
        private Task DummyTask => Task.FromResult(true);

        public void Dispose()
        { }
        
        public Task DelecteAsync(string id, Guid partitionKey)
        {
            _db.Remove(id);
            return DummyTask;
        }

        public Task<T> ReadAsync<T>(string id, Guid partitionKey) where T : IPersistedIdentity
        {
            return Task.FromResult((T)_db[id]);
        }

        public Task<IQueryable<T>> QueryAsync<T>() where T : IPersistedIdentity
        {
            var values = _db.Values.OfType<StateWrapper<T>>().Select(v => v.State).AsQueryable();
            return Task.FromResult(values);
        }

        public Task UpsertAsync<T>(T state, IStateMetadata metadata) where T : IPersistedIdentity
        {
            _db[state.Id] = new StateWrapper<T>(state.Id, state, metadata);
            return DummyTask;
        }

        public Task OpenAsync()
        {
            return DummyTask;
        }
    }
}