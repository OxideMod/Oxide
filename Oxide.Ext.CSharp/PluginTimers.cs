using System;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class Timer
    {
        private Unity.Libraries.Timer.TimerInstance instance;

        public Timer(Unity.Libraries.Timer.TimerInstance instance)
        {
            this.instance = instance;
        }

        /// <summary>
        /// Gets the number of repetitions left on this timer
        /// </summary>
        public int Repetitions => instance.Repetitions;

        /// <summary>
        /// Gets the delay between each repetition
        /// </summary>
        public float Delay => instance.Delay;

        /// <summary>
        /// Gets the callback delegate
        /// </summary>
        public Action Callback => instance.Callback;

        /// <summary>
        /// Gets if this timer has been destroyed
        /// </summary>
        public bool Destroyed => instance.Destroyed;

        /// <summary>
        /// Gets the plugin to which this timer belongs, if any
        /// </summary>
        public Plugin Owner => instance.Owner;

        /// <summary>
        /// Destroys this timer
        /// </summary>
        public void Destroy() => instance.Destroy();
    }

    public class PluginTimers
    {
        private Unity.Libraries.Timer timer = Interface.Oxide.GetLibrary<Unity.Libraries.Timer>("Timer");
        private Plugin plugin;

        public PluginTimers(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Creates a timer which fires once after the specified delay
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="callback"></param>
        public Timer Once(float seconds, Action callback)
        {
            return new Timer(timer.Once(seconds, callback, plugin));
        }

        /// <summary>
        /// Creates a timer which fires once after the specified delay
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="callback"></param>
        public Timer In(float seconds, Action callback)
        {
            return new Timer(timer.Once(seconds, callback, plugin));
        }

        /// <summary>
        /// Creates a timer which continuously fires at the specified interval
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        public Timer Every(float interval, Action callback)
        {
            return new Timer(timer.Repeat(interval, -1, callback, plugin));
        }

        /// <summary>
        /// Creates a timer which fires a set number of times at the specified interval
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="repeats"></param>
        /// <param name="callback"></param>
        public Timer Repeat(float interval, int repeats, Action callback)
        {
            return new Timer(timer.Repeat(interval, repeats, callback, plugin));
        }
    }
}
