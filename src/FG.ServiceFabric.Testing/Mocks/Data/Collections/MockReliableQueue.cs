// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace FG.ServiceFabric.Testing.Mocks.Data.Collections
{
    public class MockReliableQueue<T> : IReliableQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public Task EnqueueAsync(ITransaction tx, T item, TimeSpan timeout, CancellationToken cancellationToken)
        {
            _queue.Enqueue(item);

            return Task.FromResult(true);
        }

        public Task EnqueueAsync(ITransaction tx, T item)
        {
            _queue.Enqueue(item);

            return Task.FromResult(true);
        }

        public Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            T item;
            var result = _queue.TryDequeue(out item);

            return Task.FromResult(
                (ConditionalValue<T>) Activator.CreateInstance(typeof(ConditionalValue<T>), result, item));
        }

        public Task<ConditionalValue<T>> TryDequeueAsync(ITransaction tx)
        {
            T item;
            var result = _queue.TryDequeue(out item);

            return Task.FromResult(
                (ConditionalValue<T>) Activator.CreateInstance(typeof(ConditionalValue<T>), result, item));
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx, LockMode lockMode, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            T item;
            var result = _queue.TryPeek(out item);

            return Task.FromResult(
                (ConditionalValue<T>) Activator.CreateInstance(typeof(ConditionalValue<T>), result, item));
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx, LockMode lockMode)
        {
            T item;
            var result = _queue.TryPeek(out item);

            return Task.FromResult(
                (ConditionalValue<T>) Activator.CreateInstance(typeof(ConditionalValue<T>), result, item));
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            T item;
            var result = _queue.TryPeek(out item);

            return Task.FromResult(
                (ConditionalValue<T>) Activator.CreateInstance(typeof(ConditionalValue<T>), result, item));
        }

        public Task<ConditionalValue<T>> TryPeekAsync(ITransaction tx)
        {
            T item;
            var result = _queue.TryPeek(out item);

            return Task.FromResult(
                (ConditionalValue<T>) Activator.CreateInstance(typeof(ConditionalValue<T>), result, item));
        }

        public Task ClearAsync()
        {
            while (!_queue.IsEmpty)
            {
                T result;
                _queue.TryDequeue(out result);
            }

            return Task.FromResult(true);
        }

        public Task<IAsyncEnumerable<T>> CreateEnumerableAsync(ITransaction tx)
        {
            return Task.FromResult<IAsyncEnumerable<T>>(new MockAsyncEnumerable<T>(_queue));
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            return Task.FromResult<long>(_queue.Count);
        }

        public Uri Name { get; set; }

        public Task<long> GetCountAsync()
        {
            return Task.FromResult((long) _queue.Count);
        }
    }
}