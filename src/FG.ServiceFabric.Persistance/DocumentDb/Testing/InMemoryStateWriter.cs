using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FG.ServiceFabric.DocumentDb.Testing
{
    public class InMemoryStateSession : IDocumentDbStateWriter, IDocumentDbStateReader
    {
        private readonly IDictionary<string, DocumentStateWrapper<object>> _db = new ConcurrentDictionary<string, DocumentStateWrapper<object>>();

        public void Dispose()
        { }

        public Task UpsertAsync<T>(T state, string stateName) where T : IPersistedIdentity
        {
            _db.Add(state.Id, new DocumentStateWrapper<object> { Id = state.Id, Payload = state, StateName = stateName});
            return Task.FromResult(true);
        }

        public Task<IQueryable<T>> QueryAsync<T>() where T : IPersistedIdentity
        {
            var values = _db.Values.Select(v => v.Payload).OfType<T>().AsQueryable();
            return Task.FromResult(values);
        }
    }
}