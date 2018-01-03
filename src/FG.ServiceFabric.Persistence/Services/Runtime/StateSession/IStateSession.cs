using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSession : IStateSessionReader
    {
        Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata,
            CancellationToken cancellationToken = default(CancellationToken));

        Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata,
            CancellationToken cancellationToken = default(CancellationToken));

        Task RemoveAsync<T>(string schema, string key,
            CancellationToken cancellationToken = default(CancellationToken));

        Task RemoveAsync(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));

        Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<ConditionalValue<T>> DequeueAsync<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken));

        Task CommitAsync();
        Task AbortAsync();
    }
}