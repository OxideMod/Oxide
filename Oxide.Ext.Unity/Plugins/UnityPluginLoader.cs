using System;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Unity.Plugins
{
    /// <summary>
    /// Responsible for loading Unity specific plugins
    /// </summary>
    public class UnityPluginLoader : PluginLoader
    {
        /// <summary>
        /// Returns all plugins in the specified directory by plugin name
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public override IEnumerable<string> ScanDirectory(string directory)
        {
            // Just return the names of our fixed plugins
            return new string[] { "unitycore" };
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
                case "unitycore":
                    return new UnityCore();
                default:
                    return null;
            }
        }
    }
}
