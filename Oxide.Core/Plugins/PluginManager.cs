using Oxide.Core.Logging;
using System;
using System.Collections.Generic;

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
        private readonly IDictionary<string, Plugin> loadedPlugins;

        // All hook subscriptions
        private readonly IDictionary<string, IList<Plugin>> hookSubscriptions;

        // Stores the last time a deprecation warning was printed for a specific hook
        private readonly Dictionary<string, float> lastDeprecatedWarningAt = new Dictionary<string, float>();

        // Re-usable conflict list used for hook calls
        private readonly List<string> hookConflicts = new List<string>();

        /// <summary>
        /// Initializes a new instance of the PluginManager class
        /// </summary>
        public PluginManager(Logger logger)
        {
            // Initialize
            loadedPlugins = new Dictionary<string, Plugin>();
            hookSubscriptions = new Dictionary<string, IList<Plugin>>();
            Logger = logger;
        }

        /// <summary>
        /// Adds a plugin to this manager
        /// </summary>
        /// <param name="plugin"></param>
        public bool AddPlugin(Plugin plugin)
        {
            if (loadedPlugins.ContainsKey(plugin.Name)) return false;
            loadedPlugins.Add(plugin.Name, plugin);
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
            if (!loadedPlugins.ContainsKey(plugin.Name)) return false;
            loadedPlugins.Remove(plugin.Name);
            foreach (var list in hookSubscriptions.Values)
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
            return loadedPlugins.TryGetValue(name, out plugin) ? plugin : null;
        }

        /// <summary>
        /// Gets all plugins managed by this manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Plugin> GetPlugins() => loadedPlugins.Values;

        /// <summary>
        /// Subscribes the specified plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="plugin"></param>
        internal void SubscribeToHook(string hook, Plugin plugin)
        {
            if (!loadedPlugins.ContainsKey(plugin.Name) || !plugin.IsCorePlugin && hook.StartsWith("I")) return;
            IList<Plugin> sublist;
            if (!hookSubscriptions.TryGetValue(hook, out sublist))
            {
                sublist = new List<Plugin>();
                hookSubscriptions.Add(hook, sublist);
            }
            if (!sublist.Contains(plugin)) sublist.Add(plugin);
            //Logger.Write(LogType.Debug, $"Plugin {plugin.Name} is subscribing to hook '{hook}'!");
        }

        /// <summary>
        /// Unsubscribes the specified plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="plugin"></param>
        internal void UnsubscribeToHook(string hook, Plugin plugin)
        {
            if (!loadedPlugins.ContainsKey(plugin.Name) || !plugin.IsCorePlugin && hook.StartsWith("I")) return;
            IList<Plugin> sublist;
            if (hookSubscriptions.TryGetValue(hook, out sublist) && sublist.Contains(plugin))
                sublist.Remove(plugin);
            //Logger.Write(LogType.Debug, $"Plugin {plugin.Name} is unsubscribing to hook '{hook}'!");
        }

        /// <summary>
        /// Calls a hook on all plugins of this manager
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hook, params object[] args)
        {
            // Locate the sublist
            IList<Plugin> plugins;
            if (!hookSubscriptions.TryGetValue(hook, out plugins)) return null;
            if (plugins.Count == 0) return null;

            // Loop each item
            var values = new object[plugins.Count];
            var returnCount = 0;
            object finalValue = null;
            Plugin finalPlugin = null;
            for (var i = 0; i < plugins.Count; i++)
            {
                // Call the hook
                var value = plugins[i].CallHook(hook, args);
                if (value != null)
                {
                    values[i] = value;
                    finalValue = value;
                    finalPlugin = plugins[i];
                    returnCount++;
                }
            }

            // Is there a return value?
            if (returnCount == 0) return null;

            if (returnCount > 1 && finalValue != null)
            {
                // Notify log of hook conflict
                hookConflicts.Clear();
                for (var i = 0; i < plugins.Count; i++)
                {
                    var value = values[i];
                    if (value == null) continue;
                    if (value.GetType().IsValueType)
                    {
                        if (!values[i].Equals(finalValue))
                            hookConflicts.Add($"{plugins[i].Name} - {value} ({value.GetType().Name})");
                    }
                    else
                    {
                        if (values[i] != finalValue)
                            hookConflicts.Add($"{plugins[i].Name} - {value} ({value.GetType().Name})");
                    }
                }
                if (hookConflicts.Count > 0)
                {
                    hookConflicts.Add($"{finalPlugin.Name} ({finalValue} ({finalValue.GetType().Name}))");
                    Logger.Write(LogType.Warning, "Calling hook {0} resulted in a conflict between the following plugins: {1}", hook, string.Join(", ", hookConflicts.ToArray()));
                }
            }

            return finalValue;
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
            if (!hookSubscriptions.TryGetValue(oldHook, out plugins)) return null;
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
