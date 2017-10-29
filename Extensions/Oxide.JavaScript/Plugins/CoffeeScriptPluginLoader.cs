using Jint;
using Jint.Parser;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using System.IO;
using System.Reflection;

namespace Oxide.Core.JavaScript.Plugins
{
    /// <summary>
    /// Responsible for loading CoffeeScript based plugins
    /// </summary>
    public class CoffeeScriptPluginLoader : PluginLoader
    {
        private const string compilerResourcePath = "Oxide.Core.JavaScript.Resources.coffee-script.js";

        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        private Engine JavaScriptEngine { get; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        public override string FileExtension => ".coffee";

        /// <summary>
        /// Initializes a new instance of the CoffeeScriptPluginLoader class
        /// </summary>
        /// <param name="engine"></param>
        public CoffeeScriptPluginLoader(Engine engine)
        {
            JavaScriptEngine = engine;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(compilerResourcePath))
            {
                using (var reader = new StreamReader(stream))
                    engine.Execute(reader.ReadToEnd(), new ParserOptions { Source = "CoffeeScriptCompiler" });
            }
            engine.Execute("function __CompileScript(name){return CoffeeScript.compile(name+\"=\\n\"+__CoffeeSource.replace(/^/gm, '  '),{bare: true})}");
        }

        /// <summary>
        /// Gets a plugin given the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected override Plugin GetPlugin(string filename) => new CoffeeScriptPlugin(filename, JavaScriptEngine, Watcher);
    }
}
