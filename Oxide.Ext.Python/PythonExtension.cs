using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using IronPython.Hosting;
using IronPython.Modules;
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
        public override string Name => "Python";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        /// <summary>
        /// Gets the Python environment
        /// </summary>
        private ScriptEngine PythonEngine { get; set; }

        // The plugin change watcher
        private FSWatcher watcher;

        // The plugin loader
        private PythonPluginLoader loader;

        // Whitelist
        private static readonly string[] WhitelistModules = { "__future__", "binascii", "hashlib", "math", "random", "re", "time", "types", "warnings" };
        private static readonly Dictionary<string, string[]> WhitelistParts = new Dictionary<string, string[]> { { "os", new [] { "urandom" } } };
        private List<string> _allowedTypes;
        private bool _typesInit;

        delegate object ImportDelegate(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple fromlist);

        /// <summary>
        /// Initializes a new instance of the PythonExtension class
        /// </summary>
        /// <param name="manager"></param>
        public PythonExtension(ExtensionManager manager)
            : base(manager)
        {
            var assem = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name == "IronPython" || assembly.GetName().Name == "Microsoft.Scripting");
            var types = assem.SelectMany(Utility.GetAllTypesFromAssembly).Where(t => t.IsSubclassOf(typeof (Exception)));
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
            InitializePython();

            // Register the loader
            loader = new PythonPluginLoader(PythonEngine, this);
            Manager.RegisterPluginLoader(loader);
        }

        /// <summary>
        /// Initializes the Python engine
        /// </summary>
        private void InitializePython()
        {
            // Create the Python engine
            PythonEngine = IronPython.Hosting.Python.CreateEngine();

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

            PythonEngine.Execute(
@"class Command:
    def __init__(self, *dec_args, **dec_kw):
        self.dec_args = dec_args
        self.dec_kw = dec_kw
        self.name = list(self.dec_args)
        self.permission = []
        name = self.dec_kw.get('name', [])
        if not isinstance(name, list):
            name = [name]
        self.name = self.name + name
        permission = self.dec_kw.get('permission', [])
        if not isinstance(permission, list):
            permission = [permission]
        self.permission = self.permission + permission
        permission = self.dec_kw.get('permissions', [])
        if not isinstance(permission, list):
            permission = [permission]
        self.permission = self.permission + permission
    def __call__(self, f):
        self.f = f
        self.f.isCommand = True
        self.f.name = self.name
        self.f.permission = self.permission
        return self.f", PythonEngine.GetBuiltinModule());
        }

        internal void InitializeTypes()
        {
            if (_typesInit) return;
            _typesInit = true;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssemblyAccess);
            // Bind all namespaces and types
            var enumerable = assemblies as IList<Assembly> ?? assemblies.ToList();
            foreach (var assembly in enumerable)
            {
                PythonEngine.Runtime.LoadAssembly(assembly);
            }

            _allowedTypes = enumerable.SelectMany(Utility.GetAllTypesFromAssembly)
                .Where(t => string.IsNullOrEmpty(Utility.GetNamespace(t))).Select(t => t.Name).ToList();
        }

        private object DoImport(CodeContext context, string moduleName, PythonDictionary globals, PythonDictionary locals, PythonTuple fromlist)
        {
            if (CheckModule(moduleName, fromlist))
            {
                return Builtin.__import__(context, moduleName, globals, locals, fromlist, -1);
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
        private bool AllowAssemblyAccess(Assembly assembly)
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
                //scope = PythonEngine.CreateScope();
                //PythonEngine.GetBuiltinModule().SetVariable(path, scope);
                PythonEngine.GetBuiltinModule().SetVariable(path, library);
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
            foreach (var extension in Manager.GetAllExtensions())
            {
                if (!extension.IsGameExtension) continue;
                WhitelistAssemblies = extension.WhitelistAssemblies;
                WhitelistNamespaces = extension.WhitelistNamespaces;
                break;
            }

            // Bind Python specific libraries
            var logger = new PythonLogger(Manager.Logger);
            PythonEngine.Runtime.IO.SetOutput(logger, Encoding.UTF8);
            PythonEngine.Runtime.IO.SetErrorOutput(logger, Encoding.UTF8);
            LoadLibrary(new PythonDatafile(PythonEngine), "data");
            LoadLibrary(new PythonUtil(), "util");

            // Bind any libraries to Python
            foreach (string name in Manager.GetLibraries())
            {
                LoadLibrary(Manager.GetLibrary(name), name.ToLowerInvariant());
            }
        }
    }
}
