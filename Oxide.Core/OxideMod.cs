using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;

namespace Oxide.Core
{
    /// <summary>
    /// Responsible for core Oxide logic
    /// </summary>
    public sealed class OxideMod
    {
        // The loggers used to... log things
        private CompoundLogger rootlogger;
        private RotatingFileLogger filelogger;

        /// <summary>
        /// Gets the main logger
        /// </summary>
        public CompoundLogger RootLogger { get { return rootlogger; } }

        // The plugin manager
        private PluginManager pluginmanager;

        // The extension manager
        private ExtensionManager extensionmanager;

        // The command line
        private CommandLine commandline;

        // Various directories
        private string extdir, instancedir, plugindir, datadir, logdir, configdir;

        // Various configs
        private OxideConfig rootconfig;

        /// <summary>
        /// The current Oxide version
        /// </summary>
        public static readonly VersionNumber Version = new VersionNumber(2, 0, 0);

        /// <summary>
        /// Initialises a new instance of the OxideMod class
        /// </summary>
        public void Load()
        {
            // Create the commandline
            commandline = new CommandLine(Environment.CommandLine);

            // Load the config
            if (!File.Exists("oxide.root.json")) throw new FileNotFoundException("Could not load Oxide root configuration", "oxide.root.json");
            rootconfig = ConfigFile.Load<OxideConfig>("oxide.root.json");

            // Work out the instance directory
            for (int i = 0; i < rootconfig.InstanceCommandLines.Length; i++)
            {
                string varname, format;
                rootconfig.GetInstanceCommandLineArg(i, out varname, out format);
                if (string.IsNullOrEmpty(varname) || commandline.HasVariable(varname))
                {
                    instancedir = Path.Combine(Environment.CurrentDirectory, string.Format(format, commandline.GetVariable(varname)));
                    break;
                }
            }
            if (instancedir == null) throw new Exception("Could not identify instance directory");
            extdir = Path.Combine(Environment.CurrentDirectory, rootconfig.ExtensionDirectory);
            plugindir = Path.Combine(instancedir, rootconfig.PluginDirectory);
            datadir = Path.Combine(instancedir, rootconfig.DataDirectory);
            logdir = Path.Combine(instancedir, rootconfig.LogDirectory);
            configdir = Path.Combine(instancedir, rootconfig.ConfigDirectory);
            if (!Directory.Exists(extdir)) throw new Exception("Could not identify extension directory");
            if (!Directory.Exists(instancedir)) Directory.CreateDirectory(instancedir);
            if (!Directory.Exists(plugindir)) Directory.CreateDirectory(plugindir);
            if (!Directory.Exists(datadir)) Directory.CreateDirectory(datadir);
            if (!Directory.Exists(logdir)) Directory.CreateDirectory(logdir);
            if (!Directory.Exists(configdir)) Directory.CreateDirectory(configdir);

            // Create the loggers
            filelogger = new RotatingFileLogger();
            filelogger.Directory = logdir;
            rootlogger = new CompoundLogger();
            rootlogger.AddLogger(filelogger);
            rootlogger.Write(LogType.Info, "Loading Oxide core...");

            // Create the managers
            pluginmanager = new PluginManager(rootlogger);
            pluginmanager.ConfigPath = configdir;
            extensionmanager = new ExtensionManager(rootlogger);

            // Register core libraries
            extensionmanager.RegisterLibrary("Global", new Global());

            // Load all extensions
            rootlogger.Write(LogType.Info, "Loading extensions...");
            extensionmanager.LoadAllExtensions(extdir);

            // Load all watchers
            foreach (Extension ext in extensionmanager.GetAllExtensions())
                ext.LoadPluginWatchers(plugindir);

            // Load all plugins
            rootlogger.Write(LogType.Info, "Loading plugins...");
            LoadAllPlugins();

            // Hook all watchers
            foreach (PluginChangeWatcher watcher in extensionmanager.GetPluginChangeWatchers())
                watcher.OnPluginSourceChanged += watcher_OnPluginSourceChanged;
        }

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetLibrary<T>(string name) where T : Library
        {
            return extensionmanager.GetLibrary(name) as T;
        }

        #region Plugin Management

