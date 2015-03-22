using System.IO;
using System.Collections.Generic;
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
        private ICollection<string> watchedplugins;

        // The syncroot
        private object syncroot;

        // The file changes queue
        private Queue<FileChange> filechanges;

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            // Initialize
            watchedplugins = new HashSet<string>();
            filechanges = new Queue<FileChange>();
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
            watcher.Created += watcher_Changed;
            watcher.Deleted += watcher_Changed;
            watcher.Error += watcher_Error;
            watcher.EnableRaisingEvents = true;
            //Interface.Oxide.LogDebug("FSWatcher started '{0}' {1}", directory, filter);
        }

        /// <summary>
        /// Adds a filename-plugin mapping to this watcher
        /// </summary>
        /// <param name="name"></param>
        public void AddMapping(string name)
        {
            //filename = Path.GetFullPath(filename);
            //Interface.Oxide.LogDebug("Added mapping '{0}'", filename);
            watchedplugins.Add(name);
        }

        /// <summary>
        /// Removes the specified mapping from this watcher
        /// </summary>
        /// <param name="name"></param>
        public void RemoveMapping(string name)
        {
            //filename = Path.GetFullPath(filename);
            watchedplugins.Remove(name);
        }

        /// <summary>
        /// Called when the watcher has registered a filesystem change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //string filename = Path.Combine(e.FullPath, e.Name);
            string name = Path.GetFileNameWithoutExtension(e.Name);
            //Interface.Oxide.LogDebug(filename);
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    lock (syncroot)
                        filechanges.Enqueue(new FileChange(name, e.ChangeType));
                    //Interface.Oxide.LogDebug("Changed plugin {0}", name);
                    break;
                case WatcherChangeTypes.Created:
                    lock (syncroot)
                        filechanges.Enqueue(new FileChange(name, e.ChangeType));
                    //Interface.Oxide.LogDebug("New plugin {0}", name);
                    break;
                case WatcherChangeTypes.Deleted:
                    lock (syncroot)
                        filechanges.Enqueue(new FileChange(name, e.ChangeType));
                    //Interface.Oxide.LogDebug("Deleted plugin {0}", name);
                    break;
            }
        }

        private void watcher_Error(object sender, ErrorEventArgs e)
        {
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogError("FSWatcher error: {0}", e.GetException());
                RemoteLogger.Exception("FSWatcher error", e.GetException());
            });
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
                    FileChange fileChange = filechanges.Dequeue();
                    //Interface.Oxide.LogDebug(filename);

                    switch (fileChange.ChangeType)
                    {
                        case WatcherChangeTypes.Changed:
                            if (watchedplugins.Contains(fileChange.Name))
                                FirePluginSourceChanged(fileChange.Name);
                            else
                                FirePluginAdded(fileChange.Name);
                            break;
                        case WatcherChangeTypes.Created:
                            FirePluginAdded(fileChange.Name);
                            break;
                        case WatcherChangeTypes.Deleted:
                            if (watchedplugins.Contains(fileChange.Name))
                                FirePluginRemoved(fileChange.Name);
                            break;
                    }
                }
            }
        }
    }
}
