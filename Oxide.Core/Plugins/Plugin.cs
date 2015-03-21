using System;
using System.Diagnostics;
using System.IO;

using Oxide.Core.Configuration;

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
        /// Gets the internal name of this plugin
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the user-friendly title of this plugin
        /// </summary>
        public string Title { get; protected set; }

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
        /// Gets the object associated with this plugin
        /// </summary>
        public virtual object Object { get { return this; } }

        /// <summary>
        /// Gets the total CPU time spent in this plugin in seconds
        /// </summary>
        public float TimeSpent
        {
            get
            {
                return (float)stopwatch.Elapsed.TotalSeconds;
            }
        }

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

        // Used to measure time spent in this plugin
        private readonly Stopwatch stopwatch;

        // The depth of hook call nesting
        protected int nestcount;

        /// <summary>
        /// Initializes an empty version of the Plugin class
        /// </summary>
        protected Plugin()
        {
            Name = "baseplugin";
            Title = "Base Plugin";
            Author = "System";
            Version = new VersionNumber(1, 0, 0);
            stopwatch = new Stopwatch();
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
        }

        /// <summary>
        /// Called when this plugin has been removed from the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public virtual void HandleRemovedFromManager(PluginManager manager)
        {
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
            if (nestcount == 0) stopwatch.Start();
            nestcount++;
            try
            {
                return OnCallHook(hookname, args);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException(string.Format("Failed to call hook '{0}' on plugin '{1}'", hookname, Title), ex);
                return null;
            }
            finally
            {
                nestcount--;
                if (nestcount == 0) stopwatch.Stop();
            }
        }

        public object Call(string hookname, params object[] args)
        {
            return CallHook(hookname, args);
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
            string configpath = Path.Combine(Manager.ConfigPath, string.Format("{0}.json", Name));
            Config = new DynamicConfigFile();
            if (File.Exists(configpath))
            {
                try
                {
                    Config.Load(configpath);
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
            string configpath = Path.Combine(Manager.ConfigPath, string.Format("{0}.json", Name));
            try
            {
                Config.Save(configpath);
            }
            catch (Exception ex)
            {
                RaiseError(string.Format("Failed to save config file (does the config have illegal objects in it?) ({0})", ex.Message));
            }
        }

        #endregion
    }
}
