using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The timer library
    /// </summary>
    public class Timer : Library
    {
        public override bool IsGlobal => false;

        public static int Count { get; private set; }

        internal static readonly object Lock = new object();

        internal static readonly OxideMod Oxide = Interface.Oxide;

        public class TimeSlot
        {
            public int Count;
            public TimerInstance FirstInstance;
            public TimerInstance LastInstance;

            public void GetExpired(double now, Queue<TimerInstance> queue)
            {
                var instance = FirstInstance;
                while (instance != null)
                {
                    if (instance.ExpiresAt > now) break;
                    queue.Enqueue(instance);
                    instance = instance.NextInstance;
                }
            }

            public void InsertTimer(TimerInstance timer)
            {
                var expires_at = timer.ExpiresAt;

                var first_instance = FirstInstance;
                var last_instance = LastInstance;

                var next_instance = first_instance;
                if (first_instance != null)
                {
                    var first_at = first_instance.ExpiresAt;
                    var last_at = last_instance.ExpiresAt;
                    if (expires_at <= first_at)
                    {
                        next_instance = first_instance;
                    }
                    else if (expires_at >= last_at)
                    {
                        next_instance = null;
                    }
                    else if (last_at - expires_at < expires_at - first_at)
                    {
                        next_instance = last_instance;
                        var instance = next_instance;
                        while (instance != null)
                        {
                            if (instance.ExpiresAt <= expires_at)
                            {
                                // We need to insert after this instance
                                break;
                            }
                            next_instance = instance;
                            instance = instance.PreviousInstance;
                        }
                    }
                    else
                    {
                        while (next_instance != null)
                        {
                            if (next_instance.ExpiresAt > expires_at) break;
                            next_instance = next_instance.NextInstance;
                        }
                    }
                }

                if (next_instance == null)
                {
                    timer.NextInstance = null;
                    if (last_instance == null)
                    {
                        FirstInstance = timer;
                        LastInstance = timer;
                    }
                    else
                    {
                        last_instance.NextInstance = timer;
                        timer.PreviousInstance = last_instance;
                        LastInstance = timer;
                    }
                }
                else
                {
                    var previous = next_instance.PreviousInstance;
                    if (previous == null)
                    {
                        FirstInstance = timer;
                    }
                    else
                    {
                        previous.NextInstance = timer;
                    }
                    next_instance.PreviousInstance = timer;
                    timer.PreviousInstance = previous;
                    timer.NextInstance = next_instance;
                }

                timer.Added(this);
            }
        }

        /// <summary>
        /// Represents a single timer instance
        /// </summary>
        public class TimerInstance
        {
            public const int MaxPooled = 5000;

            internal static Queue<TimerInstance> Pool = new Queue<TimerInstance>();

            /// <summary>
            /// Gets the number of repetitions left on this timer
            /// </summary>
            public int Repetitions { get; private set; }

            /// <summary>
            /// Gets the delay between each repetition
            /// </summary>
            public float Delay { get; private set; }

            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action Callback { get; private set; }

            /// <summary>
            /// Gets if this timer has been destroyed
            /// </summary>
            public bool Destroyed { get; private set; }

            /// <summary>
            /// Gets the plugin to which this timer belongs, if any
            /// </summary>
            public Plugin Owner { get; private set; }

            internal float ExpiresAt;

            internal TimeSlot TimeSlot;
            internal TimerInstance NextInstance;
            internal TimerInstance PreviousInstance;

            private Event.Callback<Plugin, PluginManager> removedFromManager;

            private Timer timer;

            internal TimerInstance(Timer timer, int repetitions, float delay, Action callback, Plugin owner)
            {
                Load(timer, repetitions, delay, callback, owner);
            }

            internal void Load(Timer timer, int repetitions, float delay, Action callback, Plugin owner)
            {
                this.timer = timer;
                Repetitions = repetitions;
                Delay = delay;
                Callback = callback;
                ExpiresAt = Oxide.Now + delay;
                Owner = owner;
                Destroyed = false;
                if (owner != null) removedFromManager = owner.OnRemovedFromManager.Add(OnRemovedFromManager);
            }

            /// <summary>
            /// Resets the timer optionally changing the delay setting a number of repetitions
            /// </summary>
            /// <param name="delay">The new delay between repetitions</param>
            /// <param name="repetitions">Number of repetitions before being destroyed</param>
            public void Reset(float delay = -1, int repetitions = 1)
            {
                lock (Lock)
                {
                    if (delay < 0)
                        delay = Delay;
                    else
                        Delay = delay;
                    Repetitions = repetitions;
                    ExpiresAt = Oxide.Now + delay;
                    if (Destroyed)
                    {
                        Destroyed = false;
                        var owner = Owner;
                        if (owner != null) removedFromManager = owner.OnRemovedFromManager.Add(OnRemovedFromManager);
                    }
                    else
                    {
                        Remove();
                    }
                    timer.InsertTimer(this);
                }
            }

            /// <summary>
            /// Destroys this timer
            /// </summary>
            public bool Destroy()
            {
                lock (Lock)
                {
                    if (Destroyed) return false;
                    Destroyed = true;
                    Remove();
                    Event.Remove(ref removedFromManager);
                }
                return true;
            }

            /// <summary>
            /// Destroys this timer and adds it to the pool
            /// </summary>
            public bool DestroyToPool()
            {
                lock (Lock)
                {
                    if (Destroyed) return false;
                    Destroyed = true;
                    Callback = null;
                    Remove();
                    Event.Remove(ref removedFromManager);
                    var pooled_instances = Pool;
                    if (pooled_instances.Count < MaxPooled) pooled_instances.Enqueue(this);
                }
                return true;
            }

            internal void Added(TimeSlot time_slot)
            {
                time_slot.Count++;
                Count++;
                TimeSlot = time_slot;
            }

            internal void Invoke(float now)
            {
                if (Repetitions > 0)
                {
                    if (--Repetitions == 0)
                    {
                        Destroy();
                        FireCallback();
                        return;
                    }
                }

                Remove();

                var expires_at = ExpiresAt + Delay;
                ExpiresAt = expires_at;
                timer.InsertTimer(this, expires_at < now);

                FireCallback();
            }

            internal void Remove()
            {
                var slot = TimeSlot;
                if (slot == null) return;

                slot.Count--;
                Count--;

                var previous = PreviousInstance;
                var next = NextInstance;

                if (next == null)
                {
                    slot.LastInstance = previous;
                }
                else
                {
                    next.PreviousInstance = previous;
                }

                if (previous == null)
                {
                    slot.FirstInstance = next;
                }
                else
                {
                    previous.NextInstance = next;
                }

                TimeSlot = null;
                PreviousInstance = null;
                NextInstance = null;
            }

            private void FireCallback()
            {
                Owner?.TrackStart();
                try
                {
                    Callback();
                }
                catch (Exception ex)
                {
                    Destroy();
                    var error_message = $"Failed to run a {Delay:0.00} timer";
                    if (Owner && Owner != null) error_message += $" in '{Owner.Name} v{Owner.Version}'";
                    Interface.Oxide.LogException(error_message, ex);
                    return;
                }
                finally
                {
                    Owner?.TrackEnd();
                }
            }

            private void OnRemovedFromManager(Plugin sender, PluginManager manager) => Destroy();
        }

        // An even number of time slots is required. More slots means more efficient inserts with a higher number of timers but also more per-frame overhead.
        public const int TimeSlots = 512;
        public const int LastTimeSlot = TimeSlots - 1;
        public const float TickDuration = .01f;

        private readonly TimeSlot[] timeSlots = new TimeSlot[TimeSlots];
        private readonly Queue<TimerInstance> expiredInstanceQueue = new Queue<TimerInstance>();

        private int currentSlot;

        // This needs to be a double in order to avoid precision errors
        private double nextSlotAt = TickDuration;

        public Timer()
        {
            for (var i = 0; i < TimeSlots; i++)
                timeSlots[i] = new TimeSlot();
        }

        /// <summary>
        /// Called every server frame to process expired timers
        /// </summary>
        public void Update(float delta)
        {
            var now = Oxide.Now;
            var time_slots = timeSlots;
            var expired_queue = expiredInstanceQueue;
            var checked_slots = 0;

            lock (Lock)
            {
                var current_slot = currentSlot;
                var next_slot_at = nextSlotAt;

                while (true)
                {
                    time_slots[current_slot].GetExpired(next_slot_at > now ? now : next_slot_at, expired_queue);

                    // Only move to the next slot once real time is out of the current slot so that the current slot is rechecked each frame
                    if (now <= next_slot_at) break;

                    checked_slots++;
                    current_slot = current_slot < LastTimeSlot ? current_slot + 1 : 0;
                    next_slot_at += TickDuration;
                }

                if (checked_slots > 0)
                {
                    currentSlot = current_slot;
                    nextSlotAt = next_slot_at;
                }

                var expired_count = expired_queue.Count;
                for (var i = 0; i < expired_count; i++)
                {
                    var instance = expired_queue.Dequeue();
                    if (!instance.Destroyed) instance.Invoke(now);
                }
            }
        }

        internal TimerInstance AddTimer(int repetitions, float delay, Action callback, Plugin owner = null)
        {
            lock (Lock)
            {
                TimerInstance timer;
                var pooled_instances = TimerInstance.Pool;
                if (pooled_instances.Count > 0)
                {
                    timer = pooled_instances.Dequeue();
                    timer.Load(this, repetitions, delay, callback, owner);
                }
                else
                {
                    timer = new TimerInstance(this, repetitions, delay, callback, owner);
                }
                InsertTimer(timer, timer.ExpiresAt < Oxide.Now);
                return timer;
            }
        }

        private void InsertTimer(TimerInstance timer, bool in_past = false)
        {
            var index = in_past ? currentSlot : (int)(timer.ExpiresAt / TickDuration) & LastTimeSlot;
            timeSlots[index].InsertTimer(timer);
        }

        /// <summary>
        /// Creates a timer that fires once
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("Once")]
        public TimerInstance Once(float delay, Action callback, Plugin owner = null) => AddTimer(1, delay, callback, owner);

        /// <summary>
        /// Creates a timer that fires many times
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="reps"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("Repeat")]
        public TimerInstance Repeat(float delay, int reps, Action callback, Plugin owner = null) => AddTimer(reps, delay, callback, owner);

        /// <summary>
        /// Creates a timer that fires once next frame
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        [LibraryFunction("NextFrame")]
        public TimerInstance NextFrame(Action callback) => AddTimer(1, 0.0f, callback);
    }
}
