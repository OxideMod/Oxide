using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins.Watchers;

using Oxide.Ext.Python.Libraries;
using Oxide.Ext.Python.Plugins;

namespace Oxide.Ext.Python
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class PythonExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "Python"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, 0); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Nogrod"; } }

        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        public ScriptEngine PythonEngine { get; private set; }

        // The plugin change watcher
        private FSWatcher watcher;

        // The plugin loader
        private PythonPluginLoader loader;

        // Whitelist
        private static readonly string[] WhitelistAssemblies = { "Assembly-CSharp", "DestMath", "Facepunch", "Oxide.Core", "protobuf-net", "RustBuild", "System", "UnityEngine" };
        private static readonly string[] WhitelistNamespaces = { "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "UnityEngine" };

        delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple);

        /// <summary>
        /// Initialises a new instance of the PythonExtension class
        /// </summary>
        /// <param name="manager"></param>
        public PythonExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Setup Lua instance
            InitialisePython();

            // Register the loader
            loader = new PythonPluginLoader(PythonEngine);
            Manager.RegisterPluginLoader(loader);
        }

        /// <summary>
        /// Initialises the Python engine
        /// </summary>
        private void InitialisePython()
        {
            // Create the Python engine
            PythonEngine = IronPython.Hosting.Python.CreateEngine();

            // Bind all namespaces and types
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssemblyAccess))
            {
                PythonEngine.Runtime.LoadAssembly(assembly);
            }

            PythonEngine.GetBuiltinModule().SetVariable("__import__", new ImportDelegate(DoImport));
            PythonEngine.GetBuiltinModule().RemoveVariable("execfile");
            PythonEngine.GetBuiltinModule().RemoveVariable("exit");
            PythonEngine.GetBuiltinModule().RemoveVariable("file");
            PythonEngine.GetBuiltinModule().RemoveVariable("input");
            PythonEngine.GetBuiltinModule().RemoveVariable("open");
            PythonEngine.GetBuiltinModule().RemoveVariable("raw_input");
            PythonEngine.GetBuiltinModule().RemoveVariable("reload");
        }

        private object DoImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple tuple)
        {
            if (WhitelistNamespaces.Any(moduleName.StartsWith))
            {
                return IronPython.Modules.Builtin.__import__(context, moduleName);
            }
            return null;
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
            ScriptScope scope;
            if (library.IsGlobal)
            {
                scope = PythonEngine.GetBuiltinModule();
            }
            else if (!PythonEngine.GetBuiltinModule().TryGetVariable(path, out scope))
            {
                scope = PythonEngine.CreateScope();
                PythonEngine.GetBuiltinModule().SetVariable(path, scope);
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

                } else {
                    typeArgs.Add(method.ReturnType);
                    delegateType = Expression.GetFuncType(typeArgs.ToArray());
                }

                scope.SetVariable(name, Delegate.CreateDelegate(delegateType, library, method));
            }
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {
            // Register the watcher
            watcher = new FSWatcher(plugindir, "*.py");
            Manager.RegisterPluginChangeWatcher(watcher);
            loader.Watcher = watcher;
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            // Bind Python specific libraries
            var logger = new PythonLogger(Manager.Logger);
            PythonEngine.Runtime.IO.SetOutput(logger, Encoding.Default);
            PythonEngine.Runtime.IO.SetErrorOutput(logger, Encoding.Default);
            LoadLibrary(new PythonDatafile(PythonEngine), "data");
            LoadLibrary(new PythonUtil(), "util");

            // Bind any libraries to python
            foreach (string name in Manager.GetLibraries())
            {
                LoadLibrary(Manager.GetLibrary(name), name.ToLowerInvariant());
            }
        }
    }
}
