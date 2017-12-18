using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionReadOnlyQueue<T> : IStateSessionReadOnlyObject
    {
        Task<ConditionalValue<T>> PeekAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IAsyncEnumerable<T>> CreateEnumerableAsync();
        Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}