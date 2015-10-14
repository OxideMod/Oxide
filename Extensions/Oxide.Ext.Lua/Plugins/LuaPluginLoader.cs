using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.Lua.Plugins
{
    /// <summary>
    /// Responsible for loading Lua based plugins
    /// </summary>
    public class LuaPluginLoader : PluginLoader
    {
        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        private NLua.Lua LuaEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Gets the Lua Extension
        /// </summary>
        private LuaExtension LuaExtension { get; set; }

        public override string FileExtension => ".lua";

        /// <summary>
        /// Initializes a new instance of the LuaPluginLoader class
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="luaExtension"></param>
        public LuaPluginLoader(NLua.Lua lua, LuaExtension luaExtension)
        {
            LuaEnvironment = lua;
            LuaExtension = luaExtension;
        }

        /// <summary>
        /// Gets a plugin given the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected override Plugin GetPlugin(string filename)
        {
            return new LuaPlugin(filename, LuaExtension, Watcher);
        }

        /// <summary>
        /// Loads a plugin using this loader
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            LuaExtension.InitializeTypes();
            return base.Load(directory, name);
        }
    }
}
