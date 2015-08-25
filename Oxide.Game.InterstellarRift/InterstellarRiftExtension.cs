using Oxide.Core;
using Oxide.Core.Extensions;

using Game.Configuration;

namespace Oxide.Game.InterstellarRift
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class InterstellarRiftExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "InterstellarRift";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "mscorlib", "Oxide.Core", "System", "System.Core" };
        public override string[] WhitelistNamespaces => new[] { "System.Collections", "System.Security.Cryptography", "System.Text" };

        private static readonly string[] Filter =
        {
            ""
        };

        /// <summary>
        /// Initializes a new instance of the InterstellarRiftExtension class
        /// </summary>
        /// <param name="manager"></param>
        public InterstellarRiftExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new InterstellarRiftPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Rift", new Libraries.InterstellarRift());
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
            if (!Interface.Oxide.EnableConsole()) return;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var hostname = Config.m_singleton.ServerName;
                return string.Concat(hostname);
            };

            // TODO: Add console log handling
            // TODO: Add status information
        }
    }
}
