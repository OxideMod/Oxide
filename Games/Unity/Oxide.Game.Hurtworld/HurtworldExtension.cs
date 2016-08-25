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

        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine", "uLink"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine", "uLink"
        };

        public static string[] Filter =
        {
            ".",
            "Applying hit on",
            "Authorizing player for region",
            "Automove Source item not found",
            "Begin auth session result k_EBeginAuthSessionResultOK",
            "Building proper config for",
            "Built mappings for entity",
            "Client trying to move item in unknown storage:",
            "Deauthorizing player for region",
            "Degenerate triangles might have been generated.",
            "Failed to find hitbox in mappings",
            "Finished writing containers for save, waiting on save thread",
            "Fire went out due to wind",
            "Got validate auth ticket repsonse k_EAuthSessionResponseOK",
            "Hit claim against invalid view",
            "Image Effects are not supported on this platform.",
            "Loading structure with owner",
            "Object out of bounds, destroying",
            "Player denied permission to",
            "Player entity already exists aborting",
            "Player not using",
            "Player requesting spawn.",
            "PointOnEdgeException, perturbating vertices slightly",
            "Riped",
            "Sending structures to client",
            "Source was empty",
            "Syncing tree deltas",
            "System.TypeInitializationException: An exception was thrown by the type initializer for Mono.CSharp.CSharpCodeCompiler",
            "The image effect DefaultCamera",
            "Usually this is not a problem,",
            "Writing to disk completed from background thread"
        };

        /// <summary>
        /// Initializes a new instance of the HurtworldExtension class
        /// </summary>
        /// <param name="manager"></param>
        public HurtworldExtension(ExtensionManager manager) : base(manager)
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

            Interface.Oxide.ServerConsole.Title = () => $"{GameManager.Instance.GetPlayerCount()} | {GameManager.Instance.ServerConfig.GameName}";

            Interface.Oxide.ServerConsole.Status1Left = () => GameManager.Instance.ServerConfig.GameName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{GameManager.Instance.GetPlayerCount()}/{GameManager.Instance.ServerConfig.MaxPlayers} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.NetworkTime.serverTime <= 0) return "not connected";

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (var connection in uLink.Network.connections)
                {
                    var stats = connection.statistics;
                    if (stats == null) continue;

                    bytesReceived += stats.bytesReceivedPerSecond;
                    bytesSent += stats.bytesSentPerSecond;
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = TimeManager.Instance.GetCurrentGameTime();
                var gameTime = $"{(time.Hour > 12 ? (time.Hour - 12) : time.Hour)}:{time.Minute:D2} {(time.Hour >= 12 ? "pm" : "am")}";
                return $"{gameTime}, {GameManager.Instance?.ServerConfig?.Map ?? "Unknown"}";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {GameManager.Instance?.Version} ({GameManager.PROTOCOL_VERSION})";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) ConsoleManager.Instance.ExecuteCommand(input);
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
