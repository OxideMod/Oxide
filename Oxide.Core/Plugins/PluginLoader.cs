using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
        public ConcurrentHashSet<string> LoadingPlugins { get; } = new ConcurrentHashSet<string>();

        /// <summary>
        /// Optional loaded plugin instances used by loaders which need to be notified before a plugin is unloaded
        /// </summary>
        public Dictionary<string, Plugin> LoadedPlugins = new Dictionary<string, Plugin>();

        /// <summary>
        /// Stores the last error a plugin had while loading
        /// </summary>
        public Dictionary<string, string> PluginErrors { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Stores the names of core plugins which should never be unloaded
        /// </summary>
        public virtual Type[] CorePlugins { get; } = new Type[0];

        /// <summary>
        /// Stores the plugin file extension which this loader supports
        /// </summary>
        public virtual string FileExtension { get; }

        /// <summary>
        /// Scans the specified directory and returns a set of plugin names for plugins that this loader can load
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> ScanDirectory(string directory)
        {
            if (FileExtension == null) yield break;
            foreach (string file in Directory.GetFiles(directory, "*" + FileExtension))
                yield return Path.GetFileNameWithoutExtension(file);
        }

        /// <summary>
        /// Loads a plugin given the specified name
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Plugin Load(string directory, string name)
        {
            return null;
        }

        /// <summary>
        /// Reloads a plugin given the specified name, implemented by plugin loaders which support reloading plugins
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual void Reload(string directory, string name)
        {
            Interface.Oxide.UnloadPlugin(name);
            Interface.Oxide.LoadPlugin(name);
        }

        /// <summary>
        /// Called when a plugin which was loaded by this loader is being unloaded by the plugin manager
        /// </summary>
        /// <param name="plugin"></param>
        public virtual void Unloading(Plugin plugin)
        {

        }
    }
}
