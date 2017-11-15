using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace FG.ServiceFabric.Services.Runtime
{
	public abstract class WrappedReliableDictionary<TKey, TValue> : IReliableDictionary<TKey, TValue>
		where TKey : IComparable<TKey>, IEquatable<TKey>
	{
		private readonly IReliableDictionary<TKey, TValue> _innerDictionary;

		protected WrappedReliableDictionary(IReliableDictionary<TKey, TValue> innerDictionary)
		{
			_innerDictionary = innerDictionary;
			_innerDictionary.DictionaryChanged += (sender, args) => { DictionaryChanged?.Invoke(this, args); };
		}

		public Uri Name => _innerDictionary.Name;

		public virtual Task<long> GetCountAsync(ITransaction tx)
		{
			return _innerDictionary.GetCountAsync(tx);
		}

		public virtual Task ClearAsync()
		{
			return _innerDictionary.ClearAsync();
		}

		public virtual Task AddAsync(ITransaction tx, TKey key, TValue value)
		{
			return _innerDictionary.AddAsync(tx, key, value);
		}

		public virtual Task AddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.AddAsync(tx, key, value, timeout, cancellationToken);
		}

		public virtual Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory,
			Func<TKey, TValue, TValue> updateValueFactory)
		{
			return _innerDictionary.AddOrUpdateAsync(tx, key, addValueFactory, updateValueFactory);
		}

		public virtual Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue,
			Func<TKey, TValue, TValue> updateValueFactory)
		{
			return _innerDictionary.AddOrUpdateAsync(tx, key, addValue, updateValueFactory);
		}

		public virtual Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory,
			Func<TKey, TValue, TValue> updateValueFactory,
			TimeSpan timeout, CancellationToken cancellationToken)
		{
			return _innerDictionary.AddOrUpdateAsync(tx, key, addValueFactory, updateValueFactory, timeout, cancellationToken);
		}

		public virtual Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue,
			Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.AddOrUpdateAsync(tx, key, addValue, updateValueFactory, timeout, cancellationToken);
		}

		public virtual Task ClearAsync(TimeSpan timeout, CancellationToken cancellationToken)
		{
			return _innerDictionary.ClearAsync(timeout, cancellationToken);
		}

		public virtual Task<bool> ContainsKeyAsync(ITransaction tx, TKey key)
		{
			return _innerDictionary.ContainsKeyAsync(tx, key);
		}

		public virtual Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode)
		{
			return _innerDictionary.ContainsKeyAsync(tx, key, lockMode);
		}

		public virtual Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.ContainsKeyAsync(tx, key, timeout, cancellationToken);
		}

		public virtual Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.ContainsKeyAsync(tx, key, lockMode, timeout, cancellationToken);
		}

		public virtual Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn)
		{
			return _innerDictionary.CreateEnumerableAsync(txn);
		}

		public virtual Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn,
			EnumerationMode enumerationMode)
		{
			return _innerDictionary.CreateEnumerableAsync(txn, enumerationMode);
		}

		public virtual Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn,
			Func<TKey, bool> filter, EnumerationMode enumerationMode)
		{
			return _innerDictionary.CreateEnumerableAsync(txn, filter, enumerationMode);
		}

		public virtual Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory)
		{
			return _innerDictionary.GetOrAddAsync(tx, key, valueFactory);
		}

		public virtual Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value)
		{
			return _innerDictionary.GetOrAddAsync(tx, key, value);
		}

		public virtual Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory,
			TimeSpan timeout, CancellationToken cancellationToken)
		{
			return _innerDictionary.GetOrAddAsync(tx, key, valueFactory, timeout, cancellationToken);
		}

		public virtual Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.GetOrAddAsync(tx, key, value, timeout, cancellationToken);
		}

		public virtual Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value)
		{
			return _innerDictionary.TryAddAsync(tx, key, value);
		}

		public virtual Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.TryAddAsync(tx, key, value, timeout, cancellationToken);
		}

		public virtual Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key)
		{
			return _innerDictionary.TryGetValueAsync(tx, key);
		}

		public virtual Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode)
		{
			return _innerDictionary.TryGetValueAsync(tx, key, lockMode);
		}

		public virtual Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.TryGetValueAsync(tx, key, timeout, cancellationToken);
		}

		public virtual Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode,
			TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.TryGetValueAsync(tx, key, lockMode, timeout, cancellationToken);
		}

		public virtual Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key)
		{
			return _innerDictionary.TryRemoveAsync(tx, key);
		}

		public virtual Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.TryRemoveAsync(tx, key, timeout, cancellationToken);
		}

		public virtual Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue)
		{
			return _innerDictionary.TryUpdateAsync(tx, key, newValue, comparisonValue);
		}

		public virtual Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue,
			TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.TryUpdateAsync(tx, key, newValue, comparisonValue, timeout, cancellationToken);
		}

		public virtual Task SetAsync(ITransaction tx, TKey key, TValue value)
		{
			return _innerDictionary.SetAsync(tx, key, value);
		}

		public virtual Task SetAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout,
			CancellationToken cancellationToken)
		{
			return _innerDictionary.SetAsync(tx, key, value, timeout, cancellationToken);
		}

		public Func<IReliableDictionary<TKey, TValue>, NotifyDictionaryRebuildEventArgs<TKey, TValue>, Task>
			RebuildNotificationAsyncCallback
		{
			set { _innerDictionary.RebuildNotificationAsyncCallback = value; }
		}

		public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;
	}
}