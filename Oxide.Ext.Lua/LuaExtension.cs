using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using NLua;

using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins.Watchers;

using Oxide.Lua.Plugins;
using Oxide.Lua.Libraries;

namespace Oxide.Lua
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class LuaExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "Lua"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, 0); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Oxide Team"; } }

        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        public NLua.Lua LuaEnvironment { get; private set; }

        // Blacklist and whitelist
        private static readonly string[] blacklistnamespaces = new string[] { "System", "Oxide", "NLua", "KeraLua", "Microsoft", "Mono", "Windows", "POSIX" };
        private static readonly string[] whitelistnamespaces = new string[] { "System.Collections" };

        // Utility
        private LuaFunction setmetatable;
        private LuaTable overloadselectormeta;

        // The plugin change watcher
        private FSWatcher watcher;

        // The plugin loader
        private LuaPluginLoader loader;

        /// <summary>
        /// Initialises a new instance of the LuaExtension class
        /// </summary>
        /// <param name="manager"></param>
        public LuaExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        /// <param name="manager"></param>
        public override void Load()
        {
            // Setup Lua instance
            InitialiseLua();

            // Register the loader
            loader = new LuaPluginLoader(LuaEnvironment);
            Manager.RegisterPluginLoader(loader);
        }

        /// <summary>
        /// Initialises the Lua environment
        /// </summary>
        private void InitialiseLua()
        {
            // Create the Lua environment
            LuaEnvironment = new NLua.Lua();

            // Remove useless or potentially malicious libraries/functions
            LuaEnvironment["os"] = null;
            LuaEnvironment["io"] = null;
            LuaEnvironment["require"] = null;
            LuaEnvironment["dofile"] = null;
            LuaEnvironment["package"] = null;
            LuaEnvironment["luanet"] = null;
            LuaEnvironment["load"] = null;

            // Read util methods
            setmetatable = LuaEnvironment["setmetatable"] as LuaFunction;

            // Create metatables
            LuaEnvironment.NewTable("tmp");
            overloadselectormeta = LuaEnvironment["tmp"] as LuaTable;
            //LuaEnvironment.RegisterFunction("tmp.__index", GetType().GetMethod("FindOverload", BindingFlags.Public | BindingFlags.Static));
            LuaEnvironment["tmp"] = null;

            // Bind all namespaces and types
            foreach (Type type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany((a) => a.GetTypes())
                .Where(AllowTypeAccess))
            {
                // Get the namespace table
                LuaTable nspacetable = GetNamespaceTable(Utility.GetNamespace(type));

                // Bind the type
                nspacetable[type.Name] = CreateTypeTable(type);
            }
        }

        /// <summary>
        /// Gets the namespace table for the specified namespace
        /// </summary>
        /// <param name="nspace"></param>
        /// <returns></returns>
        private LuaTable GetNamespaceTable(string nspace)
        {
            if (string.IsNullOrEmpty(nspace))
            {
                return GetNamespaceTable("global");
            }
            else
            {
                string[] nspacesplit = nspace.Split('.');
                LuaTable curtable = LuaEnvironment["_G"] as LuaTable;
                if (curtable == null)
                {
                    Interface.GetMod().RootLogger.Write(LogType.Debug, "_G is null!");
                    return null;
                }
                for (int i = 0; i < nspacesplit.Length; i++)
                {
                    LuaTable prevtable = curtable;
                    curtable = curtable[nspacesplit[i]] as LuaTable;
                    if (curtable == null)
                    {
                        LuaEnvironment.NewTable("tmp");
                        curtable = LuaEnvironment["tmp"] as LuaTable;
                        LuaEnvironment["tmp"] = null;
                        prevtable[nspacesplit[i]] = curtable;
                    }
                }
                return curtable;
            }
        }

        /// <summary>
        /// Creates a table that encapsulates the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private LuaTable CreateTypeTable(Type type)
        {
            // Make the table
            LuaEnvironment.NewTable("tmp");
            LuaTable tmp = LuaEnvironment["tmp"] as LuaTable;
            

            // Set the type field
            tmp["_type"] = type;

            // Bind all public static methods
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            HashSet<string> processed = new HashSet<string>();
            foreach (MethodInfo method in methods)
            {
                if (!processed.Contains(method.Name))
                {
                    // We need to check if this method is overloaded
                    MethodInfo[] overloads = methods.Where((m) => m.Name == method.Name).ToArray();
                    if (overloads.Length == 1)
                    {
                        // It's not, simply bind it
                        LuaEnvironment.RegisterFunction(string.Format("tmp.{0}", method.Name), method);
                    }
                    else
                    {
                        // It is, "overloads" holds all our method overloads
                        tmp[method.Name] = CreateOverloadSelector(overloads);
                    }

                    // Processed
                    processed.Add(method.Name);
                }
            }

            // Return it
            LuaEnvironment["tmp"] = null;
            return tmp;
        }

        /// <summary>
        /// Creates an overload selector with the specified set of methods
        /// </summary>
        /// <param name="methods"></param>
        /// <returns></returns>
        private LuaTable CreateOverloadSelector(MethodBase[] methods)
        {
            LuaEnvironment.NewTable("overloadselector");
            LuaTable tbl = LuaEnvironment["overloadselector"] as LuaTable;
            LuaEnvironment["overloadselector"] = null;
            tbl["methodarray"] = methods;
            setmetatable.Call(tbl, overloadselectormeta);
            return tbl;
        }

        /// <summary>
        /// The __index metamethod of overload selector
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static MethodBase FindOverload(LuaTable tbl, object key)
        {
            // TODO: This
            // tbl.methodarray has an array of methods to search though
            // key should be a table of types defining the signature of the method to find
            return null;
        }

        /// <summary>
        /// Returns if the specified type should be bound to Lua or not
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool AllowTypeAccess(Type type)
        {
            // Respect the whitelist and blacklist
            // The only exception is to allow all value types directly under System
            string nspace = Utility.GetNamespace(type);
            
            if (string.IsNullOrEmpty(nspace)) return true;
            if (nspace == "System" && type.IsValueType) return true;
            foreach (string whitelist in whitelistnamespaces)
                if (nspace.StartsWith(whitelist)) return true;
            foreach (string blacklist in blacklistnamespaces)
                if (nspace.StartsWith(blacklist)) return false;
            return true;
        }

        /// <summary>
        /// Loads a library into the specified path
        /// </summary>
        /// <param name="library"></param>
        /// <param name="path"></param>
        public void LoadLibrary(Library library, string path)
        {
            foreach (string name in library.GetFunctionNames())
            {
                MethodInfo method = library.GetFunction(name);
                LuaEnvironment.RegisterFunction(string.Format("{0}.{1}", path, name), library, method);
            }
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {
            // Register the watcher
            watcher = new FSWatcher(plugindir, "*.lua");
            Manager.RegisterPluginChangeWatcher(watcher);
            loader.Watcher = watcher;
            
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        /// <param name="manager"></param>
        public override void OnModLoad()
        {
            // Bind Lua specific libraries
            LoadLibrary(new LuaGlobal(Manager.Logger), "_G");
            LuaEnvironment.NewTable("datafile");
            LoadLibrary(new LuaDatafile(LuaEnvironment), "datafile");

            // Bind any libraries to lua
            foreach (string name in Manager.GetLibraries())
            {
                string path = name.ToLowerInvariant();
                Library lib = Manager.GetLibrary(name);
                if (lib.IsGlobal)
                    path = "_G";
                else
                    LuaEnvironment.NewTable(path);
                LoadLibrary(lib, path);
            }
        }


    }
}
