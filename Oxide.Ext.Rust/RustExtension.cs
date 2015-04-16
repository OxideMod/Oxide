using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.Rust.Plugins;
using Oxide.Rust.Libraries;

namespace Oxide.Rust
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class RustExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "Rust"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, OxideMod.Version.Patch); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Oxide Team"; } }

        /// <summary>
        /// Caches the OxideMod.rootconfig field
        /// </summary>
        FieldInfo rootconfig = typeof(OxideMod).GetField("rootconfig", BindingFlags.NonPublic | BindingFlags.Instance);

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
        public RustExtension(ExtensionManager manager)
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
            Manager.RegisterPluginLoader(new RustPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("Rust", new Rust.Libraries.Rust());

            // Check if folder migration is needed
            var config = (Core.Configuration.OxideConfig)rootconfig.GetValue(Interface.Oxide);
            string rootDirectory = Interface.Oxide.RootDirectory;
            string currentDirectory = Interface.Oxide.InstanceDirectory;
            string fallbackDirectory = Path.Combine(rootDirectory, config.InstanceCommandLines[config.InstanceCommandLines.Length - 1]);
            string oldFallbackDirectory = Path.Combine(rootDirectory, "server");

            if (!Directory.Exists(oldFallbackDirectory)) return;
            if (currentDirectory == oldFallbackDirectory) return;

            // Migrate existing oxide folders from the old fallback directory to the new one
            string[] oxideDirectories = { config.PluginDirectory, config.ConfigDirectory, config.DataDirectory, config.LogDirectory, config.TempDirectory };
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
        /// <param name="manager"></param>
        public override void OnModLoad()
        {

        }
    }
}
