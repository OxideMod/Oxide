using System;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class SevenDaysExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "SevenDays";

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
            "* SKY INITIALIZED",
            "Awake done",
            "Biomes image size",
            "Command line arguments:",
            "Dedicated server only build",
            "Exited thread thread_",
            "GamePref.",
            "GameStat.",
            "HDR Render",
            "HDR and MultisampleAntiAliasing",
            "INF AIDirector:",
            "INF Adding observed entity:",
            "INF BiomeSpawnManager spawned",
            "INF Cleanup",
            "INF Clearing all pools",
            "INF Created player with",
            "INF Disconnect",
            "INF GMA.",
            "INF GOM.",
            "INF Kicking player:",
            "INF OnApplicationQuit",
            "INF PPS RequestToEnterGame sending player list",
            "INF Removing observed entity",
            "INF RequestToEnterGame:",
            "INF RequestToSpawnPlayer:",
            "INF Spawned",
            "INF Start a new wave",
            "INF Time:",
            "INF Token length:",
            "INF WSD.",
            "Load key config",
            "Loading permissions file at",
            "NET: Starting server protocols",
            "NET: Stopping server protocols",
            "NET: Unity NW server",
            "POI image size",
            "Parsing server configfile:",
            "Persistent GamePrefs saved",
            "SaveAndCleanupWorld",
            "SelectionBoxManager.Instance:",
            "Setting breakpad minidump AppID",
            "StartAsServer",
            "StartGame",
            "Started thread",
            "Weather Packages Created",
            "World.Cleanup",
            "World.Load:",
            "World.Unload",
            "WorldStaticData.Init()",
            "[EAC] FreeUser",
            "[EAC] Log:",
            "[EAC] UserStatusHandler callback",
            "[NET] PlayerConnected",
            "[NET] PlayerDisconnected",
            "[NET] ServerShutdown",
            "[Steamworks.NET]",
            "createWorld() done",
            "createWorld:"
        };

        /// <summary>
        /// Initializes a new instance of the SevenDaysExtension class
        /// </summary>
        /// <param name="manager"></param>
        public SevenDaysExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new SevenDaysPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("SDTD", new Libraries.SevenDays());
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
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                return string.IsNullOrEmpty(input) ? null : SdtdConsole.Instance.commands.Keys.Where(c => c.StartsWith(input.ToLower())).ToArray();
            };
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{GameManager.Instance.World.Players.Count} | {GamePrefs.GetString(EnumGamePrefs.ServerName)}";

            Interface.Oxide.ServerConsole.Status1Left = () => GamePrefs.GetString(EnumGamePrefs.ServerName);
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = $"{GameManager.Instance.World.Players.Count}/{GamePrefs.GetInt(EnumGamePrefs.ServerMaxPlayerCount)}";
                var entities = GameManager.Instance.World.Entities.Count;
                return $"{players}, {entities + (entities.Equals(1) ? " entity" : " entities")}";
            };
            Interface.Oxide.ServerConsole.Status2Right = () => string.Empty; // TODO: Network in/out

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var gameTime = GameManager.Instance.World.worldTime;
                var dateTime = Convert.ToDateTime($"{GameUtils.WorldTimeToHours(gameTime)}:{GameUtils.WorldTimeToMinutes(gameTime)}").ToString("h:mm tt");
                return $"{dateTime.ToLower()}, {GamePrefs.GetString(EnumGamePrefs.GameWorld)} [{GamePrefs.GetString(EnumGamePrefs.GameName)}]";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {GamePrefs.GetString(EnumGamePrefs.GameVersion)}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            var result = SdtdConsole.Instance.ExecuteSync(input, null);
            if (result != null) Interface.Oxide.ServerConsole.AddMessage(string.Join("\n", result.ToArray()));
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
