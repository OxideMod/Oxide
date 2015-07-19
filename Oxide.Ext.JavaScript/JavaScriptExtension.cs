using System;
using System.Linq;
using System.Reflection;

using Jint;
using Jint.Native;
using Jint.Native.Error;
using Jint.Native.Object;
using Jint.Parser;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

using Microsoft.Scripting.Ast;

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Logging;
using Oxide.Core.Plugins.Watchers;
using Oxide.Ext.JavaScript.Libraries;
using Oxide.Ext.JavaScript.Plugins;

namespace Oxide.Ext.JavaScript
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class JavaScriptExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "JavaScript";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        private Engine JavaScriptEngine { get; set; }
        
        // The js plugin loader
        private JavaScriptPluginLoader loader;

        // The coffee plugin loader
        private CoffeeScriptPluginLoader coffeeLoader;

        /// <summary>
        /// Initializes a new instance of the JavaScript class
        /// </summary>
        /// <param name="manager"></param>
        public JavaScriptExtension(ExtensionManager manager)
            : base(manager)
        {
            ExceptionHandler.RegisterType(typeof(JavaScriptException), ex =>
            {
                var jintEx = (JavaScriptException) ex;
                var obj = jintEx.Error.ToObject() as ErrorInstance;
                if (obj != null) return string.Format("File: {0} Line: {1} Column: {2} {3} {4}:{5}{6}", jintEx.Location.Source, jintEx.LineNumber, jintEx.Column, obj.Get("name").AsString(), obj.Get("message").AsString(), Environment.NewLine, jintEx.StackTrace);
                return string.Format("File: {0} Line: {1} Column: {2} {3}:{4}{5}", jintEx.Location.Source, jintEx.LineNumber, jintEx.Column, jintEx.Message, Environment.NewLine, jintEx.StackTrace);
            });
            ExceptionHandler.RegisterType(typeof(ParserException), ex =>
            {
                var parserEx = (ParserException)ex;
                return string.Format("File: {0} Line: {1} Column: {2} {3}:{4}{5}", parserEx.Source, parserEx.LineNumber, parserEx.Column, parserEx.Description, Environment.NewLine, parserEx.StackTrace);
            });
        }

        /// <summary>
        /// Initializes the JavaScript engine
        /// </summary>
        private void InitializeJavaScript()
        {
            // Create the JavaScript engine
            JavaScriptEngine = new Engine(cfg => cfg.AllowClr(AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssemblyAccess).ToArray()));
            JavaScriptEngine.Global.FastSetProperty("importNamespace", new PropertyDescriptor(new ClrFunctionInstance(JavaScriptEngine, (thisObj, arguments) =>
            {
                var nspace = TypeConverter.ToString(arguments.At(0));
                if (string.IsNullOrEmpty(nspace) || WhitelistNamespaces.Any(nspace.StartsWith) || nspace.Equals("System"))
                {
                    return new NamespaceReference(JavaScriptEngine, nspace);
                }
                return JsValue.Null;
            }), false, false, false));
        }

        /// <summary>
        /// Returns if the specified assembly should be loaded or not
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        internal bool AllowAssemblyAccess(Assembly assembly)
        {
            return WhitelistAssemblies.Any(whitelist => assembly.GetName().Name.Equals(whitelist));
        }

        /// <summary>
        /// Loads a library into the specified path
        /// </summary>
        /// <param name="library"></param>
        /// <param name="path"></param>
        public void LoadLibrary(Library library, string path)
        {
            ObjectInstance scope = null;
            if (library.IsGlobal)
            {
                scope = JavaScriptEngine.Global;
            }
            else if (JavaScriptEngine.Global.GetProperty(path) == PropertyDescriptor.Undefined)
            {
                JavaScriptEngine.Global.FastAddProperty(path, new LibraryWrapper(JavaScriptEngine, library) { Extensible = true }, true, false, true);
                return;
                //scope = new ObjectInstance(JavaScriptEngine) { Extensible = true };
                //JavaScriptEngine.Global.FastAddProperty(path, scope, true, false, true);
            }
            else
            {
                var jsValue = JavaScriptEngine.Global.GetProperty(path).Value;
                if (jsValue != null)
                    scope = jsValue.Value.AsObject();
            }
            if (scope == null)
            {
                Manager.Logger.Write(LogType.Info, "Library path: " + path + " cannot be set");
                return;
            }
            foreach (string name in library.GetFunctionNames())
            {
                MethodInfo method = library.GetFunction(name);

                var typeArgs = method.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToList();

                Type delegateType;
                if (method.ReturnType == typeof(void))
                {
                    delegateType = Expression.GetActionType(typeArgs.ToArray());

                }
                else
                {
                    typeArgs.Add(method.ReturnType);
                    delegateType = Expression.GetFuncType(typeArgs.ToArray());
                }

                scope.FastAddProperty(name, new DelegateWrapper(JavaScriptEngine, Delegate.CreateDelegate(delegateType, library, method)), true, false, true);
            }
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {
            // Register the watchers
            var watcher = new FSWatcher(plugindir, "*.js");
            Manager.RegisterPluginChangeWatcher(watcher);
            loader.Watcher = watcher;
            watcher = new FSWatcher(plugindir, "*.coffee");
            Manager.RegisterPluginChangeWatcher(watcher);
            coffeeLoader.Watcher = watcher;
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            foreach (var extension in Manager.GetAllExtensions())
            {
                if (!extension.IsGameExtension) continue;
                WhitelistAssemblies = extension.WhitelistAssemblies;
                WhitelistNamespaces = extension.WhitelistNamespaces;
                break;
            }

            // Setup JavaScript instance
            InitializeJavaScript();

            // Register the js loader
            loader = new JavaScriptPluginLoader(JavaScriptEngine);
            Manager.RegisterPluginLoader(loader);

            // Register the coffee loader
            coffeeLoader = new CoffeeScriptPluginLoader(JavaScriptEngine);
            Manager.RegisterPluginLoader(coffeeLoader);

            // Bind JavaScript specific libraries
            LoadLibrary(new JavaScriptGlobal(Manager.Logger), "");
            LoadLibrary(new JavaScriptDatafile(JavaScriptEngine), "data");

            // Bind any libraries to JavaScript
            foreach (string name in Manager.GetLibraries())
            {
                LoadLibrary(Manager.GetLibrary(name), name.ToLowerInvariant());
            }

            // Extension to webrequests
            LoadLibrary(new JavaScriptWebRequests(), "webrequests");
        }
    }
}
