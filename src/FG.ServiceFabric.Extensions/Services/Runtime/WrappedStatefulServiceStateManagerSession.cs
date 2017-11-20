using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.State;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime
{
	public class WrappedStatefulServiceStateManagerSession : IStatefulServiceStateManagerSession
	{
		private readonly IStatefulServiceStateManagerSession _innerSession;

		public WrappedStatefulServiceStateManagerSession(IStatefulServiceStateManagerSession innerSession)
		{
			_innerSession = innerSession;
		}

		public async Task<IStatefulServiceStateManagerSession> ForDictionary<T>(string schema)
		{
			await _innerSession.ForDictionary<T>(schema);
			return this;
		}

		public async Task<IStatefulServiceStateManagerSession> ForQueue<T>(string schema)
		{
			await _innerSession.ForQueue<T>(schema);
			return this;
		}

		public void Dispose()
		{
			_innerSession.Dispose();
		}

		public event EventHandler<SessionCommittedEventArgs> SessionCommitted;

		public virtual Task SetAsync<T>(string schema, string storageKey, T value)
		{
			return _innerSession.SetAsync(schema, storageKey, value);
		}

		public virtual Task<T> GetOrAddAsync<T>(string schema, string storageKey, Func<string, T> newValue)
		{
			return _innerSession.GetOrAddAsync<T>(schema, storageKey, newValue);
		}

		public virtual Task<ConditionalValue<T>> TryGetAsync<T>(string schema, string storageKey)
		{
			return _innerSession.TryGetAsync<T>(schema, storageKey);
		}

		public virtual Task RemoveAsync<T>(string schema, string storageKey)
		{
			return _innerSession.RemoveAsync<T>(schema, storageKey);
		}

		public virtual Task EnqueueAsync<T>(string schema, T value)
		{
			return _innerSession.EnqueueAsync<T>(schema, value);
		}

		public virtual Task<ConditionalValue<T>> DequeueAsync<T>(string schema)
		{
			return _innerSession.DequeueAsync<T>(schema);
		}

		public virtual Task<ConditionalValue<T>> PeekAsync<T>(string schema)
		{
			return _innerSession.PeekAsync<T>(schema);
		}

		public virtual Task<IEnumerable<KeyValuePair<string, T>>> EnumerateDictionary<T>(string schema)
		{
			return _innerSession.EnumerateDictionary<T>(schema);
		}

		public virtual Task<IEnumerable<T>> EnumerateQueue<T>(string schema)
		{
			return _innerSession.EnumerateQueue<T>(schema);
		}

		public virtual Task CommitAsync()
		{
			return _innerSession.CommitAsync();
		}

		public virtual Task AbortAsync()
		{
			return _innerSession.AbortAsync();
		}
	}
}