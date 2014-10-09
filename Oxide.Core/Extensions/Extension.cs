using System;

namespace Oxide.Core.Extensions
{
    /// <summary>
    /// Represents a single binary extension
    /// </summary>
    public abstract class Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public abstract VersionNumber Version { get; }

        /// <summary>
        /// Gets the extension manager responsible for this extension
        /// </summary>
        public ExtensionManager Manager { get; private set; }

        /// <summary>
        /// Initialises a new instance of the Extension class
        /// </summary>
        /// <param name="manager"></param>
        public Extension(ExtensionManager manager)
        {
            Manager = manager;
        }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public abstract string Author { get; }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// Loads any plugin watchers pertinent to this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public abstract void LoadPluginWatchers(string plugindir);

        /// <summary>
        /// Called after all other extensions have been loaded
        /// </summary>
        /// <param name="manager"></param>
        public abstract void OnModLoad();
    }
}
