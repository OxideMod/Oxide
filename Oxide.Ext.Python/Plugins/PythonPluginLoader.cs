using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private ScriptEngine PythonEngine { get; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Gets the Python Ext
        /// </summary>
        private PythonExtension PythonExtension { get; }

        /// <summary>
        /// Initializes a new instance of the PythonPluginLoader class
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="pythonExtension"></param>
        public PythonPluginLoader(ScriptEngine engine, PythonExtension pythonExtension)
        {
            PythonEngine = engine;
            PythonExtension = pythonExtension;
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
            return Directory.GetFiles(directory, "*.py").Select(Path.GetFileNameWithoutExtension);
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

            PythonExtension.InitializeTypes();

            // Create it
            var plugin = new PythonPlugin(filename, PythonEngine, Watcher);
            plugin.Load();

            // Return it
            return plugin;
        }
    }
}
