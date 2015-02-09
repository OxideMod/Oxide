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
        public override string Name { get { return "CSharp"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(0, 6, 0); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "bawNg"; } }

        public FSWatcher Watcher { get; private set; }

        private CSharpPluginLoader loader;

        /// <summary>
        /// Initialises a new instance of the CSharpExtension class
        /// </summary>
        /// <param name="manager"></param>
        public CSharpExtension(ExtensionManager manager) : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        /// <param name="manager"></param>
        public override void Load()
        {
            // Register our loader
            loader = new CSharpPluginLoader(this);
            Manager.RegisterPluginLoader(loader);
            // Register engine frame callback
            Interface.GetMod().OnFrame(OnFrame);
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
        /// <param name="manager"></param>
        public override void OnModLoad()
        {

        }

        /// <summary>
        /// Called by engine every server frame
        /// </summary>
        private void OnFrame()
        {
            foreach (var plugin in loader.LoadedPlugins)
                if (plugin.HookedOnFrame) plugin.CallHook("OnFrame", null);
        }
    }
}
