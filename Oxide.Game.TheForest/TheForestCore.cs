using System;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The core The Forest plugin
    /// </summary>
    public class TheForestCore : CSPlugin
    {
        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the TheForestCore class
        /// </summary>
        public TheForestCore()
        {
            // Set attributes
            Name = "theforestcore";
            Title = "The Forest Core";
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
            RemoteLogger.SetTag("game", "the forest");
            RemoteLogger.SetTag("protocol", "0.24b"); // TODO: Grab version/protocol
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
            RemoteLogger.SetTag("hostname", CoopLobby.Instance?.Info.Name.Split("()".ToCharArray())[0]);
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
            if (!loggingInitialized && plugin.Name == "unitycore")
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

        /// <summary>
        /// Initializes the server
        /// </summary>
        [HookMethod("InitServer")]
        private void InitServer()
        {
            Interface.Oxide.LogInfo("InitServer");
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogInfo("InitServer NextTick");

                var coop = UnityEngine.Object.FindObjectOfType<TitleScreen>();
                coop.OnCoOp();
                coop.OnMpHost();
                coop.OnNewGame();
            });
        }

        /// <summary>
        /// Initializes the server lobby
        /// </summary>
        [HookMethod("InitLobby")]
        private void InitLobby(CoopSteamNGUI ngui, Enum screen)
        {
            Interface.Oxide.LogInfo("InitLobby");

            Type type = typeof(CoopSteamNGUI).GetNestedTypes(BindingFlags.NonPublic).FirstOrDefault(x => x.IsEnum && x.Name.Equals("Screens"));
            Interface.Oxide.LogInfo("Type: {0}", type);
            object enumValue = type.GetField("LobbySetup", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            Interface.Oxide.LogInfo("Screen: {0} Value: {0} Name: {1}", Convert.ToInt32(screen), (int)enumValue, Enum.GetName(type, enumValue));
            if (Convert.ToInt32(screen) != (int)enumValue) return;
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogInfo("InitLobby NextTick");

                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostLobbySetup();
            });
        }
    }
}
