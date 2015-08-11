using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using NLua;
using NLua.Exceptions;

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Logging;
using Oxide.Core.Plugins.Watchers;

using Oxide.Ext.Lua.Libraries;
using Oxide.Ext.Lua.Plugins;

namespace Oxide.Ext.Lua
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class LuaExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Lua";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        internal NLua.Lua LuaEnvironment { get; set; }

        /// <summary>
        /// Gets the metatable to use for the plugin table
        /// </summary>
        internal LuaTable PluginMetatable { get; private set; }

        // Blacklist and whitelist
        private bool _typesInit;

        // Utility
        private LuaFunction setmetatable;
        private LuaTable overloadselectormeta;
        private LuaTable typetablemeta, generictypetablemeta;
        private LuaTable libraryMetaTable;

        // The plugin change watcher
        private FSWatcher watcher;

        // The plugin loader
        private LuaPluginLoader loader;

        /// <summary>
        /// Initializes a new instance of the LuaExtension class
        /// </summary>
        /// <param name="manager"></param>
        public LuaExtension(ExtensionManager manager)
            : base(manager)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var extDir = Interface.Oxide.ExtensionDirectory;
                File.WriteAllText(Path.Combine(extDir, "KeraLua.dll.config"), $"<configuration>\n<dllmap dll=\"lua52\" target=\"{extDir}/x86/liblua52.so\" os=\"linux\" cpu=\"x86\" />\n<dllmap dll=\"lua52\" target=\"{extDir}/x64/liblua52.so\" os=\"linux\" cpu=\"x86-64\" />\n</configuration>");
            }
            ExceptionHandler.RegisterType(typeof(LuaScriptException), ex =>
            {
                var luaex = (LuaScriptException) ex;
                var outEx = luaex.IsNetException ? luaex.InnerException : luaex;
                var match = Regex.Match(string.IsNullOrEmpty(luaex.Source) ? luaex.Message : luaex.Source, @"\[string ""(.+)""\]:(\d+): ");
                if (match.Success)
                    return string.Format("File: {0} Line: {1} {2}:{3}{4}", match.Groups[1], match.Groups[2], outEx.Message.Replace(match.Groups[0].Value, ""), Environment.NewLine, outEx.StackTrace);
                return string.Format("{0}{1}:{2}{3}", luaex.Source, outEx.Message, Environment.NewLine, outEx.StackTrace);
            });
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Setup Lua instance
            InitializeLua();

            // Register the loader
            loader = new LuaPluginLoader(LuaEnvironment, this);
            Manager.RegisterPluginLoader(loader);
        }

        /// <summary>
        /// Initializes the Lua environment
        /// </summary>
        private void InitializeLua()
        {
            // Create the Lua environment
            LuaEnvironment = new NLua.Lua();

            // Filter useless or potentially malicious libraries/functions
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
            //Type mytype = GetType();
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

            LuaEnvironment.NewTable("tmp");
            LuaEnvironment.LoadString(
@"function tmp:__index( key )
    if (type( key ) == 'table') then
        local baseType = rawget( self, '_type' )
        return util.SpecializeType( baseType, key )
    end
end
", "LuaExtension").Call();
            generictypetablemeta = LuaEnvironment["tmp"] as LuaTable;
            LuaEnvironment["tmp"] = null;

            LuaEnvironment.NewTable("libraryMetaTable");
            LuaEnvironment.LoadString(
@"function libraryMetaTable:__index( key )
    local ptbl = rawget( self, '_properties' )
    local property = ptbl[ key ]
    if (property) then return property:GetValue( rawget( self, '_object' ), null ) end
end
function libraryMetaTable:__newindex( key, value )
    local ptbl = rawget( self, '_properties' )
    local property = ptbl[ key ]
    if (property) then property:SetValue( rawget( self, '_object' ), value ) end
end
", "LuaExtension").Call();
            libraryMetaTable = LuaEnvironment["libraryMetaTable"] as LuaTable;
            LuaEnvironment["libraryMetaTable"] = null;

            LuaEnvironment.NewTable("tmp");
            PluginMetatable = LuaEnvironment["tmp"] as LuaTable;
            LuaEnvironment.LoadString(
@"function tmp:__newindex( key, value )
    if (type( value ) ~= 'function') then return rawset( self, key, value ) end
    local activeAttrib = rawget( self, '_activeAttrib' )
    if (not activeAttrib) then return rawset( self, key, value ) end
    if (activeAttrib == self) then
        print( 'PluginMetatable.__newindex - self._activeAttrib was somehow self!' )
        rawset( self, key, value )
        return
    end
    local attribArr = rawget( self, '_attribArr' )
    if (not attribArr) then
        attribArr = {}
        rawset( self, '_attribArr', attribArr )
    end
    activeAttrib._func = value
    attribArr[#attribArr + 1] = activeAttrib
    rawset( self, '_activeAttrib', nil )
end
", "LuaExtension").Call();
            LuaEnvironment["tmp"] = null;

        }

        internal void InitializeTypes()
        {
            if (_typesInit) return;
            _typesInit = true;
            var filter = new Regex(@"\$|\<|\>|\#=", RegexOptions.Compiled);
            // Bind all namespaces and types
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                .Where(AllowAssemblyAccess)
                .SelectMany(Utility.GetAllTypesFromAssembly)
                .Where(AllowTypeAccess))
            {
                if (filter.IsMatch(type.FullName)) continue;
                // Get the namespace table
                var nspacetable = GetNamespaceTable(Utility.GetNamespace(type));

                // Bind the type
                nspacetable[Utility.GetTypeName(type)] = CreateTypeTable(type);
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
                    Interface.Oxide.LogError("_G is null!");
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

            // Is it generic?
            if (type.IsGenericType)
            {
                // Setup metamethod
                setmetatable.Call(tmp, generictypetablemeta);
            }
            // Is it an enum?
            else if (type.IsEnum)
            {
                // Set all enum fields
                var fields = type.GetFields().Where(x => x.IsLiteral);
                foreach (var value in fields)
                {
                    tmp[value.Name] = value.GetValue(null);
                }
            }
            else
            {
                // Bind all public static methods
                MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                HashSet<string> processed = new HashSet<string>();
                foreach (MethodInfo method in methods)
                {
                    if (!processed.Contains(method.Name))
                    {
                        // We need to check if this method is overloaded
                        MethodInfo[] overloads = methods.Where(m => m.Name == method.Name).ToArray();
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

                // Bind all nested types
                foreach (var nested in type.GetNestedTypes())
                    tmp[nested.Name] = CreateTypeTable(nested);

                // Setup metamethod
                setmetatable.Call(tmp, typetablemeta);
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
        /// The __index metamethod of the type table
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static object ReadStaticProperty(LuaTable tbl, object key)
        {
            Interface.Oxide.LogWarning("__index ReadStaticProperty {0}", key);
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
                Interface.Oxide.LogWarning("Tried to access _sftbl on type table when writing but it doesn't exist!");
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
        /// Returns if the specified assembly should be loaded or not
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private bool AllowAssemblyAccess(Assembly assembly)
        {
            return WhitelistAssemblies.Any(whitelist => assembly.GetName().Name.Equals(whitelist));
        }

        /// <summary>
        /// Returns if the specified type should be bound to Lua or not
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool AllowTypeAccess(Type type)
        {
            // Special case: allow access to Oxide.Core.OxideMod
            if (type.FullName == "Oxide.Core.OxideMod") return true;

            // Respect the whitelist and blacklist
            // The only exception is to allow all value types directly under System
            string nspace = Utility.GetNamespace(type);
            if (string.IsNullOrEmpty(nspace)) return true;
            if (nspace == "System")
            {
                if (type.IsValueType || type.Name == "String" || type.Name == "Convert") return true;
            }
            foreach (string whitelist in WhitelistNamespaces)
                if (nspace.StartsWith(whitelist)) return true;
            return false;
        }

        /// <summary>
        /// Loads a plugin function attribute type
        /// </summary>
        /// <param name="attrName"></param>
        private void LoadPluginFunctionAttribute(string attrName)
        {
            Action<LuaTable> func = (tbl) =>
            {
                LuaTable pluginTable = LuaEnvironment["PLUGIN"] as LuaTable;
                tbl["_attribName"] = attrName;
                (LuaEnvironment["rawset"] as LuaFunction).Call(pluginTable, "_activeAttrib", tbl);
            };
            LuaEnvironment[attrName] = func;
        }

        /// <summary>
        /// Loads a library into the specified path
        /// </summary>
        /// <param name="library"></param>
        /// <param name="path"></param>
        public void LoadLibrary(Library library, string path)
        {
            //Interface.GetMod().RootLogger.Write(LogType.Debug, "Loading library '{0}' into Lua... (path is '{1}')", library.GetType().Name, path);

            // Create the library table if it doesn't exist
            LuaTable libraryTable = LuaEnvironment[path] as LuaTable;
            if (libraryTable == null)
            {
                LuaEnvironment.NewTable(path);
                libraryTable = LuaEnvironment[path] as LuaTable;
                //Interface.GetMod().RootLogger.Write(LogType.Debug, "Library table not found, creating one... {0}", libraryTable);
            }
            else
            {
                //Interface.GetMod().RootLogger.Write(LogType.Debug, "Library table found, using it... {0}", libraryTable);
            }

            // Bind all methods
            foreach (string name in library.GetFunctionNames())
            {
                MethodInfo method = library.GetFunction(name);
                LuaEnvironment.RegisterFunction(string.Format("{0}.{1}", path, name), library, method);
            }

            // Only bind properties if it's not global
            if (path != "_G")
            {
                // Create properties table
                LuaEnvironment.NewTable("tmp");
                LuaTable propertiesTable = LuaEnvironment["tmp"] as LuaTable;
                //Interface.GetMod().RootLogger.Write(LogType.Debug, "Made properties table {0}", propertiesTable);
                libraryTable["_properties"] = propertiesTable;
                libraryTable["_object"] = library; // NOTE: Is this a security risk?
                LuaEnvironment["tmp"] = null;

                // Bind all properties
                foreach (string name in library.GetPropertyNames())
                {
                    PropertyInfo property = library.GetProperty(name);
                    propertiesTable[name] = property;
                }

                // Bind the metatable
                //Interface.GetMod().RootLogger.Write(LogType.Debug, "setmetatable {0}", libraryMetaTable);
                (LuaEnvironment["setmetatable"] as LuaFunction).Call(libraryTable, libraryMetaTable);
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
        public override void OnModLoad()
        {
            foreach (var extension in Manager.GetAllExtensions())
            {
                if (!extension.IsGameExtension) continue;
                WhitelistAssemblies = extension.WhitelistAssemblies;
                WhitelistNamespaces = extension.WhitelistNamespaces;
                break;
            }

            // Bind Lua specific libraries
            LoadLibrary(new LuaGlobal(Manager.Logger), "_G");
            LuaEnvironment.NewTable("datafile");
            LoadLibrary(new LuaDatafile(LuaEnvironment), "datafile");
            if (LuaEnvironment["util"] == null)
                LuaEnvironment.NewTable("util");
            LoadLibrary(new LuaUtil(LuaEnvironment), "util");

            // Bind any libraries to Lua
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

            // Bind attributes to Lua
            LoadPluginFunctionAttribute("Command");
        }
    }
}
