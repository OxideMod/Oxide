using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using UnityEngine;

namespace Oxide.Game.FortressCraft
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class FortressCraftExtension : Extension
    {
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;

        /// <summary>
        /// Gets whether this extension is for a specific game
        /// </summary>
        public override bool IsGameExtension => true;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "FortressCraft";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

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
            "***** Current Players :", // Not filtered
            "***** DISK *****", // Not filtered
            "***** LOAD *****", // Not filtered
            "***** Last minute *****", // Not filtered
            "***** Network *****", // Not filtered
            "***** OVERALL *****", // Not filtered
            "***** Timings *****", // Not filtered
            "******************** STARTING GAME ********************",
            "********************** GAME MANAGER STARTUP **********************",
            "***Player Stats Active on object_PersistentSettings",
            "***SteamManager Initalising",
            "***SteamManager requesting Global Stats...***",
            "Achievement Manager configured",
            "Applying Dedicated server overrides now!",
            "Archives Compressed", // Not filtered
            "Attempting to roughly spawn player",
            "BIG WARNING! USER ID IS ZERO!",
            "Bars Smelted :", // Not filtered
            "BlockSelectPanel now configured",
            "C5 Total:",
            "C5:",
            "CPU:",
            "Cache: CompressWhenServerEmpty", // Not filtered
            "Camodingbridge",
            "Camoland",
            "Camoville",
            "Closing down SteamManager...",
            "Coalbury",
            "Coalton",
            "Configuring Disk Thread",
            "Configuring Steam Callbacks...",
            "Converting Pos to Unity...",
            "Creating world frustrum at",
            "Dedicated server port",
            "Deepbury",
            "Deepford",
            "Disabling HMD support",
            "Disk Thread.Comp:", // Not filtered
            "Djbot",
            "Easy Power : True",
            "FC currently has",
            "FF Total:",
            "Failed to load debug settings, using (some) defaults",
            "FillRate:",
            "FlatLand",
            "Fluid 0/0", // Not filtered
            "Fortressford",
            "Fortressville",
            "Found eligible segment at",
            "Found no Injection Overrides at",
            "Frozendingbridge",
            "Frozenland",
            "GC is reporting", // Not filtered
            "Game Paused :", // Not filtered
            "Game is running in [english]",
            "Gameplay Manager took", // Not filtered
            "Global FlatLand unlocks:",
            "Global OET Charged :",
            "Grabbing public server list...",
            "HBAO shader is not supported on this platform.",
            "Headless server active",
            "Hexahedronville",
            "Hivedingbridge",
            "Hivemind",
            "Icedingbridge",
            "Iceton",
            "Indexing entry :",
            "Initialising research in shared mode.",
            "Injection Manager took", // Not filtered
            "Instantiating Player",
            "Intialising with a Draw distance of",
            "Item Manager took", // Not filtered
            "Items 0. Max was 0. Current time", // Not filtered
            "Items took", // Not filtered
            "Load distance uses approx",
            "Load world data settings, resource factor:",
            "Loaded previous address:",
            "Loading settings.ini file from:",
            "Local user has hand-built",
            "Located 2 public servers!",
            "Located IP and stripped to",
            "Machines :",
            "Machines took", // Not filtered
            "No slots file found",
            "No texture files found!",
            "Not skipping spawn search...",
            "OrderBlockManager allocated an estimated",
            "Ore Extracted :", // Not filtered
            "Persistent Settings",
            "PTG Power :", // Not filtered
            "Player Inventory initialised",
            "Power :", // Not filtered
            "Public server list retrieved!",
            "RAM:",
            "Raycast Load:", // Not filtered
            "Re-entrant level load call!",
            "ReadWorldData :",
            "Received stats and achievements from Steam",
            "RocketManager; ready to rocket!",
            "RushMode",
            "Saved:", // Not filtered
            "Segment Manager took", // Not filtered
            "Segment Updater took", // Not filtered
            "Server 0",
            "Server 1",
            "Server IP is", // Not filtered
            "Server Session Time :", // Not filtered
            "Server has currently sent", // Not filtered
            "Server now ready for players to join!",
            "Server startup grabbing World Time Player of",
            "SetSettingsFromWorldData!",
            "Setting MaxOrderBlocks to", // Not filtered
            "Setting breakpad minidump AppID", // Not filtered
            "ShaderLevel:",
            "Showing Confirmation Panel",
            "Snowland",
            "Solar Power :", // Not filtered
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
            "Total Network Server CPU time:", // Not filtered
            "Total Packets resent:", // Not filtered
            "Total Power :", // Not filtered
            "Total Power(Min) :", // Not filtered
            "Total Segment Manager CPU time:", // Not filtered
            "Tunnel Nuker exploding!", // Not filtered
            "Turbine Power :", // Not filtered
            "UI took",
            "UniStorm - Time of day set externally to",
            "Updates/Sleeps :", // Not filtered
            "User has built",
            "User has never been in survival?",
            "VRam:",
            "WWW Ok! - result:",
            "Waiting to send first packet!", // Not filtered
            "Warning, WorldScript.instance is null, can't update server time",
            "Waypoint had no payload?!",
            "Welcome to FortressCraft,",
            "Work remaining:", // Not filtered
            "World Frustrum added of dimension",
            "World Uptime :", // Not filtered
            "WorldScript::SpawnLocalPlayer",
            "Worldmind",
            "Worlds loaded from:",
            "[1102 - UserStatsReceived]",
            "[1112 - GlobalStatsReceived]",
            "[NETWORK CONSOLE]",
            "[Server] Loading world",
            "[[{\"_id\":",
            "active machine frustae", // Not filtered
            "cap offset:",
            "console 0 found name:",
            "disk safe test:",
            "error _PersistentSettings in _PersistentSettings just took",
            "error _World in _World just took",
            "machines are using up", // Not filtered
            "mobs are using up", // Not filtered
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
            // TODO: Intercept Console.WriteLine for filtering, if possible

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{NetworkManager.instance.mServerThread.GetNumPlayers()} | {(global::ServerConsole.WorldName)}";

            Interface.Oxide.ServerConsole.Status1Left = () => global::ServerConsole.WorldName;
            //Interface.Oxide.ServerConsole.Status1Right = () => $"{Performance.current.frameRate}fps, {((ulong)Time.realtimeSinceStartup).FormatSeconds()}";

            /*Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = $"{NetworkManager.instance.mServerThread.GetNumPlayers()}/{ConVar.Server.maxplayers} players";
                var sleepers = BasePlayer.sleepingPlayerList.Count;
                var entities = BaseNetworkable.serverEntities.Count;
                return $"{players}, {sleepers + (sleepers.Equals(1) ? " sleeper" : " sleepers")}, {entities + (entities.Equals(1) ? " entity" : " entities")}";
            };*/
            /*Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (Net.sv == null || !Net.sv.IsConnected()) return "not connected";

                var bytesReceived = Net.sv.GetStat(null, NetworkPeer.StatTypeLong.BytesReceived_LastSecond);
                var bytesSent = Net.sv.GetStat(null, NetworkPeer.StatTypeLong.BytesSent_LastSecond);
                return $"{Utility.FormatBytes(bytesReceived) ?? "0"}/s in, {Utility.FormatBytes(bytesSent) ?? "0"}/s out";
            };*/

            /*Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var gameTime = (!TOD_Sky.Instance ? DateTime.Now : TOD_Sky.Instance.Cycle.DateTime).ToString("h:mm tt");
                return $"{gameTime.ToLower()}, {ConVar.Server.level} [{ConVar.Server.worldsize}, {ConVar.Server.seed}]";
            };*/
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {HUDManager.Version}"; // TODO: Use cleaned up version
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            /*Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (!input.Contains(".")) input = string.Concat("global.", input);
                return ConsoleSystem.Index.GetAll().Where(c => c.namefull.StartsWith(input.ToLower())).ToList().ConvertAll(c => c.namefull).ToArray();
            };*/
        }

        private static void ServerConsoleOnInput(string input)
        {
            input = input.Trim();
            if (!string.IsNullOrEmpty(input)) global::ServerConsole.DoServerString(input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            var remoteType = "generic";

            if (type == LogType.Warning)
            {
                color = ConsoleColor.Yellow;
                remoteType = "warning";
            }
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                color = ConsoleColor.Red;
                remoteType = "error";
            }

            Interface.Oxide.ServerConsole.AddMessage(message, color);
            Interface.Oxide.RemoteConsole.SendMessage(new RemoteMessage
            {
                Message = message,
                Identifier = 0,
                Type = remoteType,
                Stacktrace = stackTrace
            });
        }
    }
}
