using System;
using System.Diagnostics;
using System.Collections.Generic;

using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The timer library
    /// </summary>
    public class Timer : Library
    {
        #region Time Control

        private static Stopwatch stopwatch;

        private static float CurrentTime { get { return (float)stopwatch.Elapsed.TotalSeconds; } }

        static Timer()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        #endregion

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

            // The next rep time
            private float nextrep;

            /// <summary>
            /// Initialises a new instance of the TimerInstance class
            /// </summary>
            /// <param name="repetitions"></param>
            /// <param name="delay"></param>
            /// <param name="callback"></param>
            public TimerInstance(int repetitions, float delay, Action callback, Plugin owner)
            {
                Repetitions = repetitions;
                Delay = delay;
                Callback = callback;
                nextrep = CurrentTime + delay;
                Owner = owner;
                if (owner != null) owner.OnRemovedFromManager += owner_OnRemovedFromManager;
            }

            /// <summary>
            /// Called when the owner plugin was unloaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="manager"></param>
            private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
            {
                Destroy();
            }

            /// <summary>
            /// Destroys this timer
            /// </summary>
            public void Destroy()
            {
                Destroyed = true;
            }

            /// <summary>
            /// Updates this timer
            /// </summary>
            public void Update()
            {
                // Check if we need to rep
                float ctime = CurrentTime;
                if (ctime >= nextrep)
                {
                    nextrep += Delay;
                    try
                    {
                        Callback();
                    }
                    catch (Exception ex)
                    {
                        Destroy();
                        if (Owner != null)
                            Interface.GetMod().RootLogger.WriteException(string.Format("Failed to run a timer from {0}.lua.", Owner.Name), ex);
                        else
                            Interface.GetMod().RootLogger.WriteException("Failed to run a timer.", ex);
                    }

                    if (Repetitions > 0)
                    {
                        Repetitions--;
                        if (Repetitions == 0) Destroy();
                    }
                }
            }
        }

        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }


        private readonly HashSet<TimerInstance> alltimers;

        public Timer()
        {
            alltimers = new HashSet<TimerInstance>();
        }

        /// <summary>
        /// Updates all timers
        /// </summary>
        public void Update()
        {
            alltimers.RemoveWhere(timer => { timer.Update(); return timer.Destroyed; });   
        }

        /// <summary>
        /// Creates a timer that fires once
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("Once")]
        public TimerInstance Once(float delay, Action callback, Plugin owner = null)
        {
            TimerInstance timer = new TimerInstance(1, delay, callback, owner);
            alltimers.Add(timer);
            return timer;
        }

        /// <summary>
        /// Creates a timer that fires many times
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="reps"></param>
        /// <param name="callback"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("Repeat")]
        public TimerInstance Repeat(float delay, int reps, Action callback, Plugin owner = null)
        {
            TimerInstance timer = new TimerInstance(reps, delay, callback, owner);
            alltimers.Add(timer);
            return timer;
        }

        /// <summary>
        /// Creates a timer that fires once next frame
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        [LibraryFunction("NextFrame")]
        public TimerInstance NextFrame(Action callback)
        {
            TimerInstance timer = new TimerInstance(1, 0.0f, callback, null);
            alltimers.Add(timer);
            return timer;
        }
    }
}
