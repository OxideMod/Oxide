using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.RustLegacy.Libraries;
using Oxide.RustLegacy.Plugins;

namespace Oxide.RustLegacy
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class RustLegacyExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "RustLegacy"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, OxideMod.Version.Patch); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Oxide Team"; } }

        public override string[] WhitelistAssemblies { get { return new[] { "Assembly-CSharp", "DestMath", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "System.Core", "UnityEngine" }; } }
        public override string[] WhitelistNamespaces { get { return new[] { "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "UnityEngine" }; } }

        /// <summary>
        /// Caches the OxideMod.rootconfig field
        /// </summary>
        FieldInfo rootconfig = typeof(OxideMod).GetField("rootconfig", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Caches the OxideMod.commandline field
        /// </summary>
        FieldInfo commandline = typeof(OxideMod).GetField("commandline", BindingFlags.NonPublic | BindingFlags.Instance);

        public class Folders
        {
            public string Source { get; private set; }
            public string Target { get; private set; }

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
        /// <param name="manager"></param>
        public override void Load()
        {
            IsGameExtension = true;

            // Register our loader
            Manager.RegisterPluginLoader(new RustLegacyPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("Rust", new Libraries.RustLegacy());

            // Register the OnServerInitialized hook that we can't hook using the IL injector
            var serverinit = UnityEngine.Object.FindObjectOfType<ServerInit>();
            serverinit.gameObject.AddComponent<Ext.RustLegacy.OnServerInitHook>();

            // Check if folder migration is needed
            var config = (Core.Configuration.OxideConfig)rootconfig.GetValue(Interface.Oxide);
            var cmdline = (CommandLine)commandline.GetValue(Interface.Oxide);
            string rootDirectory = Interface.Oxide.RootDirectory;
            string currentDirectory = Interface.Oxide.InstanceDirectory;
            string fallbackDirectory = Path.Combine(rootDirectory, config.InstanceCommandLines[config.InstanceCommandLines.Length - 1]);
            
            string oldFallbackDirectory = string.Empty;
            string oxidedir = cmdline.GetVariable("oxidedir");

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
                            if (File.Exists(targetFile))
                            {
                                int i = 1;
                                string newTargetFile = targetFile + ".old";
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
        /// <param name="manager"></param>
        public override void OnModLoad()
        {

        }
    }
}
