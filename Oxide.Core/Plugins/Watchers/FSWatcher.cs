using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

using Oxide.Core.Libraries;

namespace Oxide.Core.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class FSWatcher : PluginChangeWatcher
    {
        class QueuedChanges : HashSet<WatcherChangeTypes>
        {
            public Timer.TimerInstance timer;
        }

        // The filesystem watcher
        private FileSystemWatcher watcher;

        // The plugin list
        private ICollection<string> watchedPlugins;

        // Changes are buffered briefly to avoid duplicate events
        private Dictionary<string, QueuedChanges> changeQueue;

        private Timer timers;

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            watchedPlugins = new HashSet<string>();
            changeQueue = new Dictionary<string, QueuedChanges>();
            timers = Interface.Oxide.GetLibrary<Timer>();
            
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
        }

        /// <summary>
        /// Adds a filename-plugin mapping to this watcher
        /// </summary>
        /// <param name="name"></param>
        public void AddMapping(string name)
        {
            watchedPlugins.Add(name);
        }

        /// <summary>
        /// Removes the specified mapping from this watcher
        /// </summary>
        /// <param name="name"></param>
        public void RemoveMapping(string name)
        {
            watchedPlugins.Remove(name);
        }

        /// <summary>
        /// Called when the watcher has registered a filesystem change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var name = Path.GetFileNameWithoutExtension(e.Name);
            QueuedChanges queued_changes;
            if (!changeQueue.TryGetValue(name, out queued_changes))
            {
                queued_changes = new QueuedChanges();
                changeQueue[name] = queued_changes;
            }
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    if (!queued_changes.Contains(WatcherChangeTypes.Created))
                        queued_changes.Add(e.ChangeType);
                    break;
                case WatcherChangeTypes.Created:
                    if (queued_changes.Remove(WatcherChangeTypes.Deleted))
                        queued_changes = new QueuedChanges { WatcherChangeTypes.Changed };
                    else
                        queued_changes = new QueuedChanges { e.ChangeType };
                    break;
                case WatcherChangeTypes.Deleted:
                    queued_changes = new QueuedChanges { e.ChangeType };
                    break;
            }
            queued_changes.timer?.Destroy();
            queued_changes.timer = timers.Once(.1f, () =>
            {
                queued_changes.timer = null;
                foreach (var change_type in queued_changes)
                {
                    switch (change_type)
                    {
                        case WatcherChangeTypes.Changed:
                            if (watchedPlugins.Contains(name))
                                FirePluginSourceChanged(name);
                            else
                                FirePluginAdded(name);
                            break;
                        case WatcherChangeTypes.Created:
                            FirePluginAdded(name);
                            break;
                        case WatcherChangeTypes.Deleted:
                            if (watchedPlugins.Contains(name))
                                FirePluginRemoved(name);
                            break;
                    }
                }
                changeQueue.Remove(name);
            });
        }

        private void watcher_Error(object sender, ErrorEventArgs e)
        {
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogError("FSWatcher error: {0}", e.GetException());
                RemoteLogger.Exception("FSWatcher error", e.GetException());
            });
        }
    }
}
