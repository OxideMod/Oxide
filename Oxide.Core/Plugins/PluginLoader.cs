using System;
using System.Collections.Generic;

namespace Oxide.Core.Plugins
{
    /// <summary>
    /// Represents a loader for a certain type of plugin
    /// </summary>
    public abstract class PluginLoader
    {
        /// <summary>
        /// Stores the names of plugins which are currently loading asynchronously
        /// </summary>
        public List<string> LoadingPlugins = new List<string>();

        /// <summary>
        /// Scans the specified directory and returns a set of plugin names for plugins that this loader can load
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public abstract IEnumerable<string> ScanDirectory(string directory);

        /// <summary>
        /// Loads a plugin given the specified name
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract Plugin Load(string directory, string name);

        /// <summary>
        /// Reloads a plugin given the specified name, implemented by plugin loaders which support reloading plugins
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual void Reload(string directory, string name)
        {
        }
    }
}
