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
        private ScriptEngine PythonEngine { get; set; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Gets the Python Ext
        /// </summary>
        private PythonExtension PythonExtension { get; set; }

        public override string FileExtension => ".py";

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
        /// Gets a plugin given the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected override Plugin GetPlugin(string filename)
        {
            return new PythonPlugin(filename, PythonEngine, Watcher);
        }

        /// <summary>
        /// Loads a plugin using this loader
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            PythonExtension.InitializeTypes();
            return base.Load(directory, name);
        }
    }
}
