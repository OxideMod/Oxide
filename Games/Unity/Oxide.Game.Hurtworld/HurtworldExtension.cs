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

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine", "uLink" };
        public override string[] WhitelistNamespaces => new[] { "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine", "uLink" };

        public static string[] Filter =
        {
            ".",
            "Applying hit on",
            "Authorizing player for region",
            "Automove Source item not found",
            "Begin auth session result k_EBeginAuthSessionResultOK",
            "Building proper config for",
            "Built mappings for entity",
            "Deauthorizing player for region",
            "Finished writing containers for save, waiting on save thread",
            "Got validate auth ticket repsonse k_EAuthSessionResponseOK",
            "Hit claim against invalid view",
            "Image Effects are not supported on this platform.",
            "Loading structure with owner",
            "Object out of bounds, destroying",
            "Player denied permission to",
            "Player not using",
            "Player requesting spawn.",
            "Riped",
            "Sending structures to client",
            "Source was empty",
            "Syncing tree deltas",
            "The image effect DefaultCamera",
            "Writing to disk completed from background thread"
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
                var players = GameManager.Instance.GetPlayerCount();
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
                var players = GameManager.Instance.GetPlayerCount();
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
                var time = TimeManager.Instance.GetCurrentGameTime();
                var gameTime = $"{(time.Hour > 12 ? (time.Hour - 12) : time.Hour)}:{time.Minute:D2} {(time.Hour >= 12 ? "pm" : "am")}";
                var map = GameManager.Instance?.ServerConfig?.Map ?? "Unknown";
                return string.Concat(" ", gameTime, ", ", map);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = GameManager.Instance?.GetProtocolVersion().ToString();
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input) => ConsoleManager.Instance.ExecuteCommand(input);

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
