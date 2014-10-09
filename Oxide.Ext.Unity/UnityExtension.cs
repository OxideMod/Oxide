using System;

using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.Unity.Plugins;

namespace Oxide.Unity
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class UnityExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "Unity"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, 0); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Oxide Team"; } }

        /// <summary>
        /// Initialises a new instance of the RustExtension class
        /// </summary>
        /// <param name="manager"></param>
        public UnityExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        /// <param name="manager"></param>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new UnityPluginLoader());

            // Register our libraries
            
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
