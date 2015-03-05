using System;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class PluginTimers
    {
        Core.Libraries.Timer timer = Interface.GetMod().GetLibrary<Core.Libraries.Timer>("Timer");
        Plugin plugin;

        public PluginTimers(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Creates a timer which fires once after the specified delay
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="callback"></param>
        public void Once(float seconds, Action callback)
        {
            timer.Once(seconds, callback, plugin);
        }

        /// <summary>
        /// Creates a timer which fires once after the specified delay
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="callback"></param>
        public void In(float seconds, Action callback)
        {
            timer.Once(seconds, callback, plugin);
        }

        /// <summary>
        /// Creates a timer which continuously fires at the specified interval
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="callback"></param>
        public void Every(float interval, Action callback)
        {
            timer.Repeat(interval, -1, callback, plugin);
        }

        /// <summary>
        /// Creates a timer which fires a set number of times at the specified interval
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="repeats"></param>
        /// <param name="callback"></param>
        public void Repeat(float interval, int repeats, Action callback)
        {
            timer.Repeat(interval, repeats, callback, plugin);
        }
    }
}
