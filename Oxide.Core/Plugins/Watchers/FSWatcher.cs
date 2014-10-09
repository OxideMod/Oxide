using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;

namespace Oxide.Core.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class FSWatcher : PluginChangeWatcher
    {
        // The filesystem watcher
        private FileSystemWatcher watcher;

        // The plugin list
        private IDictionary<string, Plugin> watchedplugins;

        // The syncroot
        private object syncroot;

        // The file changes queue
        private Queue<string> filechanges;

        /// <summary>
        /// Initialises a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            // Initialise
            watchedplugins = new Dictionary<string, Plugin>();
            filechanges = new Queue<string>();
            syncroot = new object();

            // Load watcher
            LoadWatcher(directory, filter);
        }

        /// <summary>
        /// Loads the filesystem watcher
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void LoadWatcher(string directory, string filter)
        {
            // Create the watcher
            watcher = new FileSystemWatcher(directory, filter);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += watcher_Changed;
            watcher.EnableRaisingEvents = true;
            //Interface.GetMod().RootLogger.Write(Logging.LogType.Debug, "FSWatcher started '{0}' {1}", directory, filter);
        }

        /// <summary>
        /// Adds a filename-plugin mapping to this watcher
        /// </summary>
        /// <param name="plugin"></param>
        public void AddMapping(string filename, Plugin plugin)
        {
            filename = Path.GetFullPath(filename);
            //Interface.GetMod().RootLogger.Write(Logging.LogType.Debug, "Added mapping '{0}'", filename);
            watchedplugins[filename] = plugin;
        }

        /// <summary>
        /// Removes the specified mapping from this watcher
        /// </summary>
        /// <param name="filename"></param>
        public void RemoveMapping(string filename)
        {
            filename = Path.GetFullPath(filename);
            watchedplugins.Remove(filename);
        }

        /// <summary>
        /// Removes all mappings associated with the specified plugin from this watcher
        /// </summary>
        /// <param name="plugin"></param>
        public void RemoveMappings(Plugin plugin)
        {
            HashSet<string> toremove = new HashSet<string>(watchedplugins
                .Where((pair) => pair.Value == plugin)
                .Select((pair) => pair.Key));
            foreach (string filename in toremove)
                RemoveMapping(filename);
        }

        /// <summary>
        /// Called when the watcher has registered a filesystem change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string filename = Path.Combine(e.FullPath, e.Name);
            //Interface.GetMod().RootLogger.Write(Logging.LogType.Debug, filename);
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    
                    Plugin plugin;
                    if (watchedplugins.TryGetValue(filename, out plugin))
                    {
                        lock (syncroot)
                            filechanges.Enqueue(filename);
                        //Interface.GetMod().RootLogger.Write(Logging.LogType.Debug, "Queued");
                    }
                    else
                    {
                        //Interface.GetMod().RootLogger.Write(Logging.LogType.Debug, "No plugin match {0}", watchedplugins);
                    }
                    break;
            }
        }

        /// <summary>
        /// Fires all change events
        /// </summary>
        public override void UpdateChangeStatus()
        {
            lock (syncroot)
            {
                while (filechanges.Count > 0)
                {
                    string filename = filechanges.Dequeue();
                    //Interface.GetMod().RootLogger.Write(Logging.LogType.Debug, filename);

                    Plugin plugin;
                    if (watchedplugins.TryGetValue(filename, out plugin))
                        FirePluginSourceChanged(plugin);
                }
            }
        }
    }
}
