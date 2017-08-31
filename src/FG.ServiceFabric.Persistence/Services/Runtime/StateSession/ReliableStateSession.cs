using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class ReliableStateSession : IStateSession
	{
		private readonly IReliableStateManager _stateManager;
		private ITransaction _transaction;
		private bool _needsCommit;
		private bool _isAborted;
		private bool _isCommitted;
		private bool _isOpen;

		private readonly IDictionary<string, IReliableState> _reliableDictionaries = new ConcurrentDictionary<string, IReliableState>();
		private readonly IDictionary<string, IReliableState> _reliableQueues = new ConcurrentDictionary<string, IReliableState>();

		public ReliableStateSession(IReliableStateManager stateManager)
		{
			_stateManager = stateManager;
		}

		public async Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
			{
				var reliableDictionary2 = await _stateManager.GetOrAddAsync<IReliableDictionary2<string, T>>(schema);
				_reliableDictionaries[schema] = reliableDictionary2;
			}, 3, TimeSpan.FromSeconds(1), cancellationToken);
		}

		public async Task OpenQueue<T>(string schema, CancellationToken cancellationToken = default(CancellationToken))
		{
			cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

			await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
			{
				var reliableConcurrentQueue = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<T>>(schema);
				_reliableQueues[schema] = reliableConcurrentQueue;
			}, 3, TimeSpan.FromSeconds(1), cancellationToken);
		}

		private ITransaction GetTransaction()
		{
			if (_transaction == null)
			{
				_transaction = _stateManager.CreateTransaction();
				_isOpen = true;
				_needsCommit = false;
				_isCommitted = false;
				_isAborted = false;
			}
			return _transaction;
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

		public async Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			var value = default(ConditionalValue<T>);
			try
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
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

		public async Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			var value = default(ConditionalValue<T>);
			try
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
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

		public async Task SetValueAsync<T>(string schema, string key, T value, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
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

		public async Task SetValueAsync(string schema, string key, Type valueType, object value, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var methodNameGetDictionary = nameof(GetDictionary);
				var reliableDictionary2 = this.CallGenericMethod(methodNameGetDictionary, new Type[]{valueType}, schema);

				var methodNameSetAsync = nameof(IReliableDictionary2<string, object>.SetAsync);
				
				await ExecutionHelper.ExecuteWithRetriesAsync(async (cn) =>
				{
					var task = (Task) reliableDictionary2
						.CallGenericMethod(methodNameSetAsync, new[] {valueType}, GetTransaction(), key, value, TimeSpan.FromSeconds(3), cancellationToken);
					await task;
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

		public async Task EnqueueAsync<T>(string schema, T value, CancellationToken cancellationToken = default(CancellationToken))
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

		protected virtual void Dispose(bool disposing)
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
	}
}