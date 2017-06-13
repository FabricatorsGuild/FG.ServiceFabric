// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Runtime
{
    public class MockActorStateManager : IActorStateManager
    {
        private readonly ConcurrentDictionary<string, object> _store = new ConcurrentDictionary<string, object>();

        public Task<T> AddOrUpdateStateAsync<T>(string stateName, T addValue, Func<string, T, T> updateValueFactory, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = this._store.AddOrUpdate(stateName, (k) => addValue, (k, v) => updateValueFactory(k, (T) v));
            return Task.FromResult((T)result);
        }

        public Task AddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default(CancellationToken))
        {
            this._store.TryAdd(stateName, value);
            return Task.FromResult(true);
        }

        public Task<bool> ContainsStateAsync(string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = this._store.ContainsKey(stateName);
            return Task.FromResult(result);
        }

        public Task<T> GetOrAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default(CancellationToken))
        {
            object result;
            if (!this._store.TryGetValue(stateName, out result))
            {
                this._store.TryAdd(stateName, value);
            }            
            return Task.FromResult((T)value);
        }

        public Task<T> GetStateAsync<T>(string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            object result;
            this._store.TryGetValue(stateName, out result);
            return Task.FromResult((T)result);
        }

        public Task<IEnumerable<string>> GetStateNamesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = (IEnumerable<string>) this._store.Keys;
            return Task.FromResult(result);
        }

        public Task RemoveStateAsync(string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            object value;
            this._store.TryRemove(stateName, out value);
            return Task.FromResult(true);
        }

        public Task SetStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default(CancellationToken))
        {
            this._store.AddOrUpdate(stateName, value, (key, oldvalue) => value);
            return Task.FromResult(true);
        }

        public Task<bool> TryAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tryAdd = this._store.TryAdd(stateName, value);
            return Task.FromResult(tryAdd);
        }

        public Task<ConditionalValue<T>> TryGetStateAsync<T>(string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            object item;
            var result = this._store.TryGetValue(stateName, out item);
            if (result)
            {
                return Task.FromResult(new ConditionalValue<T>(result, (T)item));
            }
            return Task.FromResult(new ConditionalValue<T>(result, default(T)));
        }
        
        public Task<bool> TryRemoveStateAsync(string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            object value;
            var result = this._store.TryRemove(stateName, out value);
            return Task.FromResult(result);
        }

        public Task ClearCacheAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            this._store.Clear();
            return Task.FromResult(true);
        }

        public Task SaveStateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(true);
        }
    }
}