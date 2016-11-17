using System;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Extensions;
using UnityEngine;

namespace Oxide.Game.FortressCraft
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class FortressCraftExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "FortressCraft";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            "** SpawnLocalPlayer Complete! **",
            "*** Configuring Survival Player ***",
            "*** Spawning Survival ARTHER ***",
            "*** Survival Hotbar awaking ***",
            "**** USER STATS RECEIVED OK! ****",
            "******************** STARTING GAME ********************",
            "********************** GAME MANAGER STARTUP **********************",
            "***Player Stats Active on object_PersistentSettings",
            "***SteamManager Initalising",
            "***SteamManager requesting Global Stats...***",
            "0.00 - error _PersistentSettings in _PersistentSettings just took",
            "Achievement Manager configured",
            "Applying Dedicated server overrides now!",
            "Attempting to roughly spawn player",
            "BIG WARNING! USER ID IS ZERO!",
            "BlockSelectPanel now configured",
            "C5 Total:",
            "C5:",
            "CPU:",
            "Camoville",
            "Closing down SteamManager...",
            "Configuring Disk Thread",
            "Configuring Steam Callbacks...",
            "Converting Pos to Unity...",
            "Creating world frustrum at",
            "Dedicated server port",
            "Deepbury",
            "Deepford",
            "Easy Power : True",
            "FC currently has",
            "FF Total:",
            "Failed to load debug settings, using (some) defaults",
            "FillRate:",
            "FlatLand",
            "Fortressford",
            "Found eligible segment at",
            "Found no Injection Overrides at",
            "Frozendingbridge",
            "Frozenland",
            "Game is running in [english]",
            "Global FlatLand unlocks:",
            "Global OET Charged :",
            "Grabbing public server list...",
            "HBAO shader is not supported on this platform.",
            "Headless server active",
            "Hexahedronville",
            "Hivemind",
            "Icedingbridge",
            "Indexing entry :",
            "Initialising research in shared mode.",
            "Instantiating Player",
            "Intialising with a Draw distance of",
            "Load distance uses approx",
            "Load world data settings, resource factor:",
            "Loaded previous address:",
            "Loading settings.ini file from:",
            "Local user has hand-built",
            "Located 2 public servers!",
            "Located IP and stripped to",
            "No slots file found",
            "No texture files found!",
            "Not skipping spawn search...",
            "OrderBlockManager allocated an estimated",
            "Player Inventory initialised",
            "Public server list retrieved!",
            "RAM:",
            "Re-entrant level load call!",
            "ReadWorldData :",
            "Received stats and achievements from Steam",
            "RocketManager; ready to rocket!",
            "RushMode",
            "Server 0",
            "Server 1",
            "Server IP is", // Not filtered
            "Server now ready for players to join!",
            "Server startup grabbing World Time Player of",
            "SetSettingsFromWorldData!",
            "Setting MaxOrderBlocks to", // Not filtered
            "Setting breakpad minidump AppID", // Not filtered
            "ShaderLevel:",
            "Showing Confirmation Panel",
            "Snowland",
            "Sparklies!",
            "Spawn Configuring",
            "Spawning Player instance....",
            "Spawning local player",
            "Spider has 8 feet!",
            "Split!",
            "Starting  with ThreadID",
            "Starting Async Level Load:",
            "Starting Dedicated server",
            "Steam Callbacks Configured ok!",
            "SteamManager has successfully initialised Steam",
            "SteamManager requesting User Stats...",
            "Steam_SetMinidumpSteamID:  Caching Steam ID:",
            "SurvivalHotBarManager added to",
            "Switching from weapon eNone to",
            "System has",
            "The image effect",
            "This object has",
            "This player has",
            "Thread with ThreadID",
            "Tunnel Nuker exploding!", // Not filtered
            "UI took",
            "UniStorm - Time of day set externally to",
            "User has built",
            "User has never been in survival?",
            "VRam:",
            "WWW Ok! - result:",
            "Warning, WorldScript.instance is null, can't update server time",
            "Waypoint had no payload?!",
            "Welcome to FortressCraft,",
            "World Frustrum added of dimension",
            "WorldScript::SpawnLocalPlayer",
            "Worlds loaded from:",
            "[1102 - UserStatsReceived]",
            "[1112 - GlobalStatsReceived]",
            "[NETWORK CONSOLE]",
            "[Server] Loading world",
            "[[{\"_id\":",
            "cap offset:",
            "console 0 found name:",
            "disk safe test:",
            "spawnable object types!"
        };

        /// <summary>
        /// Initializes a new instance of the FortressCraftExtension class
        /// </summary>
        /// <param name="manager"></param>
        public FortressCraftExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new FortressCraftPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            if (!Interface.Oxide.EnableConsole()) return;

            Application.logMessageReceived += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{NetworkManager.instance.mServerThread.GetNumPlayers()} | {(global::ServerConsole.WorldName)}";

            Interface.Oxide.ServerConsole.Status1Left = () => global::ServerConsole.WorldName;

            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {HUDManager.Version}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) global::ServerConsole.DoServerString(input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
