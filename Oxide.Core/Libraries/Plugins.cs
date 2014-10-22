using System;

using Oxide.Core.Plugins;

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
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Gets the plugin manager
        /// </summary>
        public PluginManager PluginManager { get; private set; }

        /// <summary>
        /// Initialises a new instance of the Plugins library
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
        public bool Exists(string name)
        {
            return PluginManager.GetPlugin(name) != null;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [LibraryFunction("CallHook")]
        public object CallHook(string hookname, object[] args)
        {
            return Interface.CallHook(hookname, args);
        }
    }
}
