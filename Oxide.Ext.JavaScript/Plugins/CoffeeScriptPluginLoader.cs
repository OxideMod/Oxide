using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Jint;
using Jint.Parser;

using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.JavaScript.Plugins
{
    /// <summary>
    /// Responsible for loading CoffeeScript based plugins
    /// </summary>
    public class CoffeeScriptPluginLoader : PluginLoader
    {
        const string compilerResourcePath = "Oxide.Ext.JavaScript.Resources.coffee-script.js";

        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        private Engine JavaScriptEngine { get; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        /// <summary>
        /// Initializes a new instance of the CoffeeScriptPluginLoader class
        /// </summary>
        /// <param name="engine"></param>
        public CoffeeScriptPluginLoader(Engine engine)
        {
            JavaScriptEngine = engine;
            
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(compilerResourcePath))
                using (StreamReader reader = new StreamReader(stream))
                    engine.Execute(reader.ReadToEnd(), new ParserOptions { Source = "CoffeeScriptCompiler" });
            engine.Execute("function __CompileScript(name){return CoffeeScript.compile(name+\"=\\n\"+__CoffeeSource.replace(/^/gm, '  '),{bare: true})}");
        }

        /// <summary>
        /// Returns all plugins in the specified directory by plugin name
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public override IEnumerable<string> ScanDirectory(string directory)
        {
            return Directory.GetFiles(directory, "*.coffee").Select(Path.GetFileNameWithoutExtension);
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
            string filename = Path.Combine(directory, name + ".coffee");

            // Check it exists
            if (!File.Exists(filename)) return null;

            // Create it
            var plugin = new CoffeeScriptPlugin(filename, JavaScriptEngine, Watcher);
            plugin.Load();

            // Return it
            return plugin;
        }
    }
}
