using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.Common.Utils;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class ReliableStateSessionQueue<T> : IStateSessionQueue<T>
	{
		private readonly IReliableConcurrentQueue<T> _reliableConcurrentQueue;
		public ReliableStateSessionQueue(IReliableConcurrentQueue<T> reliableConcurrentQueue) { _reliableConcurrentQueue = reliableConcurrentQueue; }
		public Task EnqueueAsync(T value, CancellationToken cancellationToken = default(CancellationToken)) { throw new NotImplementedException(); }
		public Task EnqueueAsync(T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();			
		}
		public Task<ConditionalValue<T>> DequeueAsync(CancellationToken cancellationToken = default(CancellationToken)) { throw new NotImplementedException(); }
		public Task<ConditionalValue<T>> PeekAsync(CancellationToken cancellationToken = default(CancellationToken)) { throw new NotImplementedException(); }
		public Task<IAsyncEnumerable<T>> CreateEnumerableAsync() { throw new NotImplementedException(); }
		public Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken)) { throw new NotImplementedException(); }
	}

	public class ReliableStateSessionDictionary<T> : IStateSessionDictionary<T>
	{
		private readonly IReliableDictionary2<string, T> _reliableDictionary;

		public ReliableStateSessionDictionary(IReliableDictionary2<string, T> reliableDictionary)
		{
			_reliableDictionary = reliableDictionary;
		}

		public Task<bool> Contains(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task<ConditionalValue<T>> TryGetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task<T> GetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task SetValueAsync(string key, T value, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task SetValueAsync(string key, T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}

		public Task RemoveAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			throw new NotImplementedException();
		}
		public Task<IAsyncEnumerable<KeyValuePair<string, T>>> CreateEnumerableAsync() { throw new NotImplementedException(); }
		public Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken)) { throw new NotImplementedException(); }
	}

	public class ReliableStateSessionManager : IStateSessionManager
	{
		private readonly IReliableStateManager _stateManager;

		private readonly IDictionary<string, IReliableState> _reliableDictionaries = new ConcurrentDictionary<string, IReliableState>();
		private readonly IDictionary<string, IReliableState> _reliableQueues = new ConcurrentDictionary<string, IReliableState>();

		private readonly IDictionary<string, Type> _reliableStateTypes = new ConcurrentDictionary<string, Type>();

		internal IReliableStateManager StateManager => _stateManager;

		public ReliableStateSessionManager(IReliableStateManager stateManager)
		{
			_stateManager = stateManager;
		}


		private static Type GetReliableStateInterfaceType(Type implementationType)
		{
			var genericInterfaceTypes = implementationType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());

			foreach (var genericInterfaceType in genericInterfaceTypes)
			{
				if (genericInterfaceType == typeof(IReliableDictionary2<,>)) return typeof(IReliableDictionary2<,>);
				if (genericInterfaceType == typeof(IReliableDictionary<,>)) return typeof(IReliableDictionary<,>);
				if (genericInterfaceType == typeof(IReliableConcurrentQueue<>)) return typeof(IReliableConcurrentQueue<>);
				if (genericInterfaceType == typeof(IReliableQueue<>)) return typeof(IReliableQueue<>);
			}

			throw new StateSessionException($"Cannot infer the Reliable State type for {implementationType.FullName}");
		}

		private static string GetSchemaFromUrn(Uri urn)
		{
			return urn.AbsolutePath;
		}
		public async Task<IStateSessionDictionary<T>> OpenDictionary<T>(string schema, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			IReliableDictionary2<string, T> reliableDictionary2 = null;
			await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
			{
				reliableDictionary2 = await _stateManager.GetOrAddAsync<IReliableDictionary2<string, T>>(schema);
				_reliableDictionaries[schema] = reliableDictionary2;
			}, 3, TimeSpan.FromSeconds(1), cancellationToken);

			return new ReliableStateSessionDictionary<T>(reliableDictionary2);
		}

		public async Task<IStateSessionQueue<T>> OpenQueue<T>(string schema, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			IReliableConcurrentQueue<T> reliableConcurrentQueue = null;
			await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
			{
				reliableConcurrentQueue = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<T>>(schema);
				_reliableQueues[schema] = reliableConcurrentQueue;
			}, 3, TimeSpan.FromSeconds(1), cancellationToken);

			return  new ReliableStateSessionQueue<T>(reliableConcurrentQueue);
		}

		public IStateSession CreateSession(params IStateSessionObject[] stateSessionObjects)
		{
			return new ReliableStateSession(this);
		}		

		private IReliableDictionary2<string, T> GetDictionary<T>(string schema)
		{
			var reliableDictionary2 = _reliableDictionaries[schema] as IReliableDictionary2<string, T>;
			if (reliableDictionary2 == null)
			{
				throw new KeyNotFoundException($"State dictionary schema {schema} not found");
			}
			return reliableDictionary2;
		}
		private IReliableQueue<T> GetQueue<T>(string schema)
		{
			var reliableQueue = _reliableQueues[schema] as IReliableQueue<T>;
			if (reliableQueue == null)
			{
				throw new KeyNotFoundException($"State queue schema {schema} not found");
			}
			return reliableQueue;
		}
		private void UpdateReliableStateType(string schema, Type type)
		{
			if (!_reliableStateTypes.ContainsKey(schema))
			{
				_reliableStateTypes.Add(schema, type);
			}
		}

		private async Task<Type> GetReliableStateType(string schema, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (_reliableStateTypes.ContainsKey(schema))
			{
				return _reliableStateTypes[schema];
			}

			var stateEnumerator = _stateManager.GetAsyncEnumerator();
			var type = default(Type);
			while (await stateEnumerator.MoveNextAsync(cancellationToken))
			{
				var currentState = stateEnumerator.Current;
				var currentStateSchema = GetSchemaFromUrn(currentState.Name);
				var currentStateType = GetReliableStateInterfaceType(currentState.GetType());
				UpdateReliableStateType(currentStateSchema, currentStateType);

				if (schema.Equals(currentStateSchema, StringComparison.InvariantCultureIgnoreCase))
				{
					type = currentStateType;
				}
			}

			if (type != null)
			{
				return type;
			}

			throw new StateSessionException($"Cannot find a Reliable State type for the key {schema}");
		}

		private sealed class ReliableStateSession : IStateSession
		{
			private readonly ReliableStateSessionManager _sessionManager;
			private ITransaction _transaction;
			private bool _needsCommit;
			private bool _isAborted;
			private bool _isCommitted;
			private bool _isOpen;

			public ReliableStateSession(ReliableStateSessionManager sessionManager)
			{
				_sessionManager = sessionManager;
			}

			private ITransaction GetTransaction()
			{
				if (_transaction == null)
				{
					_transaction = _sessionManager.StateManager.CreateTransaction();
					_isOpen = true;
					_needsCommit = false;
					_isCommitted = false;
					_isAborted = false;
				}
				return _transaction;
			}

			private IReliableDictionary2<string, T> GetDictionary<T>(string schema)
			{
				return _sessionManager.GetDictionary<T>(schema);
			}

			private IReliableQueue<T> GetQueue<T>(string schema)
			{
				return _sessionManager.GetQueue<T>(schema);
			}

			public async Task<bool> Contains<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var value = false;
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					_sessionManager.UpdateReliableStateType(schema, typeof(T));
					var reliableDictionary2 = GetDictionary<T>(schema);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						value = await reliableDictionary2.ContainsKeyAsync(GetTransaction(), key, TimeSpan.FromSeconds(1), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					return value;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"Contains from {schema}:{key} failed", ex);
				}
			}

			public async Task<bool> Contains(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

					var valueType = await _sessionManager.GetReliableStateType(schema, cancellationToken);

					var methodNameGetDictionary = nameof(GetDictionary);
					var reliableDictionary2 = this.CallGenericMethod(methodNameGetDictionary, new Type[] { valueType }, schema);

					var methodNameContainsKeyAsync = nameof(IReliableDictionary2<string, object>.ContainsKeyAsync);

					var contains = await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						var task = (Task<bool>)reliableDictionary2
							.CallGenericMethod(methodNameContainsKeyAsync, new[] { valueType }, GetTransaction(), key, TimeSpan.FromSeconds(3), cancellationToken);
						return await task;
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);

					return contains;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"Contains from {schema}:{key} failed", ex);
				}
			}

			public async Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var value = default(ConditionalValue<T>);
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					_sessionManager.UpdateReliableStateType(schema, typeof(T));
					var reliableDictionary2 = GetDictionary<T>(schema);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						value = await reliableDictionary2.TryGetValueAsync(GetTransaction(), key, TimeSpan.FromSeconds(1), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					return value;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync from {schema}:{key} failed", ex);
				}
			}

			private async Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schema, string keyPrefix, int maxNumResults,
				Func<IStateSessionManager, string, Task< object>> reliableDictionaryFactory,
				Func<object, ITransaction, CancellationToken, Task<IAsyncEnumerable<string>>> createKeyEnumerableAsyncFactory,
				ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				var result = new List<string>();
				var nextContinuationToken = default(string);
				var resultCount = 0;
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

					var reliableDictionary2 = await reliableDictionaryFactory(_sessionManager, schema);
					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						var keyEnumerable = await createKeyEnumerableAsyncFactory(reliableDictionary2, GetTransaction(), cancellationToken);

						var enumerateNext = (continuationToken == null);
						var enumerator = keyEnumerable.GetAsyncEnumerator();
						while (await enumerator.MoveNextAsync(cancellationToken))
						{
							var current = enumerator.Current;
							if (continuationToken != null)
							{
								if (current.Equals(continuationToken.Marker))
								{
									enumerateNext = true;
								}
							}

							if (enumerateNext)
							{
								if (string.IsNullOrEmpty(keyPrefix) || current.StartsWith(keyPrefix))
								{
									result.Add(current);
									resultCount++;
									if (resultCount > maxNumResults)
									{
										nextContinuationToken = current;
										break;
									}
								}
							}
						}

					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					return new FindByKeyPrefixResult()
					{
						Items = result.ToArray(),
						ContinuationToken = nextContinuationToken != null ? new ContinuationToken(nextContinuationToken) : null
					};
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"FindByKeyPrefixAsync for {schema}:{keyPrefix} failed", ex);
				}
			}

			public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				return FindByKeyPrefixInternalAsync(schema, keyPrefix, maxNumResults, 
					reliableDictionaryFactory: async (stateSessionManager, schema2) =>
						{
							var reliableDictionary2 = new ReliableDictionaryNonGenericWrapper(this._sessionManager, schema);
							return await reliableDictionary2.Initialize(cancellationToken);
						},
					createKeyEnumerableAsyncFactory:async (reliableDictionary2, transaction, cn) => 
						await ((ReliableDictionaryNonGenericWrapper) reliableDictionary2).CreateKeyEnumerableAsync<string>(transaction, EnumerationMode.Ordered, TimeSpan.FromSeconds(1), cn),
					continuationToken: continuationToken, 
					cancellationToken: cancellationToken);
			}

			public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync<T>(string schema, string keyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				return FindByKeyPrefixInternalAsync(schema, keyPrefix, maxNumResults,
					reliableDictionaryFactory: async (stateSessionManager, schema2) =>
					{
						_sessionManager.UpdateReliableStateType(schema, typeof(T));
						var reliableDictionary2 = GetDictionary<T>(schema);
						return reliableDictionary2;
					},
					createKeyEnumerableAsyncFactory: (reliableDictionary2, transaction, cn) =>
						((IReliableDictionary2<string, T>)reliableDictionary2).CreateKeyEnumerableAsync(transaction, EnumerationMode.Ordered, TimeSpan.FromSeconds(1), cn),
					continuationToken: continuationToken,
					cancellationToken: cancellationToken);
			}

			public async Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var result = new List<string>();
				var stateEnumerator = _sessionManager._stateManager.GetAsyncEnumerator();
				var type = default(Type);
				while (await stateEnumerator.MoveNextAsync(cancellationToken))
				{
					var currentState = stateEnumerator.Current;
					var currentStateSchema = GetSchemaFromUrn(currentState.Name);
					var currentStateType = GetReliableStateInterfaceType(currentState.GetType());
					_sessionManager.UpdateReliableStateType(currentStateSchema, currentStateType);

					result.Add(currentStateSchema);
				}
				return result;
			}

			public async Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var value = default(ConditionalValue<T>);
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					_sessionManager.UpdateReliableStateType(schema, typeof(T));
					var reliableDictionary2 = GetDictionary<T>(schema);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						value = await reliableDictionary2.TryGetValueAsync(GetTransaction(), key, TimeSpan.FromSeconds(1), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"GetValueAsync for {schema}:{key} failed", ex);
				}

				if (value.HasValue)
				{
					return value.Value;
				}

				throw new KeyNotFoundException($"State with {schema}:{key} does not exist");
			}

			public async Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					_sessionManager.UpdateReliableStateType(schema, typeof(T));
					var reliableDictionary2 = GetDictionary<T>(schema);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						await reliableDictionary2.SetAsync(GetTransaction(), key, value, TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"SetValueAsync for {schema}:{key} failed", ex);
				}
			}

			public async Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					//_sessionManager.UpdateReliableStateType(schema, valueType);

					var reliableDictionary2 = new ReliableDictionaryNonGenericWrapper(this._sessionManager, schema);
					await reliableDictionary2.Initialize(cancellationToken);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						await reliableDictionary2.SetAsync(GetTransaction(), key, value, TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"SetValueAsync for {schema}:{key} failed", ex);
				}
			}

			public async Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					_sessionManager.UpdateReliableStateType(schema, typeof(T));
					var reliableDictionary2 = GetDictionary<T>(schema);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						await reliableDictionary2.TryRemoveAsync(GetTransaction(), key, TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"RemoveAsync from {schema}:{key} failed", ex);
				}
			}

			public async Task RemoveAsync(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

					var reliableDictionary2 = new ReliableDictionaryNonGenericWrapper(this._sessionManager, schema);
					await reliableDictionary2.Initialize(cancellationToken);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						await reliableDictionary2.TryRemoveAsync(GetTransaction(), key, TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"RemoveAsync from {schema}:{key} failed", ex);
				}
			}

			public async Task<long> GetDictionaryCountAsync<T>(string schema, CancellationToken cancellationToken)
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					var dictionary = GetDictionary<T>(schema);

					long value = 0;
					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					                                              {
						                                              value = await dictionary.GetCountAsync(GetTransaction());
					                                              }, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;

					return value;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"PeekAsync failed", ex);
				}
			}

			public async Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					var queue = GetQueue<T>(schema);

					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						await queue.EnqueueAsync(GetTransaction(), value, TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"EnqueueAsync to {schema} failed", ex);
				}
			}

			public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					var queue = GetQueue<T>(schema);

					var value = default(ConditionalValue<T>);
					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						value = await queue.TryDequeueAsync(GetTransaction(), TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;

					return value;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"DequeueAsync from {schema} failed", ex);
				}
			}

			public async Task<ConditionalValue<T>> PeekAsync<T>(string schema, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					var queue = GetQueue<T>(schema);

					var value = default(ConditionalValue<T>);
					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					{
						value = await queue.TryPeekAsync(GetTransaction(), TimeSpan.FromSeconds(3), cancellationToken);
					}, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;

					return value;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"PeekAsync failed", ex);
				}
			}

			public async Task<long> GetEnqueuedCountAsync<T>(string schema, CancellationToken cancellationToken)
			{
				try
				{
					cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
					var queue = GetQueue<T>(schema);

					long value = 0;
					await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
					                                              {
						                                              value = await queue.GetCountAsync(GetTransaction());
					                                              }, 3, TimeSpan.FromSeconds(1), cancellationToken);
					_needsCommit = true;

					return value;
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"PeekAsync failed", ex);
				}
			}

			public async Task CommitAsync()
			{
				if (_isOpen && _needsCommit && !_isCommitted)
				{
					await _transaction.CommitAsync();
					_isCommitted = true;
				}
				_isOpen = false;
			}

			public Task AbortAsync()
			{
				if (_isOpen && !_isCommitted && !_isAborted)
				{
					_transaction?.Abort();
					_isAborted = true;
				}

				_isOpen = false;
				return Task.FromResult(true);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (disposing)
				{
					if (_isOpen && _needsCommit && !_isCommitted)
					{
						_transaction.CommitAsync().GetAwaiter().GetResult();
						_isCommitted = true;
						_isOpen = false;
					}

					_transaction?.Dispose();
				}
			}

			private class ReliableDictionaryNonGenericWrapper
			{
				private readonly ReliableStateSessionManager _sessionManager;
				private readonly string _schema;
				private Type _valueType;
				private Type _reliableCollectionType;
				private Type _reliableDictionaryType;
				private Type _reliableDictionary2Type;
				private IReliableState _reliableDictionary2;

				private string _methodNameCreateKeyEnumerableAsync;
				private string _methodNameSetAsync;
				private string _methodNameRemoveAsync;

				public long Count { get; }

				public ReliableDictionaryNonGenericWrapper(ReliableStateSessionManager sessionManager, string schema)
				{
					_sessionManager = sessionManager;
					_schema = schema;

				}

				public async Task<ReliableDictionaryNonGenericWrapper> Initialize(CancellationToken cancellationToken = default(CancellationToken))
				{
					_valueType = await _sessionManager.GetReliableStateType(_schema, cancellationToken);
					_reliableCollectionType = typeof(IReliableCollection<>).MakeGenericType(_valueType);
					_reliableDictionaryType = typeof(IReliableDictionary<,>).MakeGenericType(typeof(string), _valueType);
					_reliableDictionary2Type = typeof(IReliableDictionary2<,>).MakeGenericType(typeof(string), _valueType);

					var methodNameGetDictionary = nameof(ReliableStateSession.GetDictionary);
					_reliableDictionary2 = (IReliableState)_sessionManager.CallGenericMethod(methodNameGetDictionary, new Type[] { _valueType }, _schema);

					_methodNameCreateKeyEnumerableAsync = nameof(IReliableDictionary2<string, string>.CreateKeyEnumerableAsync);
					_methodNameSetAsync = nameof(IReliableDictionary<string, string>.SetAsync);

					_methodNameRemoveAsync = nameof(IReliableDictionary<string, string>.TryRemoveAsync);

					return this;
				}

				public Task<IAsyncEnumerable<TKey>> CreateKeyEnumerableAsync<TKey>(ITransaction txn)
				{
					var methodInfo = _reliableDictionary2Type
						.GetMethod(_methodNameCreateKeyEnumerableAsync, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
							new Type[] { typeof(ITransaction) }, null);

					var task = (Task<IAsyncEnumerable<TKey>>)methodInfo.Invoke(_reliableDictionary2, new object[] { txn });
					return task;
				}

				public Task<IAsyncEnumerable<TKey>> CreateKeyEnumerableAsync<TKey>(ITransaction txn, EnumerationMode enumerationMode)
				{
					var methodInfo = _reliableDictionary2Type
						.GetMethod(
							_methodNameCreateKeyEnumerableAsync, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
							new Type[] { typeof(ITransaction), typeof(EnumerationMode) }, null);

					var task = (Task<IAsyncEnumerable<TKey>>)methodInfo.Invoke(_reliableDictionary2, new object[] { txn, enumerationMode });
					return task;
				}

				public Task<IAsyncEnumerable<TKey>> CreateKeyEnumerableAsync<TKey>(ITransaction txn, EnumerationMode enumerationMode, TimeSpan timeout,
					CancellationToken cancellationToken)
				{
					var methodInfo = _reliableDictionary2Type
						.GetMethod(_methodNameCreateKeyEnumerableAsync, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
							new Type[] { typeof(ITransaction), typeof(EnumerationMode), typeof(TimeSpan), typeof(CancellationToken) }, null);

					var task = (Task<IAsyncEnumerable<TKey>>)methodInfo.Invoke(_reliableDictionary2, new object[] { txn, enumerationMode, timeout, cancellationToken });
					return task;
				}

				public Task SetAsync<TKey>(ITransaction tx, TKey key, object value)
				{
					var methodInfo = _reliableDictionaryType
						.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(method => method.Name.Equals(_methodNameSetAsync) && method.GetParameters().Length == 3);

					var task = (Task)methodInfo.Invoke(_reliableDictionary2, new object[] { tx, key, value });
					return task;
				}

				public Task SetAsync<TKey>(ITransaction tx, TKey key, object value, TimeSpan timeout, CancellationToken cancellationToken)
				{
					var methodInfo = _reliableDictionaryType
						.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(method => method.Name.Equals(_methodNameSetAsync) && method.GetParameters().Length == 5);

					var task = (Task)methodInfo.Invoke(_reliableDictionary2, new object[] { tx, key, value, timeout, cancellationToken });
					return task;
				}
				
				public Task TryRemoveAsync<TKey>(ITransaction tx, TKey key)
				{
					var methodInfo = _reliableDictionaryType
						.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(method => method.Name.Equals(_methodNameRemoveAsync) && method.GetParameters().Length == 2);

					var task = (Task)methodInfo.Invoke(_reliableDictionary2, new object[] { tx, key });
					return task;
				}

				public Task TryRemoveAsync<TKey>(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
				{
					var methodInfo = _reliableDictionaryType
						.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(method => method.Name.Equals(_methodNameRemoveAsync) && method.GetParameters().Length == 4);

					var task = (Task)methodInfo.Invoke(_reliableDictionary2, new object[] { tx, key, timeout, cancellationToken });
					return task;
				}

			}

		}

	}
}