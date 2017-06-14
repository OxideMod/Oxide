﻿using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using Oxide.Game.RustLegacy.Libraries;
using UnityEngine;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class RustLegacyExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "RustLegacy";

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
            "Assembly-CSharp", "DestMath", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "System.Core", "UnityEngine", "uLink"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "System.Security.Cryptography",
            "System.Text", "UnityEngine", "uLink"
        };

        public static string[] Filter =
        {
            "Server DataDir",
            "Server configuration loaded from",
            "HDR RenderTexture",
            "The referenced script on this",
            "Instantiator for prefab",
            "Main camera does not exist or is not tagged",
            "Loaded \"rust_island_2013\""
        };

        /// <summary>
        /// Initializes a new instance of the RustExtension class
        /// </summary>
        /// <param name="manager"></param>
        public RustLegacyExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterPluginLoader(new RustLegacyPluginLoader());

            // Register the OnServerInitialized hook that we can't hook using the IL injector
            var serverinit = UnityEngine.Object.FindObjectOfType<ServerInit>();
            serverinit.gameObject.AddComponent<OnServerInitHook>();
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
            if (!Interface.Oxide.EnableConsole(true)) return;

            ConsoleSystem.RegisterLogCallback(HandleLog, true);

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;

                if (!input.Contains(".")) input = string.Concat("global.", input);
                var native = Command.DefaultCommands.Where(c => c.StartsWith(input.ToLower())).ToArray();
                var oxide = Command.ConsoleCommands.Where(c => c.Key.StartsWith(input.ToLower())).ToList().ConvertAll(c => c.Key).ToArray();
                return native.Concat(oxide).ToArray();
            };
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{NetCull.connections.Length} | {server.hostname ?? "Unnamed"}";

            Interface.Oxide.ServerConsole.Status1Left = () => server.hostname ?? "Unnamed";
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{NetCull.connections.Length}/{NetCull.maxConnections} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (!NetCull.isServerRunning || NetCull.isNotRunning) return "not connected";

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (var connection in NetCull.connections)
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
                var gameTime = DateTime.Today.AddHours(EnvironmentControlCenter.Singleton.GetTime()).ToString("hh:mm tt");
                return $"{gameTime.ToLower()}, {(server.pvp ? "PvP" : "PvE")}";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {Rust.Defines.Connection.protocol}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) ConsoleSystem.Run(input, true);
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
