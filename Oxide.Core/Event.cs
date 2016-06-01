using System;
using System.Collections.Generic;

namespace Oxide.Core
{
    public class Event
    {
        public delegate void Action<in T1, in T2, in T3, in T4, in T5>(T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4);

        internal static Queue<Callback> PooledCallbacks = new Queue<Callback>();
        
        public static void Remove(ref Callback callback)
        {
            if (callback == null) return;
            callback.Remove();
            callback = null;
        }

        public static void Remove<T1>(ref Callback<T1> callback)
        {
            if (callback == null) return;
            callback.Remove();
            callback = null;
        }

        public static void Remove<T1, T2>(ref Callback<T1, T2> callback)
        {
            if (callback == null) return;
            callback.Remove();
            callback = null;
        }

        public static void Remove<T1, T2, T3>(ref Callback<T1, T2, T3> callback)
        {
            if (callback == null) return;
            callback.Remove();
            callback = null;
        }

        public static void Remove<T1, T2, T3, T4>(ref Callback<T1, T2, T3, T4> callback)
        {
            if (callback == null) return;
            callback.Remove();
            callback = null;
        }

        public static void Remove<T1, T2, T3, T4, T5>(ref Callback<T1, T2, T3, T4, T5> callback)
        {
            if (callback == null) return;
            callback.Remove();
            callback = null;
        }
        
        public class Callback
        {
            public Action Invoke;
            internal Callback Previous;
            internal Callback Next;
            internal Event Handler;

            public Callback(Action callback)
            {
                Invoke = callback;
            }

