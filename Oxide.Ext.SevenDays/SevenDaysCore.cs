using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.SevenDays.Plugins
{
    /// <summary>
    /// The core 7 Days to Die plugin
    /// </summary>
    public class SevenDaysCore : CSPlugin
    {
        // Track when the server has been initialized
        private bool ServerInitialized;
        private bool LoggingInitialized;

        /// <summary>
        /// Initializes a new instance of the SevenDaysCore class
        /// </summary>
        public SevenDaysCore()
        {
            // Set attributes
            Name = "sevendayscore";
            Title = "Seven Days Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "7 days to die");
            RemoteLogger.SetTag("protocol", cl000c.cCompatibilityVersion.ToLower());
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (ServerInitialized) return;
            ServerInitialized = true;
            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", GamePrefs.GetString(EnumGamePrefs.ServerName));
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown()
        {
            Interface.Oxide.OnShutdown();
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (ServerInitialized) plugin.CallHook("OnServerInitialized");
            if (!LoggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            LoggingInitialized = true;
            CallHook("InitLogging", null);
        }
    }
}
