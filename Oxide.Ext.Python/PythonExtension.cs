using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using IronPython.Hosting;
using IronPython.Runtime;
using IronPython.Runtime.Exceptions;

using Microsoft.Scripting;
using Microsoft.Scripting.Ast;
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
        private static readonly string[] WhitelistAssemblies = { "Assembly-CSharp", "DestMath", "mscorlib", "Oxide.Core", "protobuf-net", "RustBuild", "System", "System.Core", "UnityEngine" };
        private static readonly string[] WhitelistNamespaces = { "Dest", "Facepunch", "Network", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections", "UnityEngine" };
        private static readonly string[] WhitelistModules = { "__future__", "binascii", "hashlib", "math", "random", "re", "time", "types", "warnings" };
        private static readonly Dictionary<string, string[]> WhitelistParts = new Dictionary<string, string[]> { { "os", new [] { "urandom" } } };
        private List<string> _allowedTypes;

        delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple fromlist);

        /// <summary>
        /// Initialises a new instance of the PythonExtension class
        /// </summary>
        /// <param name="manager"></param>
        public PythonExtension(ExtensionManager manager)
            : base(manager)
        {
            var assem = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == "IronPython");
            var types = Utility.GetAllTypesFromAssembly(assem).Where(t => t.IsSubclassOf(typeof (Exception)));
            foreach (var type in types)
            {
                ExceptionHandler.RegisterType(type, ex => PythonEngine.GetService<ExceptionOperations>().FormatException(ex));
            }
            ExceptionHandler.RegisterType(typeof(SyntaxErrorException), ex => PythonEngine.GetService<ExceptionOperations>().FormatException(ex));
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

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssemblyAccess);
            // Bind all namespaces and types
            foreach (var assembly in assemblies)
            {
                PythonEngine.Runtime.LoadAssembly(assembly);
            }

            _allowedTypes = assemblies.SelectMany(Utility.GetAllTypesFromAssembly)
                .Where(t => string.IsNullOrEmpty(Utility.GetNamespace(t))).Select(t => t.Name).ToList();

            var paths = PythonEngine.GetSearchPaths();
            paths.Add(Path.Combine(Interface.GetMod().InstanceDirectory, "Lib"));
            PythonEngine.SetSearchPaths(paths);

            PythonEngine.GetBuiltinModule().SetVariable("__import__", new ImportDelegate(DoImport));
            PythonEngine.GetBuiltinModule().RemoveVariable("execfile");
            PythonEngine.GetBuiltinModule().RemoveVariable("exit");
            PythonEngine.GetBuiltinModule().RemoveVariable("file");
            PythonEngine.GetBuiltinModule().RemoveVariable("input");
            PythonEngine.GetBuiltinModule().RemoveVariable("open");
            PythonEngine.GetBuiltinModule().RemoveVariable("raw_input");
            PythonEngine.GetBuiltinModule().RemoveVariable("reload");
        }

        private object DoImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple fromlist)
        {
            if (CheckModule(moduleName, fromlist))
            {
                return IronPython.Modules.Builtin.__import__(context, moduleName, globals, locals, fromlist, -1);
            }
            throw new ImportException("Import of module " + moduleName + " not allowed");
        }

        private bool CheckModule(string moduleName, PythonTuple fromlist)
        {
            if (WhitelistNamespaces.Any(moduleName.StartsWith)) return true;
            if (moduleName.Equals("System")) return true;
            if (_allowedTypes.Contains(moduleName)) return true;
            if (WhitelistModules.Contains(moduleName)) return true;
            string[] parts;
            return WhitelistParts.TryGetValue(moduleName, out parts) && fromlist.All(@from => parts.Contains(@from));
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
