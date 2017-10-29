using Jint;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Core.JavaScript.Plugins
{
    /// <summary>
    /// Responsible for loading JavaScript based plugins
    /// </summary>
    public class JavaScriptPluginLoader : PluginLoader
    {
        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        private Engine JavaScriptEngine { get; }

        /// <summary>
        /// Gets or sets the watcher
        /// </summary>
        public FSWatcher Watcher { get; set; }

        public override string FileExtension => ".js";

        /// <summary>
        /// Initializes a new instance of the JavaScriptPluginLoader class
        /// </summary>
        /// <param name="engine"></param>
        public JavaScriptPluginLoader(Engine engine)
        {
            JavaScriptEngine = engine;
        }

        /// <summary>
        /// Gets a plugin given the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected override Plugin GetPlugin(string filename) => new JavaScriptPlugin(filename, JavaScriptEngine, Watcher);
    }
}
