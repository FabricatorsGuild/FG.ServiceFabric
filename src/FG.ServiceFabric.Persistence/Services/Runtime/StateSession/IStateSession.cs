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
	public interface IStateSessionObject
	{
		
	}

	public interface IStateSessionQueue<T> : IStateSessionObject
	{
		Task EnqueueAsync(T value, CancellationToken cancellationToken = default(CancellationToken));
		Task EnqueueAsync(T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> DequeueAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> PeekAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<IAsyncEnumerable<T>> CreateEnumerableAsync();
		Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken));
	}

	public interface IStateSessionDictionary<T> : IStateSessionObject
	{
		Task<bool> Contains(string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<ConditionalValue<T>> TryGetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<T> GetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
		Task SetValueAsync(string key, T value, CancellationToken cancellationToken = default(CancellationToken));
		Task SetValueAsync(string key, T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken));
		Task RemoveAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
		Task<IAsyncEnumerable<KeyValuePair<string, T>>> CreateEnumerableAsync();
		Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken));
	}

	public interface IStateSessionManager
	{
		Task<IStateSessionDictionary<T>> OpenDictionary<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));
		Task<IStateSessionQueue<T>> OpenQueue<T>(string schema, CancellationToken cancellationToken = default(CancellationToken));

		IStateSession CreateSession(params IStateSessionObject[] stateSessionObjects);		
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
		Task<long> GetDictionaryCountAsync<T>(string schema, CancellationToken cancellationToken);
		Task<long> GetEnqueuedCountAsync<T>(string schema, CancellationToken cancellationToken);
		Task CommitAsync();
		Task AbortAsync();
	}

	public class FindByKeyPrefixResult
	{
		public IEnumerable<string> Items { get; set; }
		public ContinuationToken ContinuationToken { get; set; }
	}
}