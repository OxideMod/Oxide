using System.IO;

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
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "CSharp";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public FSWatcher Watcher { get; private set; }

        private CSharpPluginLoader loader;

        /// <summary>
        /// Initializes a new instance of the CSharpExtension class
        /// </summary>
        /// <param name="manager"></param>
        public CSharpExtension(ExtensionManager manager) : base(manager)
        {
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

            Cleanup.Add(Path.Combine(Interface.Oxide.RootDirectory, "mono-2.0.dll"));
            Cleanup.Add(Path.Combine(Interface.Oxide.RootDirectory, "msvcr120.dll"));
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="PluginDirectory"></param>
        public override void LoadPluginWatchers(string PluginDirectory)
        {
            // Register the watcher
            Watcher = new FSWatcher(PluginDirectory, "*.cs");
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
