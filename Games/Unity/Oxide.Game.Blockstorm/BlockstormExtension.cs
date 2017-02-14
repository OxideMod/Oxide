using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using UnityEngine;

namespace Oxide.Game.Blockstorm
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class BlockstormExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Blockstorm";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

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
            "Buffer size",
            "Clean up after player",
            "Client connected:",
            "Client disconnected:",
            "Client started on port",
            "Connecting to Master Server",
            "Creating character:",
            "Ending auth session",
            "The server has a public address"
        };

        /// <summary>
        /// Initializes a new instance of the BlockstormExtension class
        /// </summary>
        /// <param name="manager"></param>
        public BlockstormExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new BlockstormPluginLoader());
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

            Application.RegisterLogCallback(HandleLog);

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static DedicatedServerConfiguration DedicatedServerConfiguration { get; } = new DedicatedServerConfiguration();

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{FpsMultiplayerGame.instance.playersList.method_5().Count} | {DedicatedServerConfiguration.string_12}";

            Interface.Oxide.ServerConsole.Status1Left = () => DedicatedServerConfiguration.string_12;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{FpsMultiplayerGame.instance.playersList.method_5().Count}/{DedicatedServerConfiguration.int_1} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                return string.Empty; // TODO: Network in/out
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                return string.Empty; // TODO: Server game time, map name
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {Constants.smethod_0()}";
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
