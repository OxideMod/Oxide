using System;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    /// <summary>
    /// A dictionary which returns null for non-existant keys and removes keys when setting an index to null.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class Hash<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

        public Hash()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value))
                    return value;
                if (typeof(TValue).IsValueType)
                    return (TValue)Activator.CreateInstance(typeof(TValue));
                return default(TValue);
            }

            set
            {
                if (value == null)
                    dictionary.Remove(key);
                else
                    dictionary[key] = value;
            }
        }

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => dictionary.IsReadOnly;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public bool Contains(KeyValuePair<TKey, TValue> item) => dictionary.Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => dictionary.CopyTo(array, index);
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public void Add(TKey key, TValue value) => dictionary.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => dictionary.Add(item);
        public bool Remove(TKey key) => dictionary.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => dictionary.Remove(item);
        public void Clear() => dictionary.Clear();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
