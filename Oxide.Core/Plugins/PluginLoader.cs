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
        /// Scans the specified directory and returns a set of plugin names for plugins that this loader can load
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public abstract IEnumerable<string> ScanDirectory(string directory);

        /// <summary>
        /// Loads a plugin given the specified name
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract Plugin Load(string directory, string name);
    }
}
