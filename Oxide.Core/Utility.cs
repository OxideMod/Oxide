using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Core
{
    /// <summary>
    /// A partially thread-safe HashSet (interating is not thread-safe)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentHashSet<T> : ICollection<T>
    {
        readonly HashSet<T> collection;
        readonly object syncRoot = new object();

        public ConcurrentHashSet()
        {
            collection = new HashSet<T>();
        }

        public ConcurrentHashSet(ICollection<T> values)
        {
            collection = new HashSet<T>(values);
        }

        public bool IsReadOnly => false;
        public int Count { get { lock (syncRoot) return collection.Count; } }
        public bool Contains(T value) { lock (syncRoot) return collection.Contains(value); }
        public bool Add(T value) { lock (syncRoot) return collection.Add(value); }
        public bool Remove(T value) { lock (syncRoot) return collection.Remove(value); }
        public void Clear() { lock (syncRoot) collection.Clear(); }
        public void CopyTo(T[] array, int index) { lock (syncRoot) collection.CopyTo(array, index); }
        public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();
        public bool Any(Func<T, bool> callback) { lock (syncRoot) return collection.Any(callback); }
        public T[] ToArray() { lock (syncRoot) return collection.ToArray(); }

        public bool TryDequeue(out T value)
        {
            lock (syncRoot)
            {
                value = collection.ElementAtOrDefault(0);
                if (value != null) collection.Remove(value);
                return value != null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<T>.Add(T value) => Add(value);
    }
}
