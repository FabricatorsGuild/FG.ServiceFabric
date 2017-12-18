using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession.Internal
{
	internal class StateSessionBaseReadOnlyDictionary<TStateSessionManager, TStateSession, TValueType> :
	StateSessionBaseObject<TStateSession>, IStateSessionReadOnlyDictionary<TValueType>,
	IAsyncEnumerable<KeyValuePair<string, TValueType>>
	where TStateSession : class, IStateSession
	{
		public StateSessionBaseReadOnlyDictionary(IStateSessionManagerInternals manager, string schema, bool readOnly)
			: base(manager, schema, readOnly)
		{
		}

		IAsyncEnumerator<KeyValuePair<string, TValueType>> IAsyncEnumerable<KeyValuePair<string, TValueType>>.
			GetAsyncEnumerator()
		{
			return new StateSessionBaseDictionaryEnumerator(_session, _schema);
		}

		public Task<bool> Contains(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.Contains<TValueType>(_schema, key, cancellationToken);
		}

		public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string keyPrefix, int maxNumResults = 100000,
			ContinuationToken continuationToken = null,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.FindByKeyPrefixAsync(_schema, keyPrefix, maxNumResults, continuationToken,
				cancellationToken);
		}

		public Task<ConditionalValue<TValueType>> TryGetValueAsync(string key,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.TryGetValueAsync<TValueType>(_schema, key, cancellationToken);
		}

		public Task<TValueType> GetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.GetValueAsync<TValueType>(_schema, key, cancellationToken);
		}

		public Task<IAsyncEnumerable<KeyValuePair<string, TValueType>>> CreateEnumerableAsync()
		{
			return Task.FromResult((IAsyncEnumerable<KeyValuePair<string, TValueType>>)this);
		}

		public Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.GetDictionaryCountAsync<TValueType>(_schema, cancellationToken);
		}

		private class StateSessionBaseDictionaryEnumerator : IAsyncEnumerator<KeyValuePair<string, TValueType>>
		{
			private readonly string _schema;
			private string _continuationKey;
			private IStateSession _session;

			public StateSessionBaseDictionaryEnumerator(IStateSession session, string schema)
			{
				_session = session;
				_schema = schema;
			}

			public void Dispose()
			{
				Current = default(KeyValuePair<string, TValueType>);
				_session = null;
			}

			public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
			{
				var continuationToken = Current.Key != null ? new ContinuationToken(_continuationKey) : null;

				if (Current.Key != null && _continuationKey == null)
				{
					Current = default(KeyValuePair<string, TValueType>);
					return false;
				}

				var findNext = await _session.FindByKeyPrefixAsync(_schema, null, 1, continuationToken, cancellationToken);
				var nextKey = findNext.Items.FirstOrDefault();
				if (nextKey != null)
				{
					var value = await _session.GetValueAsync<TValueType>(_schema, nextKey, cancellationToken);
					Current = new KeyValuePair<string, TValueType>(nextKey, value);
					_continuationKey = findNext.ContinuationToken?.Marker as string;
					return true;
				}
				else
				{
					Current = default(KeyValuePair<string, TValueType>);
					return false;
				}
			}

			public void Reset()
			{
				Current = default(KeyValuePair<string, TValueType>);
				_continuationKey = null;
			}

			public KeyValuePair<string, TValueType> Current { get; private set; }
		}
	}

	internal class StateSessionBaseDictionary<TStateSessionManager, TStateSession, TValueType> :
		StateSessionBaseReadOnlyDictionary<TStateSessionManager, TStateSession, TValueType>, IStateSessionDictionary<TValueType>,
		IAsyncEnumerable<KeyValuePair<string, TValueType>>
		where TStateSession : class, IStateSession
	{
		public StateSessionBaseDictionary(IStateSessionManagerInternals manager, string schema, bool readOnly)
			: base(manager, schema, readOnly)
		{
		}

		public Task SetValueAsync(string key, TValueType value,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.SetValueAsync(_schema, key, value, null, cancellationToken);
		}

		public Task SetValueAsync(string key, TValueType value, IValueMetadata metadata,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.SetValueAsync(_schema, key, value, metadata, cancellationToken);
		}

		public Task RemoveAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.RemoveAsync<TValueType>(_schema, key, cancellationToken);
		}
	}
}