        /// <summary>
        /// Scans for all available plugins and attempts to load them
        /// </summary>
        public void LoadAllPlugins()
        {
            // Get all plugin loaders, scan the plugin directory and load all reported plugins
            HashSet<Plugin> plugins = new HashSet<Plugin>();
            foreach (PluginLoader loader in extensionmanager.GetPluginLoaders())
                foreach (string name in loader.ScanDirectory(plugindir))
                {
                    // Check if the plugin is already loaded
                    if (pluginmanager.GetPlugin(name) == null)
                    {
                        // Load it and watch for errors
                        try
                        {
                            Plugin plugin = loader.Load(plugindir, name);
                            plugin.OnError += plugin_OnError;
                            rootlogger.Write(LogType.Info, "Loaded plugin {0} (v{1}) by {2}", plugin.Title, plugin.Version, plugin.Author);
                            plugins.Add(plugin);
                        }
                        catch (Exception ex)
                        {
                            //rootlogger.Write(LogType.Error, "Failed to load plugin {0} ({1})", name, ex.Message);
                            //rootlogger.Write(LogType.Debug, ex.StackTrace);
                            rootlogger.WriteException(string.Format("Failed to load plugin {0}", name), ex);
                        }
                    }
                }

            // Init all successfully loaded plugins
            foreach (Plugin plugin in plugins)
            {
                try
                {
                    pluginmanager.AddPlugin(plugin);
                }
                catch (Exception ex)
                {
                    //rootlogger.Write(LogType.Error, "Failed to initialise plugin {0} ({1})", plugin.Name, ex.Message);
                    //rootlogger.Write(LogType.Debug, ex.StackTrace);
                    rootlogger.WriteException(string.Format("Failed to initialise plugin {0}", plugin.Name), ex);
                }
            }
        }

        /// <summary>
        /// Unloads all plugins
        /// </summary>
        public void UnloadAllPlugins()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads a plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public void LoadPlugin(string name)
        {
            // Check if the plugin is already loaded
            if (pluginmanager.GetPlugin(name) != null) return;

            // Find all plugin loaders that lay claim to the name
            HashSet<PluginLoader> loaders = new HashSet<PluginLoader>(extensionmanager.GetPluginLoaders().Where((l) => l.ScanDirectory(plugindir).Contains(name)));
            if (loaders.Count == 0)
            {
                rootlogger.Write(LogType.Error, "Failed to load plugin '{0}' (no source found)", name);
                return;
            }
            if (loaders.Count > 1)
            {
                rootlogger.Write(LogType.Error, "Failed to load plugin '{0}' (multiple sources found)", name);
                return;
            }
            PluginLoader loader = loaders.First();

            // Load it and watch for errors
            Plugin plugin;
            try
            {
                plugin = loader.Load(plugindir, name);
                plugin.OnError += plugin_OnError;
                rootlogger.Write(LogType.Info, "Loaded plugin {0} (v{1}) by {2}", plugin.Title, plugin.Version, plugin.Author);
            }
            catch (Exception ex)
            {
                rootlogger.Write(LogType.Error, "Failed to load plugin {0} ({1})", name, ex.Message);
                rootlogger.Write(LogType.Debug, ex.StackTrace);
                return;
            }

            // Initialise it
            try
            {
                pluginmanager.AddPlugin(plugin);
            }
            catch (Exception ex)
            {
                rootlogger.Write(LogType.Error, "Failed to initialise plugin {0} ({1})", plugin.Name, ex.Message);
                rootlogger.Write(LogType.Debug, ex.StackTrace);
            }
        }

        /// <summary>
        /// Unloads the plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool UnloadPlugin(string name)
        {
            // Get the plugin
            Plugin plugin = pluginmanager.GetPlugin(name);
            if (plugin == null) return false;

            // Unload it
            pluginmanager.RemovePlugin(plugin);
            return true;
        }

        /// <summary>
        /// Reloads the plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool ReloadPlugin(string name)
        {
            if (!UnloadPlugin(name)) return false;
            LoadPlugin(name);
            return true;
        }

        /// <summary>
        /// Called when a plugin has raised an error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void plugin_OnError(Plugin sender, string message)
        {
            rootlogger.Write(LogType.Error, "{0}: {1}", sender.Name, message);
        }

        #endregion

        /// <summary>
        /// Calls a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, object[] args)
        {
            // Check for special hooks
            switch (hookname)
            {
                case "OnTick": // Called every tick
                    // Update plugin change watchers
                    UpdatePluginWatchers();

                    // Forward the call to the plugin manager
                    break;
            }

            // Forward the call to the plugin manager
            object retval = pluginmanager.CallHook(hookname, args);
            return retval;
        }

        #region Plugin Change Watchers

        /// <summary>
        /// Updates all plugin change watchers
        /// </summary>
        private void UpdatePluginWatchers()
        {
            foreach (PluginChangeWatcher watcher in extensionmanager.GetPluginChangeWatchers())
                watcher.UpdateChangeStatus();
        }

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="plugin"></param>
        private void watcher_OnPluginSourceChanged(Plugin plugin)
        {
            // Reload the plugin
            ReloadPlugin(plugin.Name);
        }

        #endregion
    }
}
