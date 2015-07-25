using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Game.RustLegacy.Libraries;

using UnityEngine;

using Object = UnityEngine.Object;

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
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "DestMath", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "System.Core", "UnityEngine", "uLink" };
        public override string[] WhitelistNamespaces => new[] { "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine", "uLink" };

        private static readonly string[] Filter =
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
        public RustLegacyExtension(ExtensionManager manager)
            : base(manager)
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
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {

        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            /*if (!Interface.Oxide.CheckConsole(true)) return;
            var obj = UnityEngine.Object.FindObjectsOfType<LibRust>();
            if (obj.Length > 0)
            {
                obj[0].enabled = false;
                LibRust.Shutdown();
            }
            if (!Interface.Oxide.EnableConsole(true)) return;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            ConsoleSystem.RegisterLogCallback(HandleLog, true);*/
        }

        private void ServerConsoleOnInput(string input)
        {
            ConsoleSystem.Run(input, true);
        }

        private void HandleLog(string message, string stackTrace, LogType type)
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
