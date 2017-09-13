using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public interface IStateSessionManager
	{
		Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task OpenQueue<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));

		IStateSession CreateSession();		
	}
	public interface IStateSession : IDisposable
	{
		Task<bool> Contains<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<bool> Contains(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<FindByKeyPrefixResult> FindByKeyPrefixAsync<T>(string schema, string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken));
		Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken));
		Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task RemoveAsync(string schema, string key, CancellationToken cancellationToken = default(CancellationToken));
		Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> DequeueAsync<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> PeekAsync<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task CommitAsync();
		Task AbortAsync();
	}

	public class FindByKeyPrefixResult
	{
		public IEnumerable<string> Items { get; set; }
		public ContinuationToken ContinuationToken { get; set; }
	}
}