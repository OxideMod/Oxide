extern alias Oxide;

using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.ServerConsole;
using Oxide::Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Timer = Oxide.Core.Libraries.Timer;

namespace Oxide.Core
{
    public delegate void NativeDebugCallback(string message);

    /// <summary>
    /// Responsible for core Oxide logic
    /// </summary>
    public sealed class OxideMod
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// The current Oxide version
        /// </summary>
        public static readonly VersionNumber Version = new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

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
        public string LangDirectory { get; private set; }
        public string LogDirectory { get; private set; }

        // Gets the number of seconds since the server started
        public float Now => getTimeSinceStartup();

        /// <summary>
        /// This is true if the server is shutting down
        /// </summary>
        public bool IsShuttingDown { get; private set; }

        // The extension manager
        private ExtensionManager extensionManager;

        // The command line
        public CommandLine CommandLine;

        // Various configs
        public OxideConfig Config { get; private set; }

        // Various libraries
        private Timer libtimer;
        private Covalence covalence;

        // Extension implemented delegates
        private Func<float> getTimeSinceStartup;

        // Thread safe NextTick callback queue
        private List<Action> nextTickQueue = new List<Action>();
        private List<Action> lastTickQueue = new List<Action>();
        private readonly object nextTickLock = new object();

        // Allow extensions to register a method to be called every frame
        private Action<float> onFrame;
        private bool isInitialized;
        public bool HasLoadedCorePlugins { get; private set; }

        public RemoteConsole.RemoteConsole RemoteConsole;
        public ServerConsole.ServerConsole ServerConsole;

        private Stopwatch timer;

        private NativeDebugCallback debugCallback;

        public OxideMod(NativeDebugCallback debugCallback)
        {
            this.debugCallback = debugCallback;
        }

