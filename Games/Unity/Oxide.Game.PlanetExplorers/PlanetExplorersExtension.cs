using System;
using System.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.PlanetExplorers
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class PlanetExplorersExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "PlanetExplorers";

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
            "Can not be repeated to create",
            "Packet head already exists",
            "no last name can be used by",
            "save currentVersion colonyNpc",
            "virtual funtion is running!!!"
        };

        /// <summary>
        /// Initializes a new instance of the PlanetExplorersExtension class
        /// </summary>
        /// <param name="manager"></param>
        public PlanetExplorersExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new PlanetExplorersPluginLoader());
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

            Interface.Oxide.ServerConsole.Title = () => $"{uLink.Network.connections.Length} | {ServerConfig.ServerName}";

            Interface.Oxide.ServerConsole.Status1Left = () => ServerConfig.ServerName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{uLink.Network.connections.Length}/{ServerConfig.MaxConnections} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.Network.time <= 0) return "not connected";

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (var connection in uLink.Network.connections)
                {
                    var stats = connection.statistics;
                    if (stats == null) continue;

                    bytesReceived += stats.bytesReceivedPerSecond;
                    bytesSent += stats.bytesSentPerSecond;
                }
                return $"{Core.Utility.FormatBytes(bytesReceived)}/s in, {Core.Utility.FormatBytes(bytesSent)}/s out";
            };

            /*Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = TimeManager.Instance.GetCurrentGameTime();
                var gameTime = $"{(time.Hour > 12 ? (time.Hour - 12) : time.Hour)}:{time.Minute:D2} {(time.Hour >= 12 ? "pm" : "am")}";
                var map = GameManager.Instance?.ServerConfig?.Map ?? "Unknown";
                return string.Concat(" ", gameTime, ", ", map);
            };*/
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {ServerConfig.ServerVersion}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Handle console input
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
