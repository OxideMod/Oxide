using Oxide.Core.Libraries;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace Oxide.Core.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class FSWatcher : PluginChangeWatcher
    {
        private class QueuedChange
        {
            internal WatcherChangeTypes type;
            internal Timer.TimerInstance timer;
        }

        // The filesystem watcher
        private FileSystemWatcher watcher;

        // The plugin list
        private ICollection<string> watchedPlugins;

        // Changes are buffered briefly to avoid duplicate events
        private Dictionary<string, QueuedChange> changeQueue;

        private Timer timers;

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            watchedPlugins = new HashSet<string>();
            changeQueue = new Dictionary<string, QueuedChange>();
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
            watcher = new FileSystemWatcher(directory, filter)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += watcher_Changed;
            watcher.Created += watcher_Changed;
            watcher.Deleted += watcher_Changed;
            watcher.Error += watcher_Error;
        }

        /// <summary>
        /// Adds a filename-plugin mapping to this watcher
        /// </summary>
        /// <param name="name"></param>
        public void AddMapping(string name) => watchedPlugins.Add(name);

        /// <summary>
        /// Removes the specified mapping from this watcher
        /// </summary>
        /// <param name="name"></param>
        public void RemoveMapping(string name) => watchedPlugins.Remove(name);

        /// <summary>
        /// Called when the watcher has registered a filesystem change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var watcher = (FileSystemWatcher)sender;
            var length = e.FullPath.Length - watcher.Path.Length - Path.GetExtension(e.Name).Length - 1;
            var sub_path = e.FullPath.Substring(watcher.Path.Length + 1, length);
            QueuedChange change;
            if (!changeQueue.TryGetValue(sub_path, out change))
            {
                change = new QueuedChange();
                changeQueue[sub_path] = change;
            }
            change.timer?.Destroy();
            change.timer = null;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    if (change.type != WatcherChangeTypes.Created)
                        change.type = WatcherChangeTypes.Changed;
                    break;

                case WatcherChangeTypes.Created:
                    if (change.type == WatcherChangeTypes.Deleted)
                        change.type = WatcherChangeTypes.Changed;
                    else
                        change.type = WatcherChangeTypes.Created;
                    break;

                case WatcherChangeTypes.Deleted:
                    if (change.type == WatcherChangeTypes.Created)
                    {
                        changeQueue.Remove(sub_path);
                        return;
                    }
                    change.type = WatcherChangeTypes.Deleted;
                    break;
            }
            Interface.Oxide.NextTick(() =>
            {
                change.timer?.Destroy();
                change.timer = timers.Once(.2f, () =>
                {
                    change.timer = null;
                    changeQueue.Remove(sub_path);
                    if (Regex.Match(sub_path, @"Include\\", RegexOptions.IgnoreCase).Success)
                    {
                        if (change.type == WatcherChangeTypes.Created || change.type == WatcherChangeTypes.Changed)
                            FirePluginSourceChanged(sub_path);
                        return;
                    }
                    switch (change.type)
                    {
                        case WatcherChangeTypes.Changed:
                            if (watchedPlugins.Contains(sub_path))
                                FirePluginSourceChanged(sub_path);
                            else
                                FirePluginAdded(sub_path);
                            break;

                        case WatcherChangeTypes.Created:
                            FirePluginAdded(sub_path);
                            break;

                        case WatcherChangeTypes.Deleted:
                            if (watchedPlugins.Contains(sub_path))
                                FirePluginRemoved(sub_path);
                            break;
                    }
                });
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
