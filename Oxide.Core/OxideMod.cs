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

        /// <summary>
        /// Gets the main pluginmanager
        /// </summary>
        public PluginManager RootPluginManager { get { return pluginmanager; } }

        // The extension manager
        private ExtensionManager extensionmanager;

        // The command line
        private CommandLine commandline;

        // Various directories
        public string RootDirectory { get; private set; }
        public string ExtensionDirectory { get; private set; }
        public string InstanceDirectory { get; private set; }
        public string PluginDirectory { get; private set; }
        public string ConfigDirectory { get; private set; }
        public string DataDirectory { get; private set; }
        public string LogDirectory { get; private set; }
        public string TempDirectory { get; private set; }

        // Various configs
        private OxideConfig rootconfig;

        // Various libraries
        private Timer libtimer;
        private WebRequests libwebrequests;

        // Thread safe NextTick callback queue
        private List<Action> nextTickQueue = new List<Action>();
        private object nextTickLock = new object();

        // Allow extensions to register a method to be called every frame
        private Action onFrame;
        private bool isInitialized = false;

        /// <summary>
        /// Gets the data file system
        /// </summary>
        public DataFileSystem DataFileSystem { get; private set; }

        public bool IsShuttingDown { get; private set; }

        /// <summary>
        /// The current Oxide version
        /// </summary>
        public static readonly VersionNumber Version = new VersionNumber(2, 0, 0);

        /// <summary>
        /// Initializes a new instance of the OxideMod class
        /// </summary>
        public void Load()
        {
            RootDirectory = Environment.CurrentDirectory;

            // Create the commandline
            commandline = new CommandLine(Environment.CommandLine);

            // Load the config
            var oxideConfig = Path.Combine(RootDirectory, "oxide.root.json");
            if (!File.Exists(oxideConfig)) throw new FileNotFoundException("Could not load Oxide root configuration", oxideConfig);
            rootconfig = ConfigFile.Load<OxideConfig>(oxideConfig);

            // Work out the instance directory
            for (int i = 0; i < rootconfig.InstanceCommandLines.Length; i++)
            {
                string varname, format;
                rootconfig.GetInstanceCommandLineArg(i, out varname, out format);
                if (string.IsNullOrEmpty(varname) || commandline.HasVariable(varname))
                {
                    InstanceDirectory = Path.Combine(RootDirectory, string.Format(format, commandline.GetVariable(varname)));
                    break;
                }
            }
            if (InstanceDirectory == null) throw new Exception("Could not identify instance directory");
            ExtensionDirectory = Path.Combine(RootDirectory, rootconfig.ExtensionDirectory);
            PluginDirectory = Path.Combine(InstanceDirectory, rootconfig.PluginDirectory);
            DataDirectory = Path.Combine(InstanceDirectory, rootconfig.DataDirectory);
            LogDirectory = Path.Combine(InstanceDirectory, rootconfig.LogDirectory);
            ConfigDirectory = Path.Combine(InstanceDirectory, rootconfig.ConfigDirectory);
            TempDirectory = Path.Combine(InstanceDirectory, rootconfig.TempDirectory);
            if (!Directory.Exists(ExtensionDirectory)) throw new Exception("Could not identify extension directory");
            if (!Directory.Exists(InstanceDirectory)) Directory.CreateDirectory(InstanceDirectory);
            if (!Directory.Exists(PluginDirectory)) Directory.CreateDirectory(PluginDirectory);
            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
            if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
            if (!Directory.Exists(TempDirectory)) Directory.CreateDirectory(TempDirectory);

            // Create the loggers
            filelogger = new RotatingFileLogger();
            filelogger.Directory = LogDirectory;
            rootlogger = new CompoundLogger();
            rootlogger.AddLogger(filelogger);

            // Log Oxide core loading
            rootlogger.Write(LogType.Info, "Loading Oxide core v{0}...", Version);

            // Create the managers
            pluginmanager = new PluginManager(rootlogger) { ConfigPath = ConfigDirectory };
            extensionmanager = new ExtensionManager(rootlogger);

            // Register core libraries
            extensionmanager.RegisterLibrary("Global", new Global());
            extensionmanager.RegisterLibrary("Timer", libtimer = new Timer());
            extensionmanager.RegisterLibrary("Time", new Time());
            extensionmanager.RegisterLibrary("Permission", new Permission());
            extensionmanager.RegisterLibrary("Plugins", new Libraries.Plugins(pluginmanager));
            extensionmanager.RegisterLibrary("WebRequests", libwebrequests = new WebRequests());

            // Initialize other things
            DataFileSystem = new DataFileSystem(DataDirectory);

            // Load all extensions
            rootlogger.Write(LogType.Info, "Loading extensions...");
            extensionmanager.LoadAllExtensions(ExtensionDirectory);

            // Load all watchers
            foreach (Extension ext in extensionmanager.GetAllExtensions())
                ext.LoadPluginWatchers(PluginDirectory);

            // Load all plugins
            rootlogger.Write(LogType.Info, "Loading plugins...");
            LoadAllPlugins();

            // Hook all watchers
            foreach (PluginChangeWatcher watcher in extensionmanager.GetPluginChangeWatchers())
            {
                watcher.OnPluginSourceChanged += watcher_OnPluginSourceChanged;
                watcher.OnPluginAdded += watcher_OnPluginAdded;
                watcher.OnPluginRemoved += watcher_OnPluginRemoved;
            }
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

        /// <summary>
        /// Logs a formatted message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogInfo(string format, params object[] args)
        {
            rootlogger.Write(LogType.Info, format, args);
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
                foreach (string name in loader.ScanDirectory(PluginDirectory))
                {
                    // Check if the plugin is already loaded
                    if (pluginmanager.GetPlugin(name) == null)
                    {
                        // Load it and watch for errors
                        try
                        {
                            Plugin plugin = loader.Load(PluginDirectory, name);
                            if (plugin == null) continue; // Async load
                            plugin.OnError += plugin_OnError;
                            rootlogger.Write(LogType.Info, "Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
                            plugins.Add(plugin);
                        }
                        catch (Exception ex)
                        {
                            rootlogger.WriteException(string.Format("Failed to load plugin {0}", name), ex);
                        }
                    }
                }

            foreach (PluginLoader loader in extensionmanager.GetPluginLoaders())
            {
                var loading_plugin_count = loader.LoadingPlugins.Count;
                if (loading_plugin_count < 1) continue;
                // Wait until all async plugins have finished loading
                while (loader.LoadingPlugins.Count > 0)
                {
                    System.Threading.Thread.Sleep(25);
                    // Process any NextTick callbacks which other threads may have queued
                    OnFrame();
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
                    rootlogger.WriteException(string.Format("Failed to initialize plugin {0}", plugin.Name), ex);
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// Unloads all plugins
        /// </summary>
        public void UnloadAllPlugins()
        {
            //TODO: Find a way to differentiate core plugins from reloadable ones
            foreach (var plugin in pluginmanager.GetPlugins().ToArray())
                UnloadPlugin(plugin.Name);
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
            HashSet<PluginLoader> loaders = new HashSet<PluginLoader>(extensionmanager.GetPluginLoaders().Where((l) => l.ScanDirectory(PluginDirectory).Contains(name)));
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
                plugin = loader.Load(PluginDirectory, name);
                if (plugin == null) return; // async load
                PluginLoaded(plugin);
            }
            catch (Exception ex)
            {
                rootlogger.WriteException(string.Format("Failed to load plugin {0}:", name), ex);
                return;
            }
        }

        public bool PluginLoaded(Plugin plugin)
        {
            plugin.OnError += plugin_OnError;
            // Log plugin loaded
            rootlogger.Write(LogType.Info, "Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            try
            {
                pluginmanager.AddPlugin(plugin);
                CallHook("OnPluginLoaded", new object[] { plugin });
                return true;
            }
            catch (Exception ex)
            {
                rootlogger.WriteException(string.Format("Failed to initialize plugin {0}", plugin.Name), ex);
                return false;
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

            // Let the plugin loader know that this plugin is being unloaded
            var loader = extensionmanager.GetPluginLoaders().SingleOrDefault(l => l.ScanDirectory(PluginDirectory).Contains(name));
            if (loader != null) loader.Unloading(plugin);

            // Unload it
            pluginmanager.RemovePlugin(plugin);

            // Let other plugins know that this plugin has been unloaded
            CallHook("OnPluginUnloaded", new object[] { plugin });

            rootlogger.Write(LogType.Info, "Unloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            return true;
        }

        /// <summary>
        /// Reloads the plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool ReloadPlugin(string name)
        {
            var loader = extensionmanager.GetPluginLoaders().SingleOrDefault(l => l.ScanDirectory(PluginDirectory).Contains(name));
            if (loader != null)
            {
                loader.Reload(PluginDirectory, name);
                return true;
            }
            UnloadPlugin(name);
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
            // Forward the call to the plugin manager
            return pluginmanager.CallHook(hookname, args);
        }

        /// <summary>
        /// Queue a callback to be called in the next server frame
        /// </summary>
        /// <param name="callback"></param>
        public void NextTick(Action callback)
        {
            lock (nextTickLock) nextTickQueue.Add(callback);
        }

        /// <summary>
        /// Register a callback which will be called every server frame
        /// </summary>
        /// <param name="callback"></param>
        public void OnFrame(Action callback)
        {
            onFrame += callback;
        }

        /// <summary>
        /// Called every server frame, implemented by an engine-specific extension
        /// </summary>
        public void OnFrame()
        {
            // Call any callbacks queued for this frame
            if (nextTickQueue.Count > 0)
                lock (nextTickLock)
                {
                    for (var i = 0; i < nextTickQueue.Count; i++)
                    {
                        try
                        {
                            nextTickQueue[i]();
                        }
                        catch (Exception ex)
                        {
                            rootlogger.Write(LogType.Error, "Exception while calling NextTick callback: {0}", ex.ToString());
                        }
                    }
                    nextTickQueue.Clear();
                }

            // Update libraries
            libtimer.Update();
            libwebrequests.Update();

            // Don't update plugin watchers or call OnFrame in plugins until servers starts ticking
            if (!isInitialized) return;

            // Update plugin change watchers
            UpdatePluginWatchers();

            // Update extensions
            onFrame();
        }

        public void OnShutdown()
        {
            IsShuttingDown = true;
            UnloadAllPlugins();
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
        /// <param name="name"></param>
        private void watcher_OnPluginSourceChanged(string name)
        {
            // Reload the plugin
            ReloadPlugin(name);
        }

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginAdded(string name)
        {
            // Load the plugin
            LoadPlugin(name);
        }

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginRemoved(string name)
        {
            // Unload the plugin
            UnloadPlugin(name);
        }

        #endregion
    }
}