        /// <summary>
        /// Initializes a new instance of the OxideMod class
        /// </summary>
        public void Load()
        {
            RootDirectory = Environment.CurrentDirectory;
            if (RootDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
                RootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (RootDirectory == null) throw new Exception($"RootDirectory is null");

            InstanceDirectory = Path.Combine(RootDirectory, "oxide");

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture };

            CommandLine = new CommandLine(Environment.GetCommandLineArgs());
            if (CommandLine.HasVariable("oxide.directory"))
            {
                string var, format;
                CommandLine.GetArgument("oxide.directory", out var, out format);
                if (string.IsNullOrEmpty(var) || CommandLine.HasVariable(var))
                    InstanceDirectory = Path.Combine(RootDirectory, Utility.CleanPath(string.Format(format, CommandLine.GetVariable(var))));
            }

            ExtensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (ExtensionDirectory == null || !Directory.Exists(ExtensionDirectory)) throw new Exception("Could not identify extension directory");

            PluginDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("plugins"));
            ConfigDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("config"));
            DataDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("data"));
            LangDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("lang"));
            LogDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("logs"));
            if (!Directory.Exists(InstanceDirectory)) Directory.CreateDirectory(InstanceDirectory);
            if (!Directory.Exists(PluginDirectory)) Directory.CreateDirectory(PluginDirectory);
            if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
            if (!Directory.Exists(LangDirectory)) Directory.CreateDirectory(LangDirectory);
            if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);

            RegisterLibrarySearchPath(Path.Combine(ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86"));

            var config = Path.Combine(InstanceDirectory, "oxide.config.json");
            if (File.Exists(config))
                Config = ConfigFile.Load<OxideConfig>(config);
            else
            {
                Config = new OxideConfig(config);
                Config.Save();
            }

            if (CommandLine.HasVariable("rcon.port"))
                Config.Rcon.Port = int.Parse(CommandLine.GetVariable("rcon.port"));

            RootLogger = new CompoundLogger();
            RootLogger.AddLogger(new RotatingFileLogger { Directory = LogDirectory });
            if (debugCallback != null) RootLogger.AddLogger(new CallbackLogger(debugCallback));

            LogInfo("Loading Oxide Core v{0}...", Version);

            RootPluginManager = new PluginManager(RootLogger) { ConfigPath = ConfigDirectory };
            extensionManager = new ExtensionManager(RootLogger);
            DataFileSystem = new DataFileSystem(DataDirectory);

            extensionManager.RegisterLibrary("Covalence", covalence = new Covalence());
            extensionManager.RegisterLibrary("Global", new Global());
            extensionManager.RegisterLibrary("Lang", new Lang());
            extensionManager.RegisterLibrary("Permission", new Permission());
            extensionManager.RegisterLibrary("Plugins", new Libraries.Plugins(RootPluginManager));
            extensionManager.RegisterLibrary("Time", new Time());
            extensionManager.RegisterLibrary("Timer", libtimer = new Timer());
            extensionManager.RegisterLibrary("WebRequests", new WebRequests());

            LogInfo("Loading extensions...");
            extensionManager.LoadAllExtensions(ExtensionDirectory);

            Cleanup.Run();
            covalence.Initialize();
            RemoteConsole = new RemoteConsole.RemoteConsole();
            RemoteConsole?.Initalize();

            if (getTimeSinceStartup == null)
            {
                timer = new Stopwatch();
                timer.Start();
                getTimeSinceStartup = () => (float)timer.Elapsed.TotalSeconds;
                LogWarning("A reliable clock is not available, falling back to a clock which may be unreliable on certain hardware");
            }

            foreach (var ext in extensionManager.GetAllExtensions()) ext.LoadPluginWatchers(PluginDirectory);
            LogInfo("Loading plugins...");
            LoadAllPlugins(true);

            foreach (var watcher in extensionManager.GetPluginChangeWatchers())
            {
                watcher.OnPluginSourceChanged += watcher_OnPluginSourceChanged;
                watcher.OnPluginAdded += watcher_OnPluginAdded;
                watcher.OnPluginRemoved += watcher_OnPluginRemoved;
            }

            if (CommandLine.HasVariable("nolog")) LogWarning("Usage of the 'nolog' variable will prevent logging");
        }

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetLibrary<T>(string name = null) where T : Library => extensionManager.GetLibrary(name ?? typeof(T).Name) as T;

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions() => extensionManager.GetAllExtensions();

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders() => extensionManager.GetPluginLoaders();

        #region Logging

        /// <summary>
        /// Logs a formatted info message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogInfo(string format, params object[] args) => RootLogger.Write(LogType.Info, format, args);

        /// <summary>
        /// Logs a formatted debug message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogDebug(string format, params object[] args) => RootLogger.Write(LogType.Debug, format, args);

        /// <summary>
        /// Logs a formatted warning message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogWarning(string format, params object[] args) => RootLogger.Write(LogType.Warning, format, args);

        /// <summary>
        /// Logs a formatted error message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogError(string format, params object[] args) => RootLogger.Write(LogType.Error, format, args);

        /// <summary>
        /// Logs an exception to the root logger
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public void LogException(string message, Exception ex) => RootLogger.WriteException(message, ex);

        #endregion Logging

        #region Plugin Management

        /// <summary>
        /// Scans for all available plugins and attempts to load them
        /// </summary>
        public void LoadAllPlugins(bool init = false)
        {
            var loaders = extensionManager.GetPluginLoaders();

            // Load all core plugins first
            if (!HasLoadedCorePlugins)
            {
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
                            LogException($"Failed to load core plugin {type.Name}", ex);
                        }
                    }
                }
                HasLoadedCorePlugins = true;
            }

            // Scan the plugin directory and load all reported plugins
            foreach (var loader in loaders)
                foreach (var name in loader.ScanDirectory(PluginDirectory)) LoadPlugin(name);

            if (!init) return;

            var lastCall = Now;
            foreach (var loader in extensionManager.GetPluginLoaders())
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
                UnloadPlugin(plugin.Name);
        }

        /// <summary>
        /// Reloads all plugins
        /// </summary>
        public void ReloadAllPlugins(IList<string> skip = null)
        {
            foreach (var plugin in RootPluginManager.GetPlugins().Where(p => !p.IsCorePlugin && (skip == null || !skip.Contains(p.Name))).ToArray())
                ReloadPlugin(plugin.Name);
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
            var loaders = new HashSet<PluginLoader>(extensionManager.GetPluginLoaders().Where(l => l.ScanDirectory(PluginDirectory).Contains(name)));

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

            // Load it and watch for errors
            var loader = loaders.First();
            try
            {
                var plugin = loader.Load(PluginDirectory, name);
                if (plugin == null) return true; // Async load
                plugin.Loader = loader;
                PluginLoaded(plugin);
                return true;
            }
            catch (Exception ex)
            {
                LogException($"Failed to load plugin {name}", ex);
                return false;
            }
        }

        public bool PluginLoaded(Plugin plugin)
        {
            plugin.OnError += plugin_OnError;
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
                LogInfo("Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
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
            var loader = extensionManager.GetPluginLoaders().SingleOrDefault(l => l.LoadedPlugins.ContainsKey(name));
            loader?.Unloading(plugin);

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
            var isNested = false;
            var directory = PluginDirectory;
            if (name.Contains("\\"))
            {
                isNested = true;
                var subPath = Path.GetDirectoryName(name);
                if (subPath != null)
                {
                    directory = Path.Combine(directory, subPath);
                    name = name.Substring(subPath.Length + 1);
                }
            }
            var loader = extensionManager.GetPluginLoaders().FirstOrDefault(l => l.ScanDirectory(directory).Contains(name));
            if (loader != null)
            {
                loader.Reload(directory, name);
                return true;
            }
            if (isNested) return false;
            UnloadPlugin(name);
            LoadPlugin(name);
            return true;
        }

        /// <summary>
        /// Called when a plugin has raised an error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void plugin_OnError(Plugin sender, string message) => LogError("{0} v{1}: {2}", sender.Name, sender.Version, message);

        #endregion Plugin Management

        /// <summary>
        /// Calls a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, params object[] args) => RootPluginManager?.CallHook(hookname, args);

        /// <summary>
        /// Calls a deprecated hook and prints a warning
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            return RootPluginManager?.CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

        /// <summary>
        /// Queues a callback to be called in the next server frame
        /// </summary>
        /// <param name="callback"></param>
        public void NextTick(Action callback)
        {
            lock (nextTickLock) nextTickQueue.Add(callback);
        }

        /// <summary>
        /// Registers a callback which will be called every server frame
        /// </summary>
        /// <param name="callback"></param>
        public void OnFrame(Action<float> callback) => onFrame += callback;

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
                    nextTickQueue = lastTickQueue;
                    lastTickQueue = queued;
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
                queued.Clear();
            }

            // Update libraries
            libtimer.Update(delta);

            // Don't update plugin watchers or call OnFrame in plugins until servers starts ticking
            if (!isInitialized) return;

            ServerConsole?.Update();

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
            if (IsShuttingDown) return;

            IsShuttingDown = true;
            UnloadAllPlugins();

            foreach (var extension in extensionManager.GetAllExtensions()) extension.OnShutdown();
            foreach (var name in extensionManager.GetLibraries()) extensionManager.GetLibrary(name).Shutdown();

            RemoteConsole?.Shutdown();
            ServerConsole?.OnDisable();
            RootLogger.Shutdown();
        }

        /// <summary>
        /// Called by an engine-specific extension to register the engine clock
        /// </summary>
        /// <param name="method"></param>
        public void RegisterEngineClock(Func<float> method) => getTimeSinceStartup = method;

        public bool CheckConsole(bool force = false) => ConsoleWindow.Check(force) && Config.Console.Enabled;

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
        private void watcher_OnPluginSourceChanged(string name) => ReloadPlugin(name);

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginAdded(string name) => LoadPlugin(name);

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginRemoved(string name) => UnloadPlugin(name);

        #endregion Plugin Change Watchers

        #region Library Paths

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
                    var newLdLibraryPath = string.IsNullOrEmpty(currentLdLibraryPath) ? path : currentLdLibraryPath + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", newLdLibraryPath);
                    break;
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        #endregion Library Paths
    }
}
