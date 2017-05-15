using System;
using System.IO;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Plugins
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class CSharpExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "CSharp";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public FSWatcher Watcher { get; private set; }

        // The .cs plugin loader
        private CSharpPluginLoader loader;

        /// <summary>
        /// Initializes a new instance of the CSharpExtension class
        /// </summary>
        /// <param name="manager"></param>
        public CSharpExtension(ExtensionManager manager) : base(manager)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) return;
            var extDir = Interface.Oxide.ExtensionDirectory;
            File.WriteAllText(Path.Combine(extDir, "Mono.Posix.dll.config"),
                $"<configuration>\n<dllmap dll=\"MonoPosixHelper\" target=\"{extDir}/x86/libMonoPosixHelper.so\" os=\"!windows,osx\" wordsize=\"32\" />\n" +
                $"<dllmap dll=\"MonoPosixHelper\" target=\"{extDir}/x64/libMonoPosixHelper.so\" os=\"!windows,osx\" wordsize=\"64\" />\n</configuration>");
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            loader = new CSharpPluginLoader(this);
            Manager.RegisterPluginLoader(loader);

            // Register engine frame callback
            Interface.Oxide.OnFrame(OnFrame);

            // Cleanup old files and/or file locations
            Cleanup.Add(Path.Combine(Interface.Oxide.RootDirectory, Environment.OSVersion.Platform == PlatformID.Unix ? "CSharpCompiler.exe" : "CSharpCompiler.x86"));
            Cleanup.Add(Path.Combine(Interface.Oxide.RootDirectory, "CSharpCompiler"));
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public override void LoadPluginWatchers(string pluginDirectory)
        {
            // Register the watcher
            Watcher = new FSWatcher(pluginDirectory, "*.cs");
            Manager.RegisterPluginChangeWatcher(Watcher);
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad() => loader.OnModLoaded();

        public override void OnShutdown()
        {
            base.OnShutdown();
            loader.OnShutdown();
        }

        /// <summary>
        /// Called by engine every server frame
        /// </summary>
        private void OnFrame(float delta)
        {
            var args = new object[] { delta };
            foreach (var kv in loader.LoadedPlugins)
            {
                var plugin = kv.Value as CSharpPlugin;
                if (plugin != null && plugin.HookedOnFrame) plugin.CallHook("OnFrame", args);
            }
        }
    }
}
