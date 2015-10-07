using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Oxide.Core
{
    /// <summary>
    /// A partially thread-safe HashSet (iterating is not thread-safe)
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

    public class Utility
    {
        public static void DatafileToProto<T>(string name, bool deleteAfter = true)
        {
            var dfs = Interface.Oxide.DataFileSystem;
            if (!dfs.ExistsDatafile(name)) return;
            try
            {
                var data = dfs.ReadObject<T>(name);
                ProtoStorage.Save(data, name);
                if (deleteAfter) File.Delete(dfs.GetFile(name).Filename);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("Failed to convert datafile to proto storage: " + name, ex);
            }
        }

        public static void PrintCallStack()
        {
            Interface.Oxide.LogDebug("CallStack:{0}{1}", Environment.NewLine, new StackTrace(1, true));
        }

        public static string FormatBytes(double bytes)
        {
            string type;
            if (bytes > 1024 * 1024)
            {
                type = "mb";
                bytes /= (1024 * 1024);
            }
            else if (bytes > 1024)
            {
                type = "kb";
                bytes /= 1024;
            }
            else
                type = "b";
            return $"{bytes:0}{type}";
        }
    }
}
