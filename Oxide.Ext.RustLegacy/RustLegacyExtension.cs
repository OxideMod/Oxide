using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.RustLegacy.Plugins;
using Oxide.RustLegacy.Libraries;

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
            Manager.RegisterLibrary("RustLegacy", new Libraries.RustLegacy());

            // Register the OnServerInitialized hook that we can't hook using the IL injector
            var serverinit = UnityEngine.Object.FindObjectOfType<ServerInit>();
            serverinit.gameObject.AddComponent<Ext.RustLegacy.OnServerInitHook>();
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
