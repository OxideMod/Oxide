using System.Collections.Generic;

using Oxide.Core.Plugins;

namespace Oxide.Rust.Plugins
{
    /// <summary>
    /// Responsible for loading Rust specific plugins
    /// </summary>
    public class RustPluginLoader : PluginLoader
    {
        /// <summary>
        /// Returns all plugins in the specified directory by plugin name
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public override IEnumerable<string> ScanDirectory(string directory)
        {
            // Just return the names of our fixed plugins
            return new string[] { "rustcore" };
        }

        /// <summary>
        /// Loads a plugin using this loader
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            // Switch on the plugin name
            switch (name)
            {
                case "rustcore":
                    return new RustCore();
                default:
                    return null;
            }
        }
    }
}
