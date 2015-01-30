using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

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
        public override string Name { get { return "JavaScript"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, 0); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Nogrod"; } }

        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        public Engine JavaScriptEngine { get; private set; }

        // The plugin change watcher
        private FSWatcher watcher;

        // The plugin loader
        private JavaScriptPluginLoader loader;

        private static readonly string[] WhitelistAssemblies = { "Assembly-CSharp", "DestMath", "Facepunch", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "UnityEngine" };
        private static readonly string[] WhitelistNamespaces = { "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "UnityEngine" };

        /// <summary>
        /// Initialises a new instance of the JavaScript class
        /// </summary>
        /// <param name="manager"></param>
        public JavaScriptExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Setup JavaScript instance
            InitialiseJavaScript();

            // Register the loader
            loader = new JavaScriptPluginLoader(JavaScriptEngine);
            Manager.RegisterPluginLoader(loader);
        }

        /// <summary>
        /// Initialises the JavaScript engine
        /// </summary>
        private void InitialiseJavaScript()
        {
            // Create the JavaScript engine
            JavaScriptEngine = new Engine(cfg => cfg.AllowClr(AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssemblyAccess).ToArray()));
            JavaScriptEngine.Global.FastSetProperty("importNamespace", new PropertyDescriptor(new ClrFunctionInstance(JavaScriptEngine, (thisObj, arguments) =>
            {
                var nspace = TypeConverter.ToString(arguments.At(0));
                if (string.IsNullOrEmpty(nspace) || WhitelistNamespaces.Any(nspace.StartsWith))
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
            //JavaScriptEngine.Global.FastAddProperty("System", new NamespaceReference(JavaScriptEngine, "System"), false, false, false);
            ObjectInstance scope = null;
            if (library.IsGlobal)
            {
                scope = JavaScriptEngine.Global;
            }
            else if (JavaScriptEngine.Global.GetProperty(path) == PropertyDescriptor.Undefined)
            {
                scope = new ObjectInstance(JavaScriptEngine) { Extensible = true };
                JavaScriptEngine.Global.FastAddProperty(path, scope, true, false, true);
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
            // Register the watcher
            watcher = new FSWatcher(plugindir, "*.js");
            Manager.RegisterPluginChangeWatcher(watcher);
            loader.Watcher = watcher;
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            // Bind JavaScript specific libraries
            LoadLibrary(new JavaScriptGlobal(Manager.Logger), "");
            LoadLibrary(new JavaScriptDatafile(JavaScriptEngine), "data");
            LoadLibrary(new JavaScriptWebRequests(), "webrequests");

            // Bind any libraries to JavaScript
            foreach (string name in Manager.GetLibraries())
            {
                LoadLibrary(Manager.GetLibrary(name), name.ToLowerInvariant());
            }
        }
    }
}
