using System.Collections.Generic;
using System.IO;

using Jint;

using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.JavaScript.Plugins
{
    /// <summary>
    /// Responsible for loading JavaScript based plugins
    /// </summary>
    public class JavaScriptPluginLoader : PluginLoader
    {
        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        private Engine JavaScriptEngine { get; set; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Initializes a new instance of the JavaScriptPluginLoader class
        /// </summary>
        /// <param name="engine"></param>
        public JavaScriptPluginLoader(Engine engine)
        {
            JavaScriptEngine = engine;
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
            // This might include zip files or folders that contain a number of .js files making up one plugin
            foreach (string file in Directory.GetFiles(directory, "*.js"))
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
            string filename = Path.Combine(directory, name + ".js");

            // Check it exists
            if (!File.Exists(filename)) return null;

            // Create it
            JavaScriptPlugin plugin = new JavaScriptPlugin(filename, JavaScriptEngine, Watcher);
            plugin.Load();

            // Return it
            return plugin;
        }
    }
}
