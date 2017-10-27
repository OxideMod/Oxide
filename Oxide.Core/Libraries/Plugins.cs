using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The plugins library
    /// </summary>
    public class Plugins : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets the plugin manager
        /// </summary>
        public PluginManager PluginManager { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Plugins library
        /// </summary>
        public Plugins(PluginManager pluginmanager)
        {
            PluginManager = pluginmanager;
        }

        /// <summary>
        /// Returns if a plugin has been loaded by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("Exists")]
        public bool Exists(string name) => PluginManager.GetPlugin(name) != null;

        /// <summary>
        /// Returns the object of a loaded plugin with the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("Find")]
        public Plugin Find(string name) => PluginManager.GetPlugin(name);

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [LibraryFunction("CallHook")]
        public object CallHook(string hookname, params object[] args) => Interface.Call(hookname, args);

        /// <summary>
        /// Gets an array of all currently loaded plugins
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetAll")]
        public Plugin[] GetAll() => PluginManager.GetPlugins().ToArray();
    }
}
