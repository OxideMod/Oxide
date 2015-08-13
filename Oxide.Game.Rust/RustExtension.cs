using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Facepunch;
using Network;
using Rust;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Game.Rust.Libraries;

using UnityEngine;

namespace Oxide.Game.Rust
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class RustExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Rust";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "Assembly-CSharp-firstpass", "DestMath", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "ConVar", "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine" };

        private static readonly string[] Filter =
        {
            "AngryAnt Behave version",
            "HDR RenderTexture format is not supported on this platform.",
            "Image Effects are not supported on this platform.",
            "The image effect Main Camera",
            "The image effect effect -",
            "Unable to find shaders",
            "Unsupported encoding: 'utf8'",
            "[SpawnHandler] populationCounts"
        };

        /// <summary>
        /// Caches the OxideMod.rootconfig field
        /// </summary>
        readonly FieldInfo rootconfig = typeof(OxideMod).GetField("rootconfig", BindingFlags.NonPublic | BindingFlags.Instance);

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
        public RustExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new RustPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("Rust", new Libraries.Rust());

            // Check if folder migration is needed
            var config = (OxideConfig)rootconfig.GetValue(Interface.Oxide);
            string rootDirectory = Interface.Oxide.RootDirectory;
            string currentDirectory = Interface.Oxide.InstanceDirectory;
            string fallbackDirectory = Path.Combine(rootDirectory, config.InstanceCommandLines[config.InstanceCommandLines.Length - 1]);
            string oldFallbackDirectory = Path.Combine(rootDirectory, "server");

            if (!Directory.Exists(oldFallbackDirectory)) return;
            if (currentDirectory == oldFallbackDirectory) return;

            // Migrate existing oxide folders from the old fallback directory to the new one
            string[] oxideDirectories = { config.PluginDirectory, config.ConfigDirectory, config.DataDirectory, config.LogDirectory };
            foreach (var dir in oxideDirectories)
            {
                string source = Path.Combine(oldFallbackDirectory, dir);
                string target = Path.Combine(currentDirectory, dir);

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
                            string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                            if (File.Exists(targetFile) && File.Exists(file))
                            {
                                int i = 1;
                                string newTargetFile = targetFile + ".old";
                                while (File.Exists(newTargetFile)) {
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

        }

        internal static void EnableConsole()
        {
            if (!Interface.Oxide.CheckConsole(true) || !Interface.Oxide.EnableConsole()) return;

            Output.OnMessage += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var players = BasePlayer.activePlayerList.Count;
                var hostname = ConVar.Server.hostname;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                var hostname = ConVar.Server.hostname;
                return string.Concat(" ", hostname);
            };
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Performance.frameRate;
                var uptime = Number.FormatSeconds((int)Time.realtimeSinceStartup);
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = BasePlayer.activePlayerList.Count;
                var playerLimit = (Net.sv == null ? 0 : Net.sv.maxConnections);
                var sleeperCount = BasePlayer.sleepingPlayerList.Count;
                var sleepers = sleeperCount + (sleeperCount.Equals(1) ? " sleeper" : " sleepers");
                var entitiesCount = BaseNetworkable.serverEntities.Count;
                var entities = entitiesCount + (entitiesCount.Equals(1) ? " entity" : " entities");
                return string.Concat(" ", players, "/", playerLimit, " players, ", sleepers, ", ", entities);
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (Net.sv == null || !Net.sv.IsConnected()) return "not connected";
                var inbound = Number.FormatMemoryShort(Net.sv.GetStat(null, NetworkPeer.StatTypeLong.BytesReceived_LastSecond));
                var outbound = Number.FormatMemoryShort(Net.sv.GetStat(null, NetworkPeer.StatTypeLong.BytesSent_LastSecond));
                return string.Concat(inbound, "/s in, ", outbound, "/s out");
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var gameTime = (!TOD_Sky.Instance ? DateTime.Now : TOD_Sky.Instance.Cycle.DateTime).ToString("h:mm tt").ToLower();
                return string.Concat(" ", gameTime, ", ", ConVar.Server.level, " [", ConVar.Server.worldsize, ", ", ConVar.Server.seed, "]");
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = typeof(Protocol).GetField("network").GetValue(null).ToString();
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for Protocol ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;

            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (!input.Contains(".")) input = string.Concat("global.", input);
                return ConsoleSystem.Index.GetAll().Where(c => c.namefull.StartsWith(input.ToLower())).ToList().ConvertAll(c => c.namefull).ToArray();
            };
        }

        private static void ServerConsoleOnInput(string input)
        {
            ConsoleSystem.Run.Server.Normal(input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;
            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
            {
                color = ConsoleColor.Yellow;
                ConVar.Server.Log("Log.Warning.txt", message);
            }
            else if (type == LogType.Error)
            {
                color = ConsoleColor.Red;
                ConVar.Server.Log("Log.Error.txt", message);
            }
            else if (!message.StartsWith("[CHAT]"))
                ConVar.Server.Log("Log.Log.txt", message);
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
