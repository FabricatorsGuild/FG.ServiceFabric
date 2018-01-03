﻿using System.Collections.Generic;
using System.Collections.Immutable;

namespace FG.Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }


        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        public static IImmutableDictionary<TKey, TValue> RemoveRange<TKey, TValue>(
            this IImmutableDictionary<TKey, TValue> dictionary, params TKey[] key)
        {
            return dictionary.RemoveRange(key);
        }

        public static ImmutableDictionary<TKey, TValue> RemoveRange<TKey, TValue>(
            this ImmutableDictionary<TKey, TValue> dictionary, params TKey[] key)
        {
            return dictionary.RemoveRange(key);
        }
    }
}