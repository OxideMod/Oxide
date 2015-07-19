using System.IO;

using Jint;
using Jint.Parser;

using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.JavaScript.Plugins
{
    /// <summary>
    /// Represents a JavaScript plugin
    /// </summary>
    public class CoffeeScriptPlugin : JavaScriptPlugin
    {
        /// <summary>
        /// Initializes a new instance of the CoffeeScriptPlugin class
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="engine"></param>
        /// <param name="watcher"></param>
        internal CoffeeScriptPlugin(string filename, Engine engine, FSWatcher watcher) : base(filename, engine, watcher)
        {

        }

        protected override void LoadSource()
        {
            Name = Path.GetFileNameWithoutExtension(Filename);
            var source = File.ReadAllText(Filename);
            JavaScriptEngine.SetValue("__CoffeeSource", source);
            JavaScriptEngine.Execute($"eval(__CompileScript('{Name}'))", new ParserOptions { Source = Path.GetFileName(Filename) });
        }
    }
}
