using System;
using System.Fabric;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace FG.ServiceFabric.Services.Runtime
{
	public abstract class WrappedStateManagerReplica<TReliableDictionary, TReliableQueue> : IReliableStateManager, IReliableStateManagerReplica
		where TReliableDictionary : IReliableState
		where TReliableQueue : IReliableState
	{
		private readonly IReliableStateManagerReplica _innerStateManagerReplica;

		protected WrappedStateManagerReplica (IReliableStateManagerReplica innerStateManagerReplica, 
			Func<IReliableState, TReliableDictionary> createReliableDictionary,
			Func<IReliableState, TReliableQueue> createReliableQueue )
		{
			_innerStateManagerReplica = innerStateManagerReplica;
			_innerStateManagerReplica.StateManagerChanged += (sender, args) => StateManagerChanged?.Invoke(this, args);
			_innerStateManagerReplica.TransactionChanged += (sender, args) => TransactionChanged?.Invoke(this, args);
		}

		protected abstract Task<T> CreateWrappedReliableDictionary<T>(T innerReliableDictionary) where T : IReliableState;

		public virtual IAsyncEnumerator<IReliableState> GetAsyncEnumerator()
		{
			return _innerStateManagerReplica.GetAsyncEnumerator();
		}

		public virtual bool TryAddStateSerializer<T>(IStateSerializer<T> stateSerializer)
		{
			return _innerStateManagerReplica.TryAddStateSerializer(stateSerializer);
		}

		public virtual ITransaction CreateTransaction()
		{
			return _innerStateManagerReplica.CreateTransaction();
		}

		public async Task<T> GetOrAddWrappedAsync<T>(T inner) where T : IReliableState
		{
			if (typeof(T).GetGenericTypeDefinition() == typeof(IReliableDictionary<,>))
			{
				return await CreateWrappedReliableDictionary<T>(inner);
			}

			return inner;
		}

		public virtual async Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name, TimeSpan timeout) where T : IReliableState
		{
			var innerDictionary = await _innerStateManagerReplica.GetOrAddAsync<T>(tx, name, timeout);
			if (typeof(T).GetGenericTypeDefinition() == typeof(IReliableDictionary<,>))
			{
				return await CreateWrappedReliableDictionary<T>(innerDictionary);
			}

			return innerDictionary;
		}

		public virtual Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(tx, name);
		}

		public virtual Task<T> GetOrAddAsync<T>(Uri name, TimeSpan timeout) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(name, timeout);
		}

		public virtual Task<T> GetOrAddAsync<T>(Uri name) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(name);
		}

		public virtual Task<T> GetOrAddAsync<T>(ITransaction tx, string name, TimeSpan timeout) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(tx, name, timeout);
		}

		public virtual Task<T> GetOrAddAsync<T>(ITransaction tx, string name) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(tx, name);
		}

		public virtual Task<T> GetOrAddAsync<T>(string name, TimeSpan timeout) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(name, timeout);
		}

		public virtual Task<T> GetOrAddAsync<T>(string name) where T : IReliableState
		{
			return _innerStateManagerReplica.GetOrAddAsync<T>(name);
		}

		public virtual Task RemoveAsync(ITransaction tx, Uri name, TimeSpan timeout)
		{
			return _innerStateManagerReplica.RemoveAsync(tx, name, timeout);
		}

		public virtual Task RemoveAsync(ITransaction tx, Uri name)
		{
			return _innerStateManagerReplica.RemoveAsync(tx, name);
		}

		public virtual Task RemoveAsync(Uri name, TimeSpan timeout)
		{
			return _innerStateManagerReplica.RemoveAsync(name, timeout);
		}

		public virtual Task RemoveAsync(Uri name)
		{
			return _innerStateManagerReplica.RemoveAsync(name);
		}

		public virtual Task RemoveAsync(ITransaction tx, string name, TimeSpan timeout)
		{
			return _innerStateManagerReplica.RemoveAsync(tx, name, timeout);
		}

		public virtual Task RemoveAsync(ITransaction tx, string name)
		{
			return _innerStateManagerReplica.RemoveAsync(tx, name);
		}

		public virtual Task RemoveAsync(string name, TimeSpan timeout)
		{
			return _innerStateManagerReplica.RemoveAsync(name, timeout);
		}

		public virtual Task RemoveAsync(string name)
		{
			return _innerStateManagerReplica.RemoveAsync(name);
		}

		public virtual Task<ConditionalValue<T>> TryGetAsync<T>(Uri name) where T : IReliableState
		{
			return _innerStateManagerReplica.TryGetAsync<T>(name);
		}

		public virtual Task<ConditionalValue<T>> TryGetAsync<T>(string name) where T : IReliableState
		{
			return _innerStateManagerReplica.TryGetAsync<T>(name);
		}

		public event EventHandler<NotifyTransactionChangedEventArgs> TransactionChanged;
		public event EventHandler<NotifyStateManagerChangedEventArgs> StateManagerChanged;

		public virtual void Initialize(StatefulServiceInitializationParameters initializationParameters)
		{
			_innerStateManagerReplica.Initialize(initializationParameters);
		}

		public virtual Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
		{
			return _innerStateManagerReplica.OpenAsync(openMode, partition, cancellationToken);
		}

		public virtual Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
		{
			return _innerStateManagerReplica.ChangeRoleAsync(newRole, cancellationToken);
		}

		public virtual Task CloseAsync(CancellationToken cancellationToken)
		{
			return _innerStateManagerReplica.CloseAsync(cancellationToken);
		}

		public virtual void Abort()
		{
			_innerStateManagerReplica.Abort();
		}

		public virtual Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			return _innerStateManagerReplica.BackupAsync(backupCallback);
		}

		public virtual Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
		{
			return _innerStateManagerReplica.BackupAsync(option, timeout, cancellationToken, backupCallback); 
		}

		public virtual Task RestoreAsync(string backupFolderPath)
		{
			return _innerStateManagerReplica.RestoreAsync(backupFolderPath);
		}

		public virtual Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
		{
			return _innerStateManagerReplica.RestoreAsync(backupFolderPath, restorePolicy, cancellationToken);
		}

		public virtual Func<CancellationToken, Task<bool>> OnDataLossAsync
		{
			set { _innerStateManagerReplica.OnDataLossAsync = value; }
		}
	}
}