using System;
using System.Collections.Generic;

using Oxide.Core.Logging;

namespace Oxide.Core.Plugins
{
    public delegate void PluginEvent(Plugin plugin);

    /// <summary>
    /// Manages a set of plugins
    /// </summary>
    public sealed class PluginManager
    {
        /// <summary>
        /// Gets the logger to which this plugin manager writes
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Gets or sets the path for plugin configs
        /// </summary>
        public string ConfigPath { get; set; }

        /// <summary>
        /// Called when a plugin has been added
        /// </summary>
        public event PluginEvent OnPluginAdded;

        /// <summary>
        /// Called when a plugin has been removed
        /// </summary>
        public event PluginEvent OnPluginRemoved;

        // All loaded plugins
        private readonly IDictionary<string, Plugin> loadedplugins;

        // All hook subscriptions
        private readonly IDictionary<string, IList<Plugin>> hooksubscriptions;

        // Stores the last time a deprecation warning was printed for a specific hook
        private Dictionary<string, float> lastDeprecatedWarningAt = new Dictionary<string, float>();

        /// <summary>
        /// Initializes a new instance of the PluginManager class
        /// </summary>
        public PluginManager(Logger logger)
        {
            // Initialize
            loadedplugins = new Dictionary<string, Plugin>();
            hooksubscriptions = new Dictionary<string, IList<Plugin>>();
            Logger = logger;
        }

        /// <summary>
        /// Adds a plugin to this manager
        /// </summary>
        /// <param name="plugin"></param>
        public bool AddPlugin(Plugin plugin)
        {
            if (loadedplugins.ContainsKey(plugin.Name)) return false;
            loadedplugins.Add(plugin.Name, plugin);
            plugin.HandleAddedToManager(this);
            OnPluginAdded?.Invoke(plugin);
            return true;
        }

        /// <summary>
        /// Removes a plugin from this manager
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public bool RemovePlugin(Plugin plugin)
        {
            if (!loadedplugins.ContainsKey(plugin.Name)) return false;
            loadedplugins.Remove(plugin.Name);
            foreach (var list in hooksubscriptions.Values)
                if (list.Contains(plugin)) list.Remove(plugin);
            plugin.HandleRemovedFromManager(this);
            OnPluginRemoved?.Invoke(plugin);
            return true;
        }

        /// <summary>
        /// Gets a plugin by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Plugin GetPlugin(string name)
        {
            Plugin plugin;
            return loadedplugins.TryGetValue(name, out plugin) ? plugin : null;
        }

        /// <summary>
        /// Gets all plugins managed by this manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Plugin> GetPlugins() => loadedplugins.Values;

        /// <summary>
        /// Subscribes the specified plugin to the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="plugin"></param>
        internal void SubscribeToHook(string hookname, Plugin plugin)
        {
            if (!loadedplugins.ContainsKey(plugin.Name) || !plugin.IsCorePlugin && hookname.StartsWith("I")) return;
            IList<Plugin> sublist;
            if (!hooksubscriptions.TryGetValue(hookname, out sublist))
            {
                sublist = new List<Plugin>();
                hooksubscriptions.Add(hookname, sublist);
            }
            if (!sublist.Contains(plugin)) sublist.Add(plugin);
            //Logger.Write(LogType.Debug, $"Plugin {plugin.Name} is subscribing to hook '{hookname}'!");
        }

        /// <summary>
        /// Unsubscribes the specified plugin to the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="plugin"></param>
        internal void UnsubscribeToHook(string hookname, Plugin plugin)
        {
            if (!loadedplugins.ContainsKey(plugin.Name) || !plugin.IsCorePlugin && hookname.StartsWith("I")) return;
            IList<Plugin> sublist;
            if (hooksubscriptions.TryGetValue(hookname, out sublist) && sublist.Contains(plugin))
                sublist.Remove(plugin);
            //Logger.Write(LogType.Debug, $"Plugin {plugin.Name} is unsubscribing to hook '{hookname}'!");
        }

        /// <summary>
        /// Calls a hook on all plugins of this manager
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, params object[] args)
        {
            // Locate the sublist
            IList<Plugin> plugins;
            if (!hooksubscriptions.TryGetValue(hookname, out plugins)) return null;
            if (plugins.Count == 0) return null;

            // Loop each item
            var values = new object[plugins.Count];
            var returncount = 0;
            object finalvalue = null;
            Plugin finalplugin = null;
            for (var i = 0; i < plugins.Count; i++)
            {
                // Call the hook
                var value = plugins[i].CallHook(hookname, args);
                if (value != null)
                {
                    values[i] = value;
                    finalvalue = value;
                    finalplugin = plugins[i];
                    returncount++;
                }
            }

            // Is there a return value?
            if (returncount == 0) return null;

            if (returncount > 1 && finalvalue != null)
            {
                // Notify log of hook conflict
                var conflicts = new string[returncount];
                var j = 0;
                for (var i = 0; i < plugins.Count; i++)
                {
                    if (values[i] != null && values[i] != finalvalue)
                        conflicts[j++] = plugins[i].Name;
                }
                if (j > 0)
                {
                    conflicts[j] = finalplugin.Name;
                    Logger.Write(LogType.Warning, $"Calling hook {hookname} resulted in a conflict between the following plugins: {string.Join(", ", conflicts)}");
                }
            }
            return finalvalue;
        }

        /// <summary>
        /// Calls a hook on all plugins of this manager and prints a deprecation warning
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            IList<Plugin> plugins;
            if (!hooksubscriptions.TryGetValue(oldHook, out plugins)) return null;
            if (plugins.Count == 0) return null;
            if (expireDate < DateTime.Now) return null;

            var now = Interface.Oxide.Now;
            float lastWarningAt;
            if (!lastDeprecatedWarningAt.TryGetValue(oldHook, out lastWarningAt) || now - lastWarningAt > 300f)
            {
                lastDeprecatedWarningAt[oldHook] = now;
                Interface.Oxide.LogWarning($"'{plugins[0].Name} v{plugins[0].Version}' is using deprecated hook '{oldHook}', which will stop working on {expireDate.ToString("D")}. Please ask the author to update to '{newHook}'");
            }

            return CallHook(oldHook, args);
        }
    }
}
