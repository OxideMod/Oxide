using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            if (FileExtension == null || !Directory.Exists(directory)) yield break;

            var files = new DirectoryInfo(directory).GetFiles("*" + FileExtension);
            var filtered = files.Where(f => (f.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden);
            foreach (var file in filtered) yield return Utility.GetFileNameWithoutExtension(file.FullName);
        }

        /// <summary>
        /// Loads a plugin given the specified name
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Plugin Load(string directory, string name)
        {
            if (LoadingPlugins.Contains(name))
            {
                Interface.Oxide.LogDebug("Load requested for plugin which is already loading: {0}", name);
                return null;
            }

            var filename = Path.Combine(directory, name + FileExtension);
            var plugin = GetPlugin(filename);
            LoadingPlugins.Add(plugin.Name);
            Interface.Oxide.NextTick(() => LoadPlugin(plugin));

            return null;
        }

        /// <summary>
        /// Gets a plugin given the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected virtual Plugin GetPlugin(string filename) => null;

        /// <summary>
        /// Loads a given plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="waitingForAccess"></param>
        protected void LoadPlugin(Plugin plugin, bool waitingForAccess = false)
        {
            if (!File.Exists(plugin.Filename))
            {
                LoadingPlugins.Remove(plugin.Name);
                Interface.Oxide.LogWarning("Script no longer exists: {0}", plugin.Name);
                return;
            }

            try
            {
                plugin.Load();
                Interface.Oxide.UnloadPlugin(plugin.Name);
                LoadingPlugins.Remove(plugin.Name);
                Interface.Oxide.PluginLoaded(plugin);
            }
            catch (IOException)
            {
                if (!waitingForAccess) Interface.Oxide.LogWarning("Waiting for another application to stop using script: {0}", plugin.Name);
                Interface.Oxide.GetLibrary<Timer>().Once(.5f, () => LoadPlugin(plugin, true));
            }
            catch (Exception ex)
            {
                LoadingPlugins.Remove(plugin.Name);
                Interface.Oxide.LogException($"Failed to load plugin {plugin.Name}", ex);
            }
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
