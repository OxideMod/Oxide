using System;
using System.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.Game.Hurtworld.Libraries;

namespace Oxide.Game.Hurtworld
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class HurtworldExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Hurtworld";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine" };

        private static readonly string[] Filter =
        {
            ".",
            "adding player to group",
            "Built mappings for entity",
            "Building proper config for",
            "Image Effects are not supported on this platform.",
            "Loading structure with owner",
            "Riped",
            "Syncing tree deltas",
            "The image effect DefaultCamera"
        };

        /// <summary>
        /// Initializes a new instance of the HurtworldExtension class
        /// </summary>
        /// <param name="manager"></param>
        public HurtworldExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new HurtworldPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("Hurt", new Libraries.Hurtworld());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
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
                if (GameManager.Instance == null) return string.Empty;
                var players = GameManager.Instance.GetIdentifierMap().Count(x => x.Value.IsConnected);
                var hostname = GameManager.Instance.ServerConfig.GameName;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                if (GameManager.Instance == null) return string.Empty;
                var hostname = GameManager.Instance.ServerConfig.GameName;
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
                if (GameManager.Instance == null) return string.Empty;
                var players = GameManager.Instance.GetIdentifierMap().Count(x => x.Value.IsConnected);
                var playerLimit = GameManager.Instance.ServerConfig.MaxPlayers;
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
                var time = EnvironmentManager.Instance.CurrentGameTime;
                var gameTime = $"{(time.Hour > 12 ? (time.Hour - 12) : time.Hour)}:{time.Minute:D2} {(time.Hour >= 12 ? "pm" : "am")}";
                var map = GameManager.Instance?.ServerConfig?.Map ?? "Unknown";
                return string.Concat(" ", gameTime, ", ", map);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = GameManager.PROTOCOL_VERSION.ToString();
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for Version ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            ConsoleManager.Instance.ExecuteCommand(input);
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
