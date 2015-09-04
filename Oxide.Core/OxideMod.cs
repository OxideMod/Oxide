using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.ServerConsole;

using Timer = Oxide.Core.Libraries.Timer;

namespace Oxide.Core
{
    /// <summary>
    /// Responsible for core Oxide logic
    /// </summary>
    public sealed class OxideMod
    {
        /// <summary>
        /// The current Oxide version
        /// </summary>
        public static readonly VersionNumber Version = new VersionNumber(2, 0, 0);

        /// <summary>
        /// Gets the main logger
        /// </summary>
        public CompoundLogger RootLogger { get; private set; }

        /// <summary>
        /// Gets the main pluginmanager
        /// </summary>
        public PluginManager RootPluginManager { get; private set; }

        /// <summary>
        /// Gets the data file system
        /// </summary>
        public DataFileSystem DataFileSystem { get; private set; }

        // Various directories
        public string RootDirectory { get; private set; }
        public string ExtensionDirectory { get; private set; }
        public string InstanceDirectory { get; private set; }
        public string PluginDirectory { get; private set; }
        public string ConfigDirectory { get; private set; }
        public string DataDirectory { get; private set; }
        public string LogDirectory { get; private set; }

        // Gets the number of seconds since the server started
        public float Now => getTimeSinceStartup();

        /// <summary>
        /// This is true if the server is shutting down
        /// </summary>
        public bool IsShuttingDown { get; private set; }

        // The rotating file logger
        private RotatingFileLogger filelogger;

        // The extension manager
        private ExtensionManager extensionmanager;

        // The command line
        private CommandLine commandline;

        // Various configs
        private OxideConfig rootconfig;

        // Various libraries
        private Timer libtimer;
        private WebRequests libwebrequests;
        private Covalence covalence;

        // Extension implemented delegates
        private Func<float> getTimeSinceStartup;

        // Thread safe NextTick callback queue
        private List<Action> nextTickQueue = new List<Action>();
        private object nextTickLock = new object();

        // Allow extensions to register a method to be called every frame
        private Action<float> onFrame;
        private bool isInitialized;
        private bool hasLoadedCorePlugins;

        public ServerConsole.ServerConsole ServerConsole;

        private Stopwatch timer;

        /// <summary>
        /// Initializes a new instance of the OxideMod class
        /// </summary>
        public void Load()
        {
            RootDirectory = Environment.CurrentDirectory;
            if (RootDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
                RootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Create the commandline
            commandline = new CommandLine(Environment.GetCommandLineArgs());

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
                    InstanceDirectory = Path.Combine(RootDirectory, CleanPath(string.Format(format, commandline.GetVariable(varname))));
                    break;
                }
            }
            if (InstanceDirectory == null) throw new Exception("Could not identify instance directory");
            ExtensionDirectory = Path.Combine(RootDirectory, CleanPath(rootconfig.ExtensionDirectory));
            PluginDirectory = Path.Combine(InstanceDirectory, CleanPath(rootconfig.PluginDirectory));
            DataDirectory = Path.Combine(InstanceDirectory, CleanPath(rootconfig.DataDirectory));
            LogDirectory = Path.Combine(InstanceDirectory, CleanPath(rootconfig.LogDirectory));
            ConfigDirectory = Path.Combine(InstanceDirectory, CleanPath(rootconfig.ConfigDirectory));
            if (!Directory.Exists(ExtensionDirectory)) throw new Exception("Could not identify extension directory");
            if (!Directory.Exists(InstanceDirectory)) Directory.CreateDirectory(InstanceDirectory);
            if (!Directory.Exists(PluginDirectory)) Directory.CreateDirectory(PluginDirectory);
            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
            if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);

