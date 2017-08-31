using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public interface IStateSession : IDisposable
	{
		Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task OpenQueue<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task SetValueAsync<T>(string schema, string key, T value, CancellationToken cancellationToken = default(CancellationToken));
		Task SetValueAsync(string schema, string key, Type valueType, object value, CancellationToken cancellationToken = default(CancellationToken));
		Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task EnqueueAsync<T>(string schema, T value, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> DequeueAsync<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> PeekAsync<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task CommitAsync();
		Task AbortAsync();
	}
}