using System;
using System.Collections.Generic;

namespace Protobuild
{
    public static class SafeToDictionaryExtension
    {
        public static Dictionary<TKey, TValue> ToDictionarySafe<TSource, TKey, TValue>(this IEnumerable<TSource> enumerable, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, Action<TSource> onDuplicate)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            foreach (var s in enumerable)
            {
                var key = keySelector(s);

                if (dictionary.ContainsKey(key))
                {
                    onDuplicate(s);
                }
                else
                {
                    dictionary.Add(key, valueSelector(s));
                }
            }

            return dictionary;
        }
    }
}

