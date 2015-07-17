namespace Oxide.Core.Plugins.Watchers
{
    public delegate void PluginChangeEvent(string name);
    public delegate void PluginAddEvent(string name);
    public delegate void PluginRemoveEvent(string name);

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
        /// Called when new plugin has been added
        /// </summary>
        public event PluginAddEvent OnPluginAdded;

        /// <summary>
        /// Called when new plugin has been removed
        /// </summary>
        public event PluginRemoveEvent OnPluginRemoved;

        /// <summary>
        /// Fires the OnPluginSourceChanged event
        /// </summary>
        /// <param name="name"></param>
        protected void FirePluginSourceChanged(string name)
        {
            if (OnPluginSourceChanged != null)
                OnPluginSourceChanged(name);
        }

        /// <summary>
        /// Fires the OnPluginAdded event
        /// </summary>
        /// <param name="name"></param>
        protected void FirePluginAdded(string name)
        {
            if (OnPluginAdded != null)
                OnPluginAdded(name);
        }

        /// <summary>
        /// Fires the OnPluginRemoved event
        /// </summary>
        /// <param name="name"></param>
        protected void FirePluginRemoved(string name)
        {
            if (OnPluginRemoved != null)
                OnPluginRemoved(name);
        }
    }
}
