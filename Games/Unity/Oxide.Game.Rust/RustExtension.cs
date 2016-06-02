using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Facepunch;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Game.Rust.Libraries;

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
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "Assembly-CSharp-firstpass", "DestMath", "Facepunch.Network", "Facepunch.System", "Facepunch.UnityEngine",
            "mscorlib", "Oxide.Core", "Oxide.Game.Rust", "protobuf-net", "RustBuild", "Rust.Data", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "ConVar", "Dest", "Facepunch", "Network", "Oxide.Game.Rust.Cui", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections",
            "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            "AngryAnt Behave version",
            "Failed to load plugin '1' (no source found)",
            "HDR RenderTexture format is not supported on this platform.",
            "Image Effects are not supported on this platform.",
            "Missing projectileID",
            "The image effect Main Camera",
            "The image effect effect -",
            "Unable to find shaders",
            "Unsupported encoding: 'utf8'",
            "[AmplifyColor] This image effect is not supported on this platform.",
            "[AmplifyOcclusion]",
            "[SpawnHandler] populationCounts"
        };

        /// <summary>
        /// Caches the OxideMod.rootconfig field
        /// </summary>
        private readonly FieldInfo rootconfig = typeof(OxideMod).GetField("rootconfig", BindingFlags.NonPublic | BindingFlags.Instance);

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
        public RustExtension(ExtensionManager manager) : base(manager)
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
            var rootDirectory = Interface.Oxide.RootDirectory;
            var currentDirectory = Interface.Oxide.InstanceDirectory;
            var fallbackDirectory = Path.Combine(rootDirectory, config.InstanceCommandLines[config.InstanceCommandLines.Length - 1]);
            var oldFallbackDirectory = Path.Combine(rootDirectory, "server");

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
                            if (file == null) continue;
                            var targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                            if (File.Exists(targetFile) && File.Exists(file))
                            {
                                var i = 1;
                                var newTargetFile = targetFile + ".old";
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
                            if (folder != null) stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                    }

                    Directory.Delete(source, true);
                }
            }
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

            Output.OnMessage += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (!input.Contains(".")) input = string.Concat("global.", input);
                return ConsoleSystem.Index.GetAll().Where(c => c.namefull.StartsWith(input.ToLower())).ToList().ConvertAll(c => c.namefull).ToArray();
            };
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) ConsoleSystem.Run.Server.Normal(input);
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
            else if (type == LogType.Exception || type == LogType.Assert)
            {
                color = ConsoleColor.Red;
                ConVar.Server.Log("Log.Exception.txt", message);
            }
            else if (type == LogType.Assert)
            {
                color = ConsoleColor.Red;
                ConVar.Server.Log("Log.Assert.txt", message);
            }
            else if (!message.StartsWith("[CHAT]"))
                ConVar.Server.Log("Log.Log.txt", message);
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
