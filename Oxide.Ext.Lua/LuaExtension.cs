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
        private LuaTable typetablemeta;

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
            Type mytype = GetType();
            LuaEnvironment.NewTable("tmp");
            overloadselectormeta = LuaEnvironment["tmp"] as LuaTable;
            //LuaEnvironment.RegisterFunction("tmp.__index", mytype.GetMethod("FindOverload", BindingFlags.Public | BindingFlags.Static));
            LuaEnvironment.NewTable("tmp"); 
            // Ideally I'd like for this to be implemented C# side, but using C#-bound methods as metamethods seems dodgy
            LuaEnvironment.LoadString(
@"function tmp:__index( key )
    local sftbl = rawget( self, '_sftbl' )
    local field = sftbl[ key ]
    if (field) then return field:GetValue( nil ) end
end
function tmp:__newindex( key, value )
    local sftbl = rawget( self, '_sftbl' )
    local field = sftbl[ key ]
    if (field) then field:SetValue( nil, value ) end
end
", "LuaExtension").Call();
            //LuaEnvironment.RegisterFunction("tmp.__index", mytype.GetMethod("ReadStaticProperty", BindingFlags.NonPublic | BindingFlags.Static));
            //LuaEnvironment.RegisterFunction("tmp.__newindex", mytype.GetMethod("WriteStaticProperty", BindingFlags.NonPublic | BindingFlags.Static));
            typetablemeta = LuaEnvironment["tmp"] as LuaTable;
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

            // Make the public static field table
            LuaEnvironment.NewTable("sftbl");
            LuaTable sftbl = LuaEnvironment["sftbl"] as LuaTable;
            LuaEnvironment["sftbl"] = null;
            tmp["_sftbl"] = sftbl;
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                sftbl[field.Name] = field;
            }

            // Setup metamethod
            setmetatable.Call(tmp, typetablemeta);

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
        /// The __index metamethod of the type table
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static object ReadStaticProperty(LuaTable tbl, object key)
        {
            Interface.GetMod().RootLogger.Write(LogType.Warning, "__index ReadStaticProperty {0}", key);
            string keystr = key as string;
            if (keystr == null) return null;
            if (keystr == "_sftbl") return null;
            LuaTable sftbl = tbl["_sftbl"] as LuaTable;
            if (sftbl == null)
            {
                Interface.GetMod().RootLogger.Write(LogType.Warning, "Tried to access _sftbl on type table when reading but it doesn't exist!");
                return null;
            }
            object prop = sftbl[keystr];
            if (prop == null) return null;
            FieldInfo field = prop as FieldInfo;
            if (field != null)
            {
                return field.GetValue(null);
            }
            return null;
        }

        /// <summary>
        /// The __newindex metamethod of the type table
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void WriteStaticProperty(LuaTable tbl, object key, object value)
        {
            string keystr = key as string;
            if (keystr == null) return;
            if (keystr == "_sftbl") return;
            LuaTable sftbl = tbl["_sftbl"] as LuaTable;
            if (sftbl == null)
            {
                Interface.GetMod().RootLogger.Write(LogType.Warning, "Tried to access _sftbl on type table when writing but it doesn't exist!");
                return;
            }
            object prop = sftbl[keystr];
            if (prop == null) return;
            FieldInfo field = prop as FieldInfo;
            if (field != null)
            {
                field.SetValue(null, value);
            }
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
            if (nspace == "System")
            {
                 if (type.IsValueType) return true;
                 if (type.Name == "string" || type.Name == "String") return true;
            }
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
            if (LuaEnvironment["util"] == null)
                LuaEnvironment.NewTable("util");
            LoadLibrary(new LuaUtil(), "util");

            // Bind any libraries to lua
            foreach (string name in Manager.GetLibraries())
            {
                string path = name.ToLowerInvariant();
                Library lib = Manager.GetLibrary(name);
                if (lib.IsGlobal)
                    path = "_G";
                else if (LuaEnvironment[path] == null)
                    LuaEnvironment.NewTable(path);
                LoadLibrary(lib, path);
            }
        }


    }
}
