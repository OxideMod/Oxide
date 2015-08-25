using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Game.Terraria
{
    /// <summary>
    /// The core Terraria plugin
    /// </summary>
    public class TerrariaCore : CSPlugin
    {
        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the TerrariaCore class
        /// </summary>
        public TerrariaCore()
        {
            // Set attributes
            Name = "terrariascore";
            Title = "Terraria Core";
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
            RemoteLogger.SetTag("game", "terraria");
            //RemoteLogger.SetTag("protocol", cl000c.cCompatibilityVersion.ToLower());
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;
            // Configure the hostname after it has been set
            //RemoteLogger.SetTag("hostname", GamePrefs.GetString(EnumGamePrefs.ServerName));
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
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized)
                InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }
    }
}
