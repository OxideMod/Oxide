using System;
using System.IO;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Lua.Plugins
{
    /// <summary>
    /// Responsible for loading Lua based plugins
    /// </summary>
    public class LuaPluginLoader : PluginLoader
    {
        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        public NLua.Lua LuaEnvironment { get; private set; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Initialises a new instance of the LuaPluginLoader class
        /// </summary>
        /// <param name="lua"></param>
        public LuaPluginLoader(NLua.Lua lua)
        {
            LuaEnvironment = lua;
        }

        /// <summary>
        /// Returns all plugins in the specified directory by plugin name
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public override IEnumerable<string> ScanDirectory(string directory)
        {
            // For now, we will only load single-file plugins
            // In the future, we might want to accept multi-file plugins
            // This might include zip files or folders that contain a number of lua files making up 1 plugin
            foreach (string file in Directory.GetFiles(directory, "*.lua"))
                yield return Path.GetFileNameWithoutExtension(file);
        }

        /// <summary>
        /// Loads a plugin using this loader
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            // Get the filename
            string filename = Path.Combine(directory, name + ".lua");
            
            // Check it exists
            if (!File.Exists(filename)) return null;

            // Create it
            LuaPlugin plugin = new LuaPlugin(filename, LuaEnvironment, Watcher);
            plugin.Load();

            // Return it
            return plugin;
        }
    }
}
