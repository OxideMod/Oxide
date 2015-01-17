using System.Collections.Generic;
using System.IO;

using Microsoft.Scripting.Hosting;

using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.Python.Plugins
{
    /// <summary>
    /// Responsible for loading Python based plugins
    /// </summary>
    public class PythonPluginLoader : PluginLoader
    {
        /// <summary>
        /// Gets the Python engine
        /// </summary>
        public ScriptEngine PythonEngine { get; private set; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Initialises a new instance of the PythonPluginLoader class
        /// </summary>
        /// <param name="engine"></param>
        public PythonPluginLoader(ScriptEngine engine)
        {
            PythonEngine = engine;
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
            // This might include zip files or folders that contain a number of .py files making up one plugin
            foreach (string file in Directory.GetFiles(directory, "*.py"))
                yield return Path.GetFileNameWithoutExtension(file);
        }

        /// <summary>
        /// Loads a plugin using this loader
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            // Get the filename
            string filename = Path.Combine(directory, name + ".py");
            
            // Check it exists
            if (!File.Exists(filename)) return null;

            // Create it
            PythonPlugin plugin = new PythonPlugin(filename, PythonEngine, Watcher);
            plugin.Load();

            // Return it
            return plugin;
        }
    }
}
