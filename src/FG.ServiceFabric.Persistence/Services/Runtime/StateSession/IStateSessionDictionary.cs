using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionDictionary<T> : IStateSessionReadOnlyDictionary<T>, IStateSessionObject
    {
        Task SetValueAsync(string key, T value, CancellationToken cancellationToken = default(CancellationToken));

        Task SetValueAsync(string key, T value, IValueMetadata metadata,
            CancellationToken cancellationToken = default(CancellationToken));

        Task RemoveAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
    }
}