using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionReadOnlyDictionary<T> : IStateSessionReadOnlyObject
    {
        Task<bool> Contains(string key, CancellationToken cancellationToken = default(CancellationToken));

        Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string keyPrefix, int maxNumResults = 100000,
            ContinuationToken continuationToken = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<ConditionalValue<T>> TryGetValueAsync(string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<T> GetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
        Task<IAsyncEnumerable<KeyValuePair<string, T>>> CreateEnumerableAsync();
        Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}