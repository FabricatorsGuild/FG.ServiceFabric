using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.State
{
    public interface IStatefulServiceStateManagerSession : IDisposable
    {
        Task<IStatefulServiceStateManagerSession> ForDictionary<T>(string schema);
        Task<IStatefulServiceStateManagerSession> ForQueue<T>(string schema);

        event EventHandler<SessionCommittedEventArgs> SessionCommitted;
        Task SetAsync<T>(string schema, string storageKey, T value);
        Task<T> GetOrAddAsync<T>(string schema, string storageKey, Func<string, T> newValue);
        Task<ConditionalValue<T>> TryGetAsync<T>(string schema, string storageKey);
        Task RemoveAsync<T>(string schema, string storageKey);

        Task EnqueueAsync<T>(string schema, T value);
        Task<ConditionalValue<T>> DequeueAsync<T>(string schema);

        Task<ConditionalValue<T>> PeekAsync<T>(string schema);

        Task<IEnumerable<KeyValuePair<string, T>>> EnumerateDictionary<T>(string schema);
        Task<IEnumerable<T>> EnumerateQueue<T>(string schema);

        Task CommitAsync();
        Task AbortAsync();
    }
}