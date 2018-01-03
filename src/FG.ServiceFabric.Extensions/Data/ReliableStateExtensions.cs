using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace FG.ServiceFabric.Data
{
    public static class ReliableStateExtensions
    {
        public static async Task<TValue> GetSimpleValueAsync<TKey, TValue>(
            this IReliableDictionary<TKey, TValue> dictionary,
            ITransaction tx, TKey key,
            TValue defaultValue = default(TValue)) where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            var valueState = await dictionary.TryGetValueAsync(tx, key);
            var value = valueState.HasValue ? valueState.Value : defaultValue;
            return value;
        }

        public static async Task<ReliableStateInfo> GetCount(this IReliableState state, ITransaction tx)
        {
            var reliableStateInfo = new ReliableStateInfo();

            var type = state.GetType();
            reliableStateInfo.Type = type.GetFriendlyName();

            var methodInfo = state.GetType().GetMethod("GetCountAsync", new[] {typeof(ITransaction)}) ??
                             state.GetType().GetMethods()
                                 .FirstOrDefault(m => m.Name == "GetCountAsync" && m.GetParameters().Length == 1);
            if (methodInfo != null)
            {
                var countTask = (Task<long>) methodInfo.Invoke(state, new object[] {tx});
                var count = await countTask;
                reliableStateInfo.Count = count;
            }
            return reliableStateInfo;
        }

        public static async Task<IEnumerable<ReliableStateInfo>> GetAllStates(this IReliableStateManager stateManager,
            CancellationToken ct = default(CancellationToken))
        {
            var states = new List<ReliableStateInfo>();
            using (var tx = stateManager.CreateTransaction())
            {
                var enumerable = stateManager.GetAsyncEnumerator();
                while (await enumerable.MoveNextAsync(ct))
                {
                    var reliableStateInfo = new ReliableStateInfo
                    {
                        Name = enumerable.Current.Name.ToString(),
                        Count = -1
                    };

                    var conditionalValue = await stateManager.TryGetAsync<IReliableState>(enumerable.Current.Name);
                    if (conditionalValue.HasValue)
                    {
                        var rsi = await conditionalValue.Value.GetCount(tx);
                        reliableStateInfo.Count = rsi.Count;
                        reliableStateInfo.Type = rsi.Type;
                    }
                    states.Add(reliableStateInfo);
                }
            }
            return states;
        }

        private static async Task<IEnumerable<object>> EnumerateState<T, T2>(IReliableStateManager stateManager,
            IReliableDictionary<T, T2> collection, CancellationToken cancellationToken)
            where T : IComparable<T>, IEquatable<T>
        {
            var list = new List<object>();
            using (var tx = stateManager.CreateTransaction())
            {
                var enumerable = await collection.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(cancellationToken))
                    list.Add(enumerator.Current);
            }
            return list;
        }

        private static async Task<IEnumerable<object>> EnumerateState<T>(IReliableStateManager stateManager,
            IReliableQueue<T> collection, CancellationToken cancellationToken)
        {
            var list = new List<object>();
            using (var tx = stateManager.CreateTransaction())
            {
                var enumerable = await collection.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(cancellationToken))
                    list.Add(enumerator.Current);
            }
            return list;
        }
    }
}