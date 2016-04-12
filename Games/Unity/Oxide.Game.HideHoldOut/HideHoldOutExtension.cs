using System;
using System.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.HideHoldOut
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class HideHoldOutExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "HideHoldOut";

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
            NetworkController.NetManager_.get_GAME_VERSION,
            "CONNECTION GRANTED",
            "DataBase_Storing end",
            "DataBase_Storing start",
            "Player wants to connect. Waiting for approval",
            "Resulting playerInfos",
            "Searching for a player",
            "b - ServerManager - DataBase_Storing"
        };

        /// <summary>
        /// Initializes a new instance of the HideHoldOutExtension class
        /// </summary>
        /// <param name="manager"></param>
        public HideHoldOutExtension(ExtensionManager manager)
            : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new HideHoldOutPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("H2o", new Libraries.HideHoldOut());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public override void LoadPluginWatchers(string pluginDirectory)
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

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var players = uLink.Network.connections.Length;
                var hostname = NetworkController.NetManager_.ServManager.Server_NAME;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                var hostname = NetworkController.NetManager_.ServManager.Server_NAME;
                return string.Concat(" ", hostname);
            };
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = uLink.Network.connections.Length;
                var playerLimit = NetworkController.NetManager_.ServManager.NumberOfSlot;
                return string.Concat(" ", players, "/", playerLimit, " players");
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.NetworkTime.serverTime <= 0) return "0b/s in, 0b/s out";
                double bytesSent = 0;
                double bytesReceived = 0;
                foreach (var connection in uLink.Network.connections)
                {
                    var stats = connection.statistics;
                    if (stats == null) continue;
                    bytesSent += stats.bytesSentPerSecond;
                    bytesReceived += stats.bytesReceivedPerSecond;
                }
                return string.Concat(Utility.FormatBytes(bytesReceived), "/s in, ", Utility.FormatBytes(bytesSent), "/s out");
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = DateTime.Today.Add(TimeSpan.FromSeconds(NetworkController.NetManager_.TimeManager.TIME_display)).ToString("h:mm tt").ToLower();
                var map = "Unknown";
                return string.Concat(" ", time, ", ", map);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = NetworkController.NetManager_.get_GAME_VERSION;
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            //if (!string.IsNullOrEmpty(input)) Commander.execute(CSteamID.Nil, input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
