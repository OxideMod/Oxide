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
            if (OnPluginAdded != null) OnPluginAdded(plugin);
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
            foreach (IList<Plugin> list in hooksubscriptions.Values)
                if (list.Contains(plugin)) list.Remove(plugin);
            plugin.HandleRemovedFromManager(this);
            if (OnPluginRemoved != null) OnPluginRemoved(plugin);
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
            if (loadedplugins.TryGetValue(name, out plugin)) return plugin;
            return null;
        }

        /// <summary>
        /// Gets all plugins managed by this manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Plugin> GetPlugins()
        {
            return loadedplugins.Values;
        }

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
            sublist.Add(plugin);
            //Logger.Write(LogType.Debug, "Plugin {0} is subscribing to hook '{1}'!", plugin.Name, hookname);
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
            object[] values = new object[plugins.Count];
            int returncount = 0;
            object finalvalue = null;
            Plugin finalplugin = null;
            for (int i = 0; i < plugins.Count; i++)
            {
                // Call the hook
                object value = plugins[i].CallHook(hookname, args);
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
                string[] conflicts = new string[returncount];
                int j = 0;
                for (int i = 0; i < plugins.Count; i++)
                {
                    if (values[i] != null && values[i] != finalvalue)
                        conflicts[j++] = plugins[i].Name;
                }
                if (j > 0)
                {
                    conflicts[j] = finalplugin.Name;
                    Logger.Write(LogType.Warning, "Calling hook {0} resulted in a conflict between the following plugins: {1}", hookname, string.Join(", ", conflicts));
                }
            }
            return finalvalue;
        }

        /// <summary>
        /// Calls a hook on all plugins of this manager and prints a deprecation warning
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallDeprecatedHook(string name, params object[] args)
        {
            IList<Plugin> plugins;
            if (!hooksubscriptions.TryGetValue(name, out plugins)) return null;
            if (plugins.Count == 0) return null;

            var now = Interface.Oxide.Now;
            float last_warning_at;
            if (!lastDeprecatedWarningAt.TryGetValue(name, out last_warning_at) || now - last_warning_at > 300f)
            {
                lastDeprecatedWarningAt[name] = now;
                Interface.Oxide.LogWarning("'{0} v{1}' plugin is using deprecated hook: {2}", plugins[0].Name, plugins[0].Version, name);
            }

            return CallHook(name, args);
        }
    }
}
