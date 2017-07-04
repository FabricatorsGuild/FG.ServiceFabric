using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class WrappedStateManager : IActorStateManager
    {
        private readonly IActorStateManager _innerStateManager;

        public WrappedStateManager(IActorStateManager innerStateManager)
        {
            _innerStateManager = innerStateManager;
        }

        public virtual Task AddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.AddStateAsync<T>(stateName, value, cancellationToken);
        }

        public virtual Task<T> GetStateAsync<T>(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.GetStateAsync<T>(stateName, cancellationToken);
        }

        public virtual Task SetStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.SetStateAsync<T>(stateName, value, cancellationToken);
        }

        public virtual Task RemoveStateAsync(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.RemoveStateAsync(stateName, cancellationToken);
        }

        public virtual  Task<bool> TryAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.TryAddStateAsync<T>(stateName, value, cancellationToken);
        }

        public virtual Task<ConditionalValue<T>> TryGetStateAsync<T>(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.TryGetStateAsync<T>(stateName, cancellationToken);
        }

        public virtual Task<bool> TryRemoveStateAsync(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.TryRemoveStateAsync(stateName, cancellationToken);
        }

        public virtual Task<bool> ContainsStateAsync(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.ContainsStateAsync(stateName, cancellationToken);
        }

        public virtual Task<T> GetOrAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.GetOrAddStateAsync<T>(stateName, value, cancellationToken);
        }

        public virtual Task<T> AddOrUpdateStateAsync<T>(string stateName, T addValue, Func<string, T, T> updateValueFactory,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.AddOrUpdateStateAsync<T>(stateName, addValue, updateValueFactory,
                cancellationToken);
        }

        public virtual Task<IEnumerable<string>> GetStateNamesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.GetStateNamesAsync(cancellationToken);
        }

        public virtual Task ClearCacheAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.ClearCacheAsync(cancellationToken);
        }

        public virtual Task SaveStateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return _innerStateManager.SaveStateAsync(cancellationToken);
        }
    }
}