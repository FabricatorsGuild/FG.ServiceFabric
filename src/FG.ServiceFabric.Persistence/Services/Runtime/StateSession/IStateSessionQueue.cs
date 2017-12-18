using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionQueue<T> : IStateSessionReadOnlyQueue<T>, IStateSessionObject
    {
        Task EnqueueAsync(T value, CancellationToken cancellationToken = default(CancellationToken));
        Task EnqueueAsync(T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken));
        Task<ConditionalValue<T>> DequeueAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}