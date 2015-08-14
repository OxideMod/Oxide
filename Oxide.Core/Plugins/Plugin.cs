using System;
using System.IO;
using System.Collections.Generic;

using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Plugins
{
    public delegate void PluginError(Plugin sender, string message);

    public delegate void PluginManagerEvent(Plugin sender, PluginManager manager);

    /// <summary>
    /// Represents a single plugin
    /// </summary>
    public abstract class Plugin
    {
        public static implicit operator bool(Plugin plugin)
        {
            return plugin != null;
        }

        public static bool operator !(Plugin plugin)
        {
            return !(bool)plugin;
        }

        /// <summary>
        /// Gets the source file name, if any
        /// </summary>
        public string Filename { get; protected set; }

        /// <summary>
        /// Gets the internal name of this plugin
        /// </summary>
        public string Name { get; protected set; }

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
        /// Gets if this plugin should never be unloaded
        /// </summary>
        public bool IsCorePlugin { get; set; }

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
        /// Called when this plugin was added/removed from a manager
        /// </summary>
        public event PluginManagerEvent OnAddedToManager, OnRemovedFromManager;

        /// <summary>
        /// Has this plugins Init/Loaded hook been called
        /// </summary>
        public bool IsLoaded { get; internal set; }

        // Used to measure time spent in this plugin
        private float startedAt;
        private float stoppedAt;
        private float averageAt;
        private float sum;

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

        /// <summary>
        /// Initializes an empty version of the Plugin class
        /// </summary>
        protected Plugin()
        {
            Name = "baseplugin";
            Title = "Base Plugin";
            Author = "System";
            Version = new VersionNumber(1, 0, 0);
            commandInfos = new Dictionary<string, CommandInfo>();
        }

        /// <summary>
        /// Subscribes this plugin to the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        protected void Subscribe(string hookname)
        {
            Manager.SubscribeToHook(hookname, this);
        }

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public virtual void HandleAddedToManager(PluginManager manager)
        {
            Manager = manager;
            if (HasConfig) LoadConfig();
            if (OnAddedToManager != null) OnAddedToManager(this, manager);
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
            if (OnRemovedFromManager != null) OnRemovedFromManager(this, manager);
        }

        /// <summary>
        /// Calls a hook on this plugin
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, params object[] args)
        {
            if (!IsCorePlugin && nestcount == 0)
            {
                startedAt = Interface.Oxide.Now;
                if (averageAt < 1) averageAt = startedAt;
            }
            nestcount++;
            try
            {
                return OnCallHook(hookname, args);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to call hook '{hookname}' on plugin '{Name} v{Version}'", ex);
                return null;
            }
            finally
            {
                nestcount--;
                if (!IsCorePlugin && nestcount == 0)
                {
                    stoppedAt = Interface.Oxide.Now;
                    if (stoppedAt - startedAt > 0.5)
                        Interface.Oxide.LogWarning($"CallHook '{hookname}' on plugin '{Name} v{Version}' took: {(stoppedAt - startedAt)*1000:0}ms");
                    sum += stoppedAt - startedAt;
                    if (stoppedAt - averageAt > 10)
                    {
                        sum /= stoppedAt - averageAt;
                        if (sum > 0.25)
                            Interface.Oxide.LogWarning($"CallHook '{hookname}' on plugin '{Name} v{Version}' took average: {sum*1000:0}ms");
                        sum = 0;
                        averageAt = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Calls a hook on this plugin
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object Call(string hookname, params object[] args)
        {
            return CallHook(hookname, args);
        }

        /// <summary>
        /// Calls a hook on this plugin and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Call<T>(string hookname, params object[] args) {
            return (T)Convert.ChangeType(CallHook(hookname, args), typeof(T));
        }

        /// <summary>
        /// Called when it's time to run a hook on this plugin
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract object OnCallHook(string hookname, object[] args);

        /// <summary>
        /// Raises an error on this plugin
        /// </summary>
        /// <param name="message"></param>
        public void RaiseError(string message)
        {
            if (OnError != null)
                OnError(this, message);
        }

        #region Config

        /// <summary>
        /// Loads the config file for this plugin
        /// </summary>
        protected virtual void LoadConfig()
        {
            Config = new DynamicConfigFile(Path.Combine(Manager.ConfigPath, string.Format("{0}.json", Name)));
            if (Config.Exists())
            {
                try
                {
                    Config.Load();
                }
                catch (Exception ex)
                {
                    RaiseError(string.Format("Failed to load config file (is the config file corrupt?) ({0})", ex.Message));
                }
            }
            else
            {
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        /// <summary>
        /// Populates the config with default settings
        /// </summary>
        protected virtual void LoadDefaultConfig()
        {
            // Call LoadDefaultConfig if it was defined as a private hook
            CallHook("LoadDefaultConfig", null);
        }

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
                RaiseError(string.Format("Failed to save config file (does the config have illegal objects in it?) ({0})", ex.Message));
            }
        }

        #endregion

        #region Covalence

        protected void AddCovalenceCommand(string[] commands, string[] perms, CommandCallback callback)
        {
            foreach (var cmdName in commands)
            {
                if (commandInfos.ContainsKey(cmdName))
                {
                    Interface.Oxide.LogWarning("Plugin.AddCovalenceCommand command alias already exists: {0}", cmdName);
                    continue;
                }
                commandInfos.Add(cmdName, new CommandInfo(commands, perms, callback));
            }
        }

        private void RegisterWithCovalence()
        {
            var covalence = Interface.GetMod().GetLibrary<Covalence>();
            foreach (var pair in commandInfos)
            {
                covalence.RegisterCommand(pair.Key, CovalenceCommandCallback);
            }
        }

        private bool CovalenceCommandCallback(string cmd, CommandType type, IPlayer caller, string[] args)
        {
            // Get the command
            CommandInfo cmdInfo;
            if (!commandInfos.TryGetValue(cmd, out cmdInfo)) return false;

            // Check for permissions
            if (caller == null)
            {
                Interface.Oxide.LogWarning("Plugin.CovalenceCommandCallback received null as the caller (bad game Covalence bindings?)");
                return false;
            }
            foreach (var perm in cmdInfo.PermissionsRequired)
            {
                if (!caller.HasPermission(perm))
                {
                    caller.ConnectedPlayer?.SendChatMessage(string.Format("Missing permission '{0}' to run command '{1}'!", perm, cmd));
                    return true;
                }
            }

            // Call it
            cmdInfo.Callback(cmd, type, caller, args);

            // Handled
            return true;
        }

        private void UnregisterWithCovalence()
        {
            var covalence = Interface.GetMod().GetLibrary<Covalence>();
            foreach (var pair in commandInfos)
            {
                covalence.UnregisterCommand(pair.Key);
            }
        }

        #endregion
    }
}
