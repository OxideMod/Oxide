using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Oxide.Core.Plugins
{
    public delegate void PluginError(Plugin sender, string message);

    public class PluginManagerEvent : Event<Plugin, PluginManager> { }

    /// <summary>
    /// Represents a single plugin
    /// </summary>
    public abstract class Plugin
    {
        public static implicit operator bool(Plugin plugin) => plugin != null;

        public static bool operator !(Plugin plugin) => !(bool)plugin;

        /// <summary>
        /// Gets the source file name, if any
        /// </summary>
        public string Filename { get; protected set; }

        /// <summary>
        /// Gets the internal name of this plugin
        /// </summary>
        private string name;

        public string Name
        {
            get { return name; }
            set { if (string.IsNullOrEmpty(Name) || name == GetType().Name) name = value; }
        }

        /// <summary>
        /// Gets the user-friendly title of this plugin
        /// </summary>
        public string Title { get; protected set; }

        /// <summary>
        /// Gets the description of this plugin
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets the author of this plugin
        /// </summary>
        public string Author { get; protected set; }

        /// <summary>
        /// Gets the version of this plugin
        /// </summary>
        public VersionNumber Version { get; protected set; }

        /// <summary>
        /// Gets the resource ID associated with this plugin
        /// </summary>
        public int ResourceId { get; protected set; }

        /// <summary>
        /// Gets the plugin manager responsible for this plugin
        /// </summary>
        public PluginManager Manager { get; private set; }

        /// <summary>
        /// Gets if this plugin has a config file or not
        /// </summary>
        public bool HasConfig { get; protected set; }

        /// <summary>
        /// Gets if this plugin has a lang file or not
        /// </summary>
        public bool HasMessages { get; protected set; }

        /// <summary>
        /// Gets if this plugin should never be unloaded
        /// </summary>
        private bool isCorePlugin;

        public bool IsCorePlugin
        {
            get { return isCorePlugin; }
            set { if (!Interface.Oxide.HasLoadedCorePlugins) isCorePlugin = value; }
        }

        /// <summary>
        /// Gets the PluginLoader which loaded this plugin
        /// </summary>
        public PluginLoader Loader { get; set; }

        /// <summary>
        /// Gets the object associated with this plugin
        /// </summary>
        public virtual object Object => this;

        /// <summary>
        /// Gets the config file in use by this plugin
        /// </summary>
        public DynamicConfigFile Config { get; private set; }

        /// <summary>
        /// Called when this plugin has raised an error
        /// </summary>
        public event PluginError OnError;

        /// <summary>
        /// Called when this plugin was added to a manager
        /// </summary>
        public PluginManagerEvent OnAddedToManager = new PluginManagerEvent();

        /// <summary>
        /// Called when this plugin was removed from a manager
        /// </summary>
        public PluginManagerEvent OnRemovedFromManager = new PluginManagerEvent();

        /// <summary>
        /// Has this plugins Init/Loaded hook been called
        /// </summary>
        public bool IsLoaded { get; internal set; }

        /// <summary>
        /// Gets or sets the total hook time
        /// </summary>
        /// <value>The total hook time.</value>
        public double TotalHookTime { get; internal set; }

        // Used to measure time spent in this plugin
        private Stopwatch trackStopwatch = new Stopwatch();
        private Stopwatch stopwatch = new Stopwatch();
        //private float trackStartAt;
        private float averageAt;
        private double sum;
        private int preHookGcCount;

        // The depth of hook call nesting
        protected int nestcount;

        private class CommandInfo
        {
            public readonly string[] Names;
            public readonly string[] PermissionsRequired;
            public readonly CommandCallback Callback;

            public CommandInfo(string[] names, string[] perms, CommandCallback callback)
            {
                Names = names;
                PermissionsRequired = perms;
                Callback = callback;
            }
        }

        private IDictionary<string, CommandInfo> commandInfos;

        private Permission permission = Interface.Oxide.GetLibrary<Permission>();

        /// <summary>
        /// Initializes an empty version of the Plugin class
        /// </summary>
        protected Plugin()
        {
            Name = GetType().Name;
            Title = Name.Humanize();
            Author = "Unnamed";
            Version = new VersionNumber(1, 0, 0);
            commandInfos = new Dictionary<string, CommandInfo>();
        }

        /// <summary>
        /// Subscribes this plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        protected void Subscribe(string hook) => Manager.SubscribeToHook(hook, this);

        /// <summary>
        /// Unsubscribes this plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        protected void Unsubscribe(string hook) => Manager.UnsubscribeToHook(hook, this);

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public virtual void HandleAddedToManager(PluginManager manager)
        {
            Manager = manager;
            if (HasConfig) LoadConfig();
            if (HasMessages) LoadDefaultMessages();
            OnAddedToManager?.Invoke(this, manager);
            RegisterWithCovalence();
        }

        /// <summary>
        /// Called when this plugin has been removed from the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public virtual void HandleRemovedFromManager(PluginManager manager)
        {
            UnregisterWithCovalence();
            if (Manager == manager) Manager = null;
            OnRemovedFromManager?.Invoke(this, manager);
        }

        /// <summary>
        /// Called when this plugin is loading
        /// </summary>
        public virtual void Load()
        {
        }

        /// <summary>
        /// Calls a hook on this plugin
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hook, params object[] args)
        {
            var startedAt = 0f;
            if (!IsCorePlugin && nestcount == 0)
            {
                preHookGcCount = GC.CollectionCount(0);
                startedAt = Interface.Oxide.Now;
                stopwatch.Start();
                if (averageAt < 1) averageAt = startedAt;
            }
            TrackStart();
            nestcount++;
            try
            {
                return OnCallHook(hook, args);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to call hook '{hook}' on plugin '{Name} v{Version}'", ex);
                return null;
            }
            finally
            {
                nestcount--;
                TrackEnd();
                if (startedAt > 0)
                {
                    stopwatch.Stop();
                    var duration = stopwatch.Elapsed.TotalSeconds;
                    if (duration > 0.5)
                    {
                        var suffix = preHookGcCount == GC.CollectionCount(0) ? string.Empty : " [GARBAGE COLLECT]";
                        Interface.Oxide.LogWarning($"Calling '{hook}' on '{Name} v{Version}' took {duration * 1000:0}ms{suffix}");
                    }
                    stopwatch.Reset();
                    var total = sum + duration;
                    var endedAt = startedAt + duration;
                    if (endedAt - averageAt > 10)
                    {
                        total /= endedAt - averageAt;
                        if (total > 0.5)
                        {
                            var suffix = preHookGcCount == GC.CollectionCount(0) ? string.Empty : " [GARBAGE COLLECT]";
                            Interface.Oxide.LogWarning($"Calling '{hook}' on '{Name} v{Version}' took average {sum * 1000:0}ms{suffix}");
                        }
                        sum = 0;
                        averageAt = 0;
                    }
                    else
                    {
                        sum = total;
                    }
                }
            }
        }

        /// <summary>
        /// Calls a hook on this plugin
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object Call(string hook, params object[] args) => CallHook(hook, args);

        /// <summary>
        /// Calls a hook on this plugin and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Call<T>(string hook, params object[] args) => (T)Convert.ChangeType(CallHook(hook, args), typeof(T));

        /// <summary>
        /// Called when it's time to run a hook on this plugin
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract object OnCallHook(string hook, object[] args);

        /// <summary>
        /// Raises an error on this plugin
        /// </summary>
        /// <param name="message"></param>
        public void RaiseError(string message) => OnError?.Invoke(this, message);

        public void TrackStart()
        {
            if (IsCorePlugin || nestcount > 0) return;
            var stopwatch = trackStopwatch;
            if (stopwatch.IsRunning) return;
            stopwatch.Start();
        }

        public void TrackEnd()
        {
            if (IsCorePlugin || nestcount > 0) return;
            var stopwatch = trackStopwatch;
            if (!stopwatch.IsRunning) return;
            stopwatch.Stop();
            TotalHookTime += stopwatch.Elapsed.TotalSeconds;
            stopwatch.Reset();
        }

        #region Config

        /// <summary>
        /// Loads the config file for this plugin
        /// </summary>
        protected virtual void LoadConfig()
        {
            Config = new DynamicConfigFile(Path.Combine(Manager.ConfigPath, $"{Name}.json"));
            if (!Config.Exists())
            {
                LoadDefaultConfig();
                SaveConfig();
            }
            try
            {
                Config.Load();
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to load config file (is the config file corrupt?) ({ex.Message})");
            }
        }

        /// <summary>
        /// Populates the config with default settings
        /// </summary>
        protected virtual void LoadDefaultConfig() => CallHook("LoadDefaultConfig", null);

        /// <summary>
        /// Saves the config file for this plugin
        /// </summary>
        protected virtual void SaveConfig()
        {
            if (Config == null) return;
            try
            {
                Config.Save();
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to save config file (does the config have illegal objects in it?) ({ex.Message})");
            }
        }

        #endregion Config

        #region Lang

        /// <summary>
        /// Populates the lang file(s) with default messages
        /// </summary>
        protected virtual void LoadDefaultMessages() => CallHook("LoadDefaultMessages", null);

        #endregion Lang

        #region Covalence

        public void AddCovalenceCommand(string command, string callback, string perm = null)
        {
            AddCovalenceCommand(new[] { command }, callback, string.IsNullOrEmpty(perm) ? null : new[] { perm });
        }

        public void AddCovalenceCommand(string[] commands, string callback, string perm)
        {
            AddCovalenceCommand(commands, callback, string.IsNullOrEmpty(perm) ? null : new[] { perm });
        }

        public void AddCovalenceCommand(string[] commands, string callback, string[] perms = null)
        {
            AddCovalenceCommand(commands, perms, (caller, command, args) =>
            {
                CallHook(callback, caller, command, args);
                return true;
            });

            var covalence = Interface.Oxide.GetLibrary<Covalence>();
            foreach (var command in commands) covalence.RegisterCommand(command, this, CovalenceCommandCallback);
        }

        protected void AddCovalenceCommand(string[] commands, string[] perms, CommandCallback callback)
        {
            foreach (var cmdName in commands)
            {
                if (commandInfos.ContainsKey(cmdName.ToLowerInvariant()))
                {
                    Interface.Oxide.LogWarning("Covalence command alias already exists: {0}", cmdName);
                    continue;
                }
                commandInfos.Add(cmdName.ToLowerInvariant(), new CommandInfo(commands, perms, callback));
            }

            if (perms == null) return;

            foreach (var perm in perms)
            {
                if (permission.PermissionExists(perm)) continue;
                permission.RegisterPermission(perm, this);
            }
        }

        private void RegisterWithCovalence()
        {
            var covalence = Interface.Oxide.GetLibrary<Covalence>();
            foreach (var pair in commandInfos) covalence.RegisterCommand(pair.Key, this, CovalenceCommandCallback);
        }

        private bool CovalenceCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            CommandInfo cmdInfo;
            if (!commandInfos.TryGetValue(cmd, out cmdInfo)) return false;

            if (caller == null)
            {
                Interface.Oxide.LogWarning("Plugin.CovalenceCommandCallback received null as the caller (bad game Covalence bindings?)");
                return false;
            }

            if (cmdInfo.PermissionsRequired != null && !caller.IsAdmin)
            {
                foreach (var perm in cmdInfo.PermissionsRequired)
                {
                    if (caller.HasPermission(perm)) continue;

                    caller.Message($"You don't have permission to use the command '{cmd}'!"); // TODO: Use Lang API for this message
                    return true;
                }
            }

            cmdInfo.Callback(caller, cmd, args);
            return true;
        }

        private void UnregisterWithCovalence()
        {
            var covalence = Interface.Oxide.GetLibrary<Covalence>();
            foreach (var pair in commandInfos) covalence.UnregisterCommand(pair.Key, this);
        }

        #endregion Covalence
    }
}