            public void Remove()
            {
                var handler = Handler;
                var next = Next;
                var previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null) handler.Last = previous;
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null) handler.First = next;
                }
                Invoke = null;
                Previous = null;
                Next = null;
                Handler = null;
                if (PooledCallbacks.Count < 1024) PooledCallbacks.Enqueue(this);
            }
        }

        public class Callback<T>
        {
            internal static Queue<Callback<T>> PooledCallbacks = new Queue<Callback<T>>();

            public Action<T> Invoke;
            internal Callback<T> Previous;
            internal Callback<T> Next;
            internal Event<T> Handler;

            public Callback(Action<T> callback)
            {
                Invoke = callback;
            }

            public void Remove()
            {
                var handler = Handler;
                var next = Next;
                var previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null) handler.Last = previous;
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null) handler.First = next;
                }
                Invoke = null;
                Previous = null;
                Next = null;
                Handler = null;
                if (PooledCallbacks.Count < 1024) PooledCallbacks.Enqueue(this);
            }
        }

        public class Callback<T1, T2>
        {
            internal static Queue<Callback<T1, T2>> PooledCallbacks = new Queue<Callback<T1, T2>>();

            //internal static int nextNumber = 1;

            public Action<T1, T2> Invoke;
            internal Callback<T1, T2> Previous;
            internal Callback<T1, T2> Next;
            internal Event<T1, T2> Handler;

            //internal int number;

            public Callback(Action<T1, T2> callback)
            {
                Invoke = callback;
            }

            public void Remove()
            {
                var handler = Handler;
                var next = Next;
                var previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                Invoke = null;
                Previous = null;
                Next = null;
                Handler = null;
                if (PooledCallbacks.Count < 1024) PooledCallbacks.Enqueue(this);
            }
        }

        public class Callback<T1, T2, T3>
        {
            internal static Queue<Callback<T1, T2, T3>> PooledCallbacks = new Queue<Callback<T1, T2, T3>>();

            public Action<T1, T2, T3> Invoke;
            internal Callback<T1, T2, T3> Previous;
            internal Callback<T1, T2, T3> Next;
            internal Event<T1, T2, T3> Handler;

            public Callback(Action<T1, T2, T3> callback)
            {
                Invoke = callback;
            }

            public void Remove()
            {
                var handler = Handler;
                var next = Next;
                var previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null) handler.Last = previous;
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null) handler.First = next;
                }
                Invoke = null;
                Previous = null;
                Next = null;
                Handler = null;
                if (PooledCallbacks.Count < 1024) PooledCallbacks.Enqueue(this);
            }
        }

        public class Callback<T1, T2, T3, T4>
        {
            internal static Queue<Callback<T1, T2, T3, T4>> PooledCallbacks = new Queue<Callback<T1, T2, T3, T4>>();

            public Action<T1, T2, T3, T4> Invoke;
            internal Callback<T1, T2, T3, T4> Previous;
            internal Callback<T1, T2, T3, T4> Next;
            internal Event<T1, T2, T3, T4> Handler;

            public Callback(Action<T1, T2, T3, T4> callback)
            {
                Invoke = callback;
            }

            public void Remove()
            {
                var handler = Handler;
                var next = Next;
                var previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null) handler.Last = previous;
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null) handler.First = next;
                }
                Invoke = null;
                Previous = null;
                Next = null;
                Handler = null;
                if (PooledCallbacks.Count < 1024) PooledCallbacks.Enqueue(this);
            }
        }

        public class Callback<T1, T2, T3, T4, T5>
        {
            internal static Queue<Callback<T1, T2, T3, T4, T5>> PooledCallbacks = new Queue<Callback<T1, T2, T3, T4, T5>>();

            public Action<T1, T2, T3, T4, T5> Invoke;
            internal Callback<T1, T2, T3, T4, T5> Previous;
            internal Callback<T1, T2, T3, T4, T5> Next;
            internal Event<T1, T2, T3, T4, T5> Handler;

            public Callback(Action<T1, T2, T3, T4, T5> callback)
            {
                Invoke = callback;
            }

            public void Remove()
            {
                var handler = Handler;
                var next = Next;
                var previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null) handler.Last = previous;
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null) handler.First = next;
                }
                Invoke = null;
                Previous = null;
                Next = null;
                Handler = null;
                if (PooledCallbacks.Count < 1024) PooledCallbacks.Enqueue(this);
            }
        }


        public Callback First;
        public Callback Last;

        public void Add(Callback callback)
        {
            callback.Handler = this;
            var last = Last;
            if (last == null)
            {
                First = callback;
                Last = callback;
            }
            else
            {
                last.Next = callback;
                callback.Previous = last;
                Last = callback;
            }
        }

        public Callback Add(Action callback)
        {
            Callback event_callback;
            var pooled_callbacks = PooledCallbacks;
            if (pooled_callbacks.Count > 0)
            {
                event_callback = pooled_callbacks.Dequeue();
                event_callback.Invoke = callback;
            }
            else
            {
                event_callback = new Callback(callback);
            }
            Add(event_callback);
            return event_callback;
        }

        public void Invoke()
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke();
                callback = callback.Next;
            }
        }
    }

    public class Event<T>
    {
        public Event.Callback<T> First;
        public Event.Callback<T> Last;

        public void Add(Event.Callback<T> callback)
        {
            callback.Handler = this;
            var last = Last;
            if (last == null)
            {
                First = callback;
                Last = callback;
            }
            else
            {
                last.Next = callback;
                callback.Previous = last;
                Last = callback;
            }
        }

        public Event.Callback<T> Add(Action<T> callback)
        {
            Event.Callback<T> event_callback;
            var pooled_callbacks = Event.Callback<T>.PooledCallbacks;
            if (pooled_callbacks.Count > 0)
            {
                event_callback = pooled_callbacks.Dequeue();
                event_callback.Invoke = callback;
            }
            else
            {
                event_callback = new Event.Callback<T>(callback);
            }
            Add(event_callback);
            return event_callback;
        }

        public void Invoke(T arg0)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0);
                callback = callback.Next;
            }
        }
    }

    public class Event<T1, T2>
    {
        public Event.Callback<T1, T2> First;
        public Event.Callback<T1, T2> Last;

        public void Add(Event.Callback<T1, T2> callback)
        {
            callback.Handler = this;
            var last = Last;
            if (last == null)
            {
                First = callback;
                Last = callback;
            }
            else
            {
                last.Next = callback;
                callback.Previous = last;
                Last = callback;
            }
        }

        public Event.Callback<T1, T2> Add(Action<T1, T2> callback)
        {
            Event.Callback<T1, T2> event_callback;
            var pooled_callbacks = Event.Callback<T1, T2>.PooledCallbacks;
            if (pooled_callbacks.Count > 0)
            {
                event_callback = pooled_callbacks.Dequeue();
                event_callback.Invoke = callback;
            }
            else
            {
                event_callback = new Event.Callback<T1, T2>(callback);
            }
            //event_callback.number = Event.Callback<T1, T2>.nextNumber++;
            Add(event_callback);
            return event_callback;
        }
        
        public void Invoke()
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(default(T1), default(T2));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, default(T2));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1);
                callback = callback.Next;
            }
        }
    }

    public class Event<T1, T2, T3>
    {
        public Event.Callback<T1, T2, T3> First;
        public Event.Callback<T1, T2, T3> Last;

        public void Add(Event.Callback<T1, T2, T3> callback)
        {
            callback.Handler = this;
            var last = Last;
            if (last == null)
            {
                First = callback;
                Last = callback;
            }
            else
            {
                last.Next = callback;
                callback.Previous = last;
                Last = callback;
            }
        }

        public Event.Callback<T1, T2, T3> Add(Action<T1, T2, T3> callback)
        {
            Event.Callback<T1, T2, T3> event_callback;
            var pooled_callbacks = Event.Callback<T1, T2, T3>.PooledCallbacks;
            if (pooled_callbacks.Count > 0)
            {
                event_callback = pooled_callbacks.Dequeue();
                event_callback.Invoke = callback;
            }
            else
            {
                event_callback = new Event.Callback<T1, T2, T3>(callback);
            }
            Add(event_callback);
            return event_callback;
        }

        public void Invoke()
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(default(T1), default(T2), default(T3));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, default(T2), default(T3));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, default(T3));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, arg2);
                callback = callback.Next;
            }
        }
    }

    public class Event<T1, T2, T3, T4>
    {
        public Event.Callback<T1, T2, T3, T4> First;
        public Event.Callback<T1, T2, T3, T4> Last;

        public void Add(Event.Callback<T1, T2, T3, T4> callback)
        {
            callback.Handler = this;
            var last = Last;
            if (last == null)
            {
                First = callback;
                Last = callback;
            }
            else
            {
                last.Next = callback;
                callback.Previous = last;
                Last = callback;
            }
        }

        public Event.Callback<T1, T2, T3, T4> Add(Action<T1, T2, T3, T4> callback)
        {
            Event.Callback<T1, T2, T3, T4> event_callback;
            var pooled_callbacks = Event.Callback<T1, T2, T3, T4>.PooledCallbacks;
            if (pooled_callbacks.Count > 0)
            {
                event_callback = pooled_callbacks.Dequeue();
                event_callback.Invoke = callback;
            }
            else
            {
                event_callback = new Event.Callback<T1, T2, T3, T4>(callback);
            }
            Add(event_callback);
            return event_callback;
        }

        public void Invoke()
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(default(T1), default(T2), default(T3), default(T4));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, default(T2), default(T3), default(T4));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, default(T3), default(T4));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, arg2, default(T4));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, arg2, arg3);
                callback = callback.Next;
            }
        }
    }


    public class Event<T1, T2, T3, T4, T5>
    {
        public Event.Callback<T1, T2, T3, T4, T5> First;
        public Event.Callback<T1, T2, T3, T4, T5> Last;

        public void Add(Event.Callback<T1, T2, T3, T4, T5> callback)
        {
            callback.Handler = this;
            var last = Last;
            if (last == null)
            {
                First = callback;
                Last = callback;
            }
            else
            {
                last.Next = callback;
                callback.Previous = last;
                Last = callback;
            }
        }

        public Event.Callback<T1, T2, T3, T4, T5> Add(Event.Action<T1, T2, T3, T4, T5> callback)
        {
            Event.Callback<T1, T2, T3, T4, T5> event_callback;
            var pooled_callbacks = Event.Callback<T1, T2, T3, T4, T5>.PooledCallbacks;
            if (pooled_callbacks.Count > 0)
            {
                event_callback = pooled_callbacks.Dequeue();
                event_callback.Invoke = callback;
            }
            else
            {
                event_callback = new Event.Callback<T1, T2, T3, T4, T5>(callback);
            }
            Add(event_callback);
            return event_callback;
        }

        public void Invoke()
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(default(T1), default(T2), default(T3), default(T4), default(T5));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, default(T2), default(T3), default(T4), default(T5));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, default(T3), default(T4), default(T5));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, arg2, default(T4), default(T5));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, arg2, arg3, default(T5));
                callback = callback.Next;
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4)
        {
            var callback = First;
            while (callback != null)
            {
                callback.Invoke(arg0, arg1, arg2, arg3, arg4);
                callback = callback.Next;
            }
        }
    }
}
