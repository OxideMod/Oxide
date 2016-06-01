using System;
using System.Collections.Generic;
using System.Threading;

using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The timer library
    /// </summary>
    public class Timer : Library
    {
        public static int Count { get; private set; }

        internal static Queue<TimerInstance> PooledInstances = new Queue<TimerInstance>();

        private readonly Thread mainThread = Thread.CurrentThread;

        public class TimeSlot
        {
            public int Count;
            public TimerInstance FirstInstance;
            public TimerInstance LastInstance;

            public void Update(float now)
            {
                var instance = FirstInstance;
                while (instance != null)
                {
                    if (instance.ExpiresAt > now) break;
                    var next_instance = instance.NextInstance;
                    instance.Update(now);
                    instance = next_instance;
                }
            }

            public void InsertTimer(TimerInstance timer)
            {
                var nextrep = timer.ExpiresAt;

                var first_instance = FirstInstance;
                var last_instance = LastInstance;

                var next_instance = first_instance;
                if (first_instance != null)
                {
                    var first_at = first_instance.ExpiresAt;
                    var last_at = last_instance.ExpiresAt;
                    if (nextrep <= first_at)
                    {
                        next_instance = first_instance;
                    }
                    else if (nextrep >= last_at)
                    {
                        next_instance = null;
                    }
                    else if (last_at - nextrep < nextrep - first_at)
                    {
                        next_instance = last_instance;
                        var instance = next_instance;
                        while (instance != null)
                        {
                            if (instance.ExpiresAt <= nextrep)
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
                            if (next_instance.ExpiresAt > nextrep) break;
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
            internal bool HasBeenRemoved;

            internal TimeSlot TimeSlot;
            internal TimerInstance NextInstance;
            internal TimerInstance PreviousInstance;

            private Event.Callback<Plugin, PluginManager> removedFromManager;

            private readonly Timer timer;

            /// <summary>
            /// Initializes a new instance of the TimerInstance class
            /// </summary>
            /// <param name="timer"></param>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public TimerInstance(Timer timer, int repetitions, float delay, Action callback, Plugin owner)
            {
                this.timer = timer;
                Load(repetitions, delay, callback, owner);
            }

            /// <summary>
            /// Load into an existing timer instance
            /// </summary>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
            /// <param name="owner"></param>
            public void Load(int repetitions, float delay, Action callback, Plugin owner)
            {
                Repetitions = repetitions;
                Delay = delay;
                Callback = callback;
                ExpiresAt = Interface.Oxide.Now + delay;
                Owner = owner;
                removedFromManager = owner?.OnRemovedFromManager.Add(OnRemovedFromManager);
                Count++;
                Destroyed = false;
                HasBeenRemoved = false;
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void OnRemovedFromManager(Plugin sender, PluginManager manager) => Destroy();

            /// <summary>
            /// Destroys this timer
            /// </summary>
            public bool Destroy()
            {
                if (Destroyed) return false;
                Destroyed = true;
                Remove();
                Count--;
                Event.Remove(ref removedFromManager);
                return true;
            }

            /// <summary>
            /// Destroys this timer and adds the instances to the pool
            /// </summary>
            public void DestroyToPool()
            {
                if (Destroy()) PooledInstances.Enqueue(this);
            }

            internal void Added(TimeSlot time_slot)
            {
                time_slot.Count++;
                Count++;
                TimeSlot = time_slot;
                HasBeenRemoved = false;
            }

            internal void Update(float now)
            {
                if (Destroyed) return;

                Remove();

                Owner?.TrackStart();
                try
                {
                    Callback();
                }
                catch (Exception ex)
                {
                    Destroyed = true;
                    var error_message = $"Failed to run a {Delay:0.00} timer";
                    if (Owner && Owner != null) error_message += $" in '{Owner.Name} v{Owner.Version}'";
                    Interface.Oxide.LogException(error_message, ex);
                }
                Owner?.TrackEnd();

                if (Repetitions > 0)
                {
                    Repetitions--;
                    if (Repetitions == 0)
                    {
                        Destroyed = true;
                    }
                    else
                    {
                        var scheduled_at = ExpiresAt + Delay;
                        ExpiresAt = scheduled_at;
                        timer.InsertTimer(this, scheduled_at < now);
                    }
                }
                else
                {
                    var scheduled_at = ExpiresAt + Delay;
                    ExpiresAt = scheduled_at;
                    timer.InsertTimer(this, scheduled_at < now);
                }
            }

            internal void Remove()
            {
                if (HasBeenRemoved) return;
                HasBeenRemoved = true;

                var slot = TimeSlot;
                var previous = PreviousInstance;
                var next = NextInstance;

                slot.Count--;
                Count--;

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
            }
        }

        public override bool IsGlobal => false;

        /// <summary>
        /// An even number of time slots is required. More slots means more efficient inserts with a higher number of timers but also more per-frame overhead.
        /// </summary>
        public const int MaxTimeSlots = 512;
        public const int LastTimeSlot = MaxTimeSlots - 1;
        public const float TickDuration = .01f;

        private readonly TimeSlot[] timeSlots = new TimeSlot[MaxTimeSlots];
        private int currentSlot;
        private float lastUpdateAt;

        public Timer()
        {
            for (var i = 0; i < MaxTimeSlots; i++)
                timeSlots[i] = new TimeSlot();
        }

        /// <summary>
        /// Called every server frame to process expired timers
        /// </summary>
        public void Update(float delta)
        {
            var now = Interface.Oxide.Now;

            var last_update_at = lastUpdateAt;
            if (last_update_at == 0)
            {
                lastUpdateAt = Interface.Oxide.Now;
                currentSlot = (int)(lastUpdateAt / TickDuration) % MaxTimeSlots;
                return;
            }

            var time_slots = timeSlots;
            var slots_remaining = (int)((now - last_update_at) / TickDuration);
            var checked_slots = 0;

            while (true)
            {
                var current_slot = currentSlot;
                time_slots[current_slot].Update(now);

                // Only move to the next slot once real time is out of the current slot so that the current slot is rechecked each frame
                if (--slots_remaining < 0) break;

                checked_slots++;

                if (current_slot < LastTimeSlot)
                    currentSlot = current_slot + 1;
                else
                    currentSlot = 0;
            }

            if (checked_slots > 1)
            {
                lastUpdateAt = last_update_at + checked_slots * TickDuration;
            }
        }

        private TimerInstance AddTimer(int repetitions, float delay, Action callback, Plugin owner = null)
        {
            TimerInstance timer;
            //TODO: complete pooled instance support
            /*var pooled_instances = PooledInstances;
            if (pooled_instances.Count > 0)
            {
                timer = pooled_instances.Dequeue();
                timer.Load(repetitions, delay, callback, owner);
            }
            else*/
            {
                timer = new TimerInstance(this, repetitions, delay, callback, owner);
            }
            if (Thread.CurrentThread == mainThread)
                InsertTimer(timer);
            else
                ScheduleInsert(timer);
            return timer;
        }

        private void ScheduleInsert(TimerInstance timer)
        {
            Interface.Oxide.NextTick(() => InsertTimer(timer));
        }

        private void InsertTimer(TimerInstance timer, bool in_past = false)
        {
            var current_slot = currentSlot;
            int index;
            if (in_past)
                index = current_slot < LastTimeSlot ? current_slot + 1 : 0;
            else
                index = (int)(timer.ExpiresAt / TickDuration) % MaxTimeSlots;
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
