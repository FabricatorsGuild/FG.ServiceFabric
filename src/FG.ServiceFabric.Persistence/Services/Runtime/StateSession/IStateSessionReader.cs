using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionReader : IDisposable
    {
        Task<bool> Contains<T>(string schema, string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> Contains(string schema, string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<FindByKeyPrefixResult<T>> FindByKeyPrefixAsync<T>(string schema, string keyPrefix,
            int maxNumResults = 100000,
            ContinuationToken continuationToken = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix, int maxNumResults = 100000,
            ContinuationToken continuationToken = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<T> GetValueAsync<T>(string schema, string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<ConditionalValue<T>> PeekAsync<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<long> GetDictionaryCountAsync<T>(string schema, CancellationToken cancellationToken);
        Task<long> GetEnqueuedCountAsync<T>(string schema, CancellationToken cancellationToken);
    }
}