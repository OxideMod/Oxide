﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEngine;
using Object = UnityEngine.Object;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Game.RustLegacy.Libraries;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class RustLegacyExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "RustLegacy";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "DestMath", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "System.Core", "UnityEngine", "uLink" };
        public override string[] WhitelistNamespaces => new[] { "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine", "uLink" };

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
        /// Caches the OxideMod.rootconfig field
        /// </summary>
        readonly FieldInfo rootconfig = typeof(OxideMod).GetField("rootconfig", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Caches the OxideMod.commandline field
        /// </summary>
        readonly FieldInfo commandline = typeof(OxideMod).GetField("commandline", BindingFlags.NonPublic | BindingFlags.Instance);

        public class Folders
        {
            public string Source { get; }
            public string Target { get; }

            public Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }

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
            // Register our loader
            Manager.RegisterPluginLoader(new RustLegacyPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("Rust", new Libraries.RustLegacy());

            // Register the OnServerInitialized hook that we can't hook using the IL injector
            var serverinit = Object.FindObjectOfType<ServerInit>();
            serverinit.gameObject.AddComponent<OnServerInitHook>();

            // Check if folder migration is needed
            var config = (OxideConfig)rootconfig.GetValue(Interface.Oxide);
            var cmdline = (CommandLine)commandline.GetValue(Interface.Oxide);
            var rootDirectory = Interface.Oxide.RootDirectory;
            var currentDirectory = Interface.Oxide.InstanceDirectory;
            var fallbackDirectory = Path.Combine(rootDirectory, config.InstanceCommandLines[config.InstanceCommandLines.Length - 1]);
            var oldFallbackDirectory = string.Empty;
            var oxidedir = cmdline.GetVariable("oxidedir");

            if (cmdline.HasVariable("oxidedir"))
                oldFallbackDirectory = Path.Combine(rootDirectory, cmdline.GetVariable("oxidedir"));

            if (!Directory.Exists(oldFallbackDirectory))
                oldFallbackDirectory = Path.Combine(rootDirectory, "save\\oxide");

            if (!Directory.Exists(oldFallbackDirectory)) return;
            if (currentDirectory == oldFallbackDirectory) return;

            // Migrate existing oxide folders from the old fallback directory to the new one
            string[] oxideDirectories = { config.PluginDirectory, config.ConfigDirectory, config.DataDirectory, config.LogDirectory };
            foreach (var dir in oxideDirectories)
            {
                var source = Path.Combine(oldFallbackDirectory, dir);
                var target = Path.Combine(currentDirectory, dir);
                if (Directory.Exists(source))
                {
                    var stack = new Stack<Folders>();
                    stack.Push(new Folders(source, target));

                    while (stack.Count > 0)
                    {
                        var folders = stack.Pop();
                        Directory.CreateDirectory(folders.Target);
                        foreach (var file in Directory.GetFiles(folders.Source, "*"))
                        {
                            var targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                            if (File.Exists(targetFile))
                            {
                                var i = 1;
                                var newTargetFile = targetFile + ".old";
                                while (File.Exists(newTargetFile))
                                {
                                    newTargetFile = targetFile + ".old" + i;
                                    i++;
                                }
                                File.Move(file, newTargetFile);
                            }
                            else
                                File.Move(file, targetFile);
                        }

                        foreach (var folder in Directory.GetDirectories(folders.Source))
                            stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                    }

                    Directory.Delete(source, true);
                }
            }
            Directory.Delete(oldFallbackDirectory, true);
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
            if (!Interface.Oxide.CheckConsole(true) || !Interface.Oxide.EnableConsole(true)) return;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            ConsoleSystem.RegisterLogCallback(HandleLog, true);

            Interface.Oxide.ServerConsole.Title = () => $"{NetCull.connections.Length} | {server.hostname ?? "Unnamed"}";

            Interface.Oxide.ServerConsole.Status1Left = () => string.Concat(" ", server.hostname ?? "Unnamed");
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $" {NetCull.connections.Length}/{NetCull.maxConnections} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (!NetCull.isServerRunning || NetCull.isNotRunning) return "not connected";
                double bytesSent = 0;
                double bytesReceived = 0;
                foreach (var connection in NetCull.connections)
                {
                    var stats = connection.statistics;
                    if (stats == null) continue;
                    bytesSent += stats.bytesSentPerSecond;
                    bytesReceived += stats.bytesReceivedPerSecond;
                }
                return string.Concat(Utility.FormatBytes(bytesReceived), "/s in, ", Utility.FormatBytes(bytesSent), "/s out");
            };

            Interface.Oxide.ServerConsole.Status3Left = () => $" {EnvironmentControlCenter.Singleton?.GetTime().ToString() ?? "Unknown"}, {(server.pvp ? "PvP" : "PvE")}";
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {Rust.Defines.Connection.protocol}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;

            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (!input.Contains(".")) input = string.Concat("global.", input);
                return Command.consoleCommands.Where(c => c.Key.StartsWith(input.ToLower())).ToList().ConvertAll(c => c.Key).ToArray();
            };
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) ConsoleSystem.Run(input, true);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