            RegisterLibrarySearchPath(Path.Combine(ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86"));

            // Create the loggers
            filelogger = new RotatingFileLogger();
            filelogger.Directory = LogDirectory;
            RootLogger = new CompoundLogger();
            RootLogger.AddLogger(filelogger);

            // Log Oxide core loading
            LogInfo("Loading Oxide core v{0}...", Version);

            // Create the managers
            RootPluginManager = new PluginManager(RootLogger) { ConfigPath = ConfigDirectory };
            extensionmanager = new ExtensionManager(RootLogger);

            // Initialize other things
            DataFileSystem = new DataFileSystem(DataDirectory);

            // Register core libraries
            extensionmanager.RegisterLibrary("Global", new Global());
            extensionmanager.RegisterLibrary("Time", new Time());
            extensionmanager.RegisterLibrary("Timer", libtimer = new Timer());
            extensionmanager.RegisterLibrary("Permission", new Permission());
            extensionmanager.RegisterLibrary("Plugins", new Libraries.Plugins(RootPluginManager));
            extensionmanager.RegisterLibrary("WebRequests", libwebrequests = new WebRequests());
            extensionmanager.RegisterLibrary("Covalence", covalence = new Covalence());

            // Load all extensions
            LogInfo("Loading extensions...");
            extensionmanager.LoadAllExtensions(ExtensionDirectory);

            // Initialize covalence library after extensions (as it depends on things from within an ext)
            covalence.Initialize();

            // If no clock has been defined, make our own
            if (getTimeSinceStartup == null)
            {
                timer = new Stopwatch();
                timer.Start();
                getTimeSinceStartup = () => (float)timer.Elapsed.TotalSeconds;
            }

            // Load all watchers
            foreach (var ext in extensionmanager.GetAllExtensions())
                ext.LoadPluginWatchers(PluginDirectory);

            // Load all plugins
            LogInfo("Loading plugins...");
            LoadAllPlugins();

            // Hook all watchers
            foreach (var watcher in extensionmanager.GetPluginChangeWatchers())
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
        public T GetLibrary<T>(string name = null) where T : Library
        {
            return extensionmanager.GetLibrary(name ?? typeof(T).Name) as T;
        }

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions()
        {
            return extensionmanager.GetAllExtensions();
        }

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders()
        {
            return extensionmanager.GetPluginLoaders();
        }

        /// <summary>
        /// Logs a formatted info message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogInfo(string format, params object[] args)
        {
            RootLogger.Write(LogType.Info, format, args);
        }

        /// <summary>
        /// Logs a formatted debug message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogDebug(string format, params object[] args)
        {
            RootLogger.Write(LogType.Debug, format, args);
        }

        /// <summary>
        /// Logs a formatted warning message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogWarning(string format, params object[] args)
        {
            RootLogger.Write(LogType.Warning, format, args);
        }

        /// <summary>
        /// Logs a formatted error message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogError(string format, params object[] args)
        {
            RootLogger.Write(LogType.Error, format, args);
        }

        /// <summary>
        /// Logs an exception to the root logger
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public void LogException(string message, Exception ex)
        {
            RootLogger.WriteException(message, ex);
        }

        #region Plugin Management

        /// <summary>
        /// Scans for all available plugins and attempts to load them
        /// </summary>
        public void LoadAllPlugins()
        {
            var loaders = extensionmanager.GetPluginLoaders();
            // Load all core plugins first
            if (!hasLoadedCorePlugins)
            {
                hasLoadedCorePlugins = true;
                foreach (var loader in loaders)
                {
                    foreach (var type in loader.CorePlugins)
                    {
                        try
                        {
                            var plugin = (Plugin)Activator.CreateInstance(type);
                            plugin.IsCorePlugin = true;
                            PluginLoaded(plugin);
                        }
                        catch (Exception ex)
                        {
                            LogException(string.Format("Failed to load core plugin {0}", type.Name), ex);
                        }
                    }
                }
            }
            // Scan the plugin directory and load all reported plugins
            foreach (var loader in loaders)
            {
                foreach (string name in loader.ScanDirectory(PluginDirectory))
                {
                    LoadPlugin(name);
                }
            }
            var lastCall = Now;
            foreach (var loader in extensionmanager.GetPluginLoaders())
            {
                // Wait until all async plugins have finished loading
                while (loader.LoadingPlugins.Count > 0)
                {
                    Thread.Sleep(25);
                    OnFrame(Now - lastCall);
                    lastCall = Now;
                }
            }
            isInitialized = true;
        }

        /// <summary>
        /// Unloads all plugins
        /// </summary>
        public void UnloadAllPlugins(IList<string> skip = null)
        {
            foreach (var plugin in RootPluginManager.GetPlugins().Where(p => !p.IsCorePlugin && (skip == null || !skip.Contains(p.Name))).ToArray())
            {
                UnloadPlugin(plugin.Name);
            }
        }

        /// <summary>
        /// Reloads all plugins
        /// </summary>
        public void ReloadAllPlugins(IList<string> skip = null)
        {
            foreach (var plugin in RootPluginManager.GetPlugins().Where(p => !p.IsCorePlugin && (skip == null || !skip.Contains(p.Name))).ToArray())
            {
                ReloadPlugin(plugin.Name);
            }
        }

        /// <summary>
        /// Loads a plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool LoadPlugin(string name)
        {
            // Check if the plugin is already loaded
            if (RootPluginManager.GetPlugin(name) != null) return false;

            // Find all plugin loaders that lay claim to the name
            var loaders = new HashSet<PluginLoader>(extensionmanager.GetPluginLoaders().Where(l => l.ScanDirectory(PluginDirectory).Contains(name)));
            if (loaders.Count == 0)
            {
                LogError("Failed to load plugin '{0}' (no source found)", name);
                return false;
            }
            if (loaders.Count > 1)
            {
                LogError("Failed to load plugin '{0}' (multiple sources found)", name);
                return false;
            }
            var loader = loaders.First();

            // Load it and watch for errors
            Plugin plugin;
            try
            {
                plugin = loader.Load(PluginDirectory, name);
                if (plugin == null) return true; // async load
                plugin.Loader = loader;
                PluginLoaded(plugin);
                return true;
            }
            catch (Exception ex)
            {
                LogException(string.Format("Failed to load plugin {0}", name), ex);
                return false;
            }
        }

        public bool PluginLoaded(Plugin plugin)
        {
            plugin.OnError += plugin_OnError;
            // Log plugin loaded
            LogInfo("Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            try
            {
                plugin.Loader?.PluginErrors.Remove(plugin.Name);
                RootPluginManager.AddPlugin(plugin);
                if (plugin.Loader != null)
                {
                    if (plugin.Loader.PluginErrors.ContainsKey(plugin.Name))
                    {
                        UnloadPlugin(plugin.Name);
                        return false;
                    }
                }
                plugin.IsLoaded = true;
                CallHook("OnPluginLoaded", plugin);
                return true;
            }
            catch (Exception ex)
            {
                if (plugin.Loader != null) plugin.Loader.PluginErrors[plugin.Name] = ex.Message;
                LogException($"Failed to initialize plugin '{plugin.Name} v{plugin.Version}'", ex);
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
            var plugin = RootPluginManager.GetPlugin(name);
            if (plugin == null) return false;

            // Let the plugin loader know that this plugin is being unloaded
            var loader = extensionmanager.GetPluginLoaders().SingleOrDefault(l => l.LoadedPlugins.ContainsKey(name));
            if (loader != null) loader.Unloading(plugin);

            // Unload it
            RootPluginManager.RemovePlugin(plugin);

            // Let other plugins know that this plugin has been unloaded
            if (plugin.IsLoaded) CallHook("OnPluginUnloaded", plugin);
            plugin.IsLoaded = false;

            LogInfo("Unloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            return true;
        }

        /// <summary>
        /// Reloads the plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool ReloadPlugin(string name)
        {
            var loader = extensionmanager.GetPluginLoaders().FirstOrDefault(l => l.ScanDirectory(PluginDirectory).Contains(name));
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
            LogError("{0} v{1}: {2}", sender.Name, sender.Version, message);
        }

        #endregion

        /// <summary>
        /// Calls a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, params object[] args)
        {
            if (RootPluginManager == null) return null;
            return RootPluginManager.CallHook(hookname, args);
        }

        /// <summary>
        /// Calls a deprecated hook and print a warning
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallDeprecatedHook(string hookname, params object[] args)
        {
            if (RootPluginManager == null) return null;
            return RootPluginManager.CallDeprecatedHook(hookname, args);
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
        public void OnFrame(Action<float> callback)
        {
            onFrame += callback;
        }

        /// <summary>
        /// Called every server frame, implemented by an engine-specific extension
        /// </summary>
        public void OnFrame(float delta)
        {
            // Call any callbacks queued for this frame
            if (nextTickQueue.Count > 0)
            {
                List<Action> queued;
                lock (nextTickLock)
                {
                    queued = nextTickQueue;
                    nextTickQueue = new List<Action>();
                }
                for (var i = 0; i < queued.Count; i++)
                {
                    try
                    {
                        queued[i]();
                    }
                    catch (Exception ex)
                    {
                        LogException("Exception while calling NextTick callback", ex);
                    }
                }
            }

            // Update libraries
            libtimer.Update(delta);

            // Don't update plugin watchers or call OnFrame in plugins until servers starts ticking
            if (!isInitialized) return;

            if (ServerConsole != null) ServerConsole.Update();

            // Update extensions
            try
            {
                onFrame?.Invoke(delta);
            }
            catch (Exception ex)
            {
                LogException($"{ex.GetType().Name} while invoke OnFrame in extensions", ex);
            }
        }

        public void OnShutdown()
        {
            IsShuttingDown = true;
            UnloadAllPlugins();
            foreach (var extension in extensionmanager.GetAllExtensions())
            {
                extension.OnShutdown();
            }
            if (ServerConsole != null) ServerConsole.OnDisable();
        }

        /// <summary>
        /// Called by an engine-specific extension to register the engine clock
        /// </summary>
        /// <param name="method"></param>
        public void RegisterEngineClock(Func<float> method)
        {
            getTimeSinceStartup = method;
        }

        public bool CheckConsole(bool force = false)
        {
            return ConsoleWindow.Check(force) && !rootconfig.DisableConsole;
        }

        public bool EnableConsole(bool force = false)
        {
            if (!CheckConsole(force)) return false;
            ServerConsole = new ServerConsole.ServerConsole();
            ServerConsole.OnEnable();
            return true;
        }

        #region Plugin Change Watchers
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

        private static string CleanPath(string path)
        {
            return path?.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        private static void RegisterLibrarySearchPath(string path)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    var newPath = string.IsNullOrEmpty(currentPath) ? path : currentPath + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("PATH", newPath);
                    SetDllDirectory(path);
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    var currentLdLibraryPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
                    path = "." + Path.PathSeparator + path;
                    var newLdLibraryPath = string.IsNullOrEmpty(currentLdLibraryPath) ? path : currentLdLibraryPath + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", newLdLibraryPath);
                    break;
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
