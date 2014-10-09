using System;

namespace Oxide.Core.Plugins.Watchers
{
    public delegate void PluginChangeEvent(Plugin plugin);

    /// <summary>
    /// A class that watches for changes in the source of an arbitrary set of plugins
    /// </summary>
    public abstract class PluginChangeWatcher
    {
        /// <summary>
        /// Called when the source of the plugin has changed
        /// </summary>
        public event PluginChangeEvent OnPluginSourceChanged;

        /// <summary>
        /// Checks to see if changes have been made and fires the OnPluginSourceChanged as needed
        /// </summary>
        public abstract void UpdateChangeStatus();

        /// <summary>
        /// Fires the OnPluginSourceChanged event
        /// </summary>
        /// <param name="plugin"></param>
        protected void FirePluginSourceChanged(Plugin plugin)
        {
            if (OnPluginSourceChanged != null)
                OnPluginSourceChanged(plugin);
        }
    }
}
