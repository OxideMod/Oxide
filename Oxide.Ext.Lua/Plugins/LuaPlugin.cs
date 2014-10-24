using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using Oxide.Core.Configuration;

using NLua;

namespace Oxide.Lua.Plugins
{
    /// <summary>
    /// Represents a Lua plugin
    /// </summary>
    public class LuaPlugin : Plugin
    {
        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        public NLua.Lua LuaEnvironment { get; private set; }

        /// <summary>
        /// Gets this plugin's Lua table
        /// </summary>
        public LuaTable Table { get; private set; }

        /// <summary>
        /// Gets the object associated with this plugin
        /// </summary>
        public override object Object { get { return Table; } }

        /// <summary>
        /// Gets the filename of this plugin
        /// </summary>
        public string Filename { get; private set; }

        // All functions in this plugin
        private IDictionary<string, LuaFunction> functions;

        // The plugin change watcher
        private FSWatcher watcher;

        /// <summary>
        /// Initialises a new instance of the LuaPlugin class
        /// </summary>
        /// <param name="filename"></param>
        internal LuaPlugin(string filename, NLua.Lua lua, FSWatcher watcher)
        {
            // Store filename
            Filename = filename;
            LuaEnvironment = lua;
            this.watcher = watcher;
        }

        #region Config

        /// <summary>
        /// Populates the config with default settings
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            LuaEnvironment.NewTable("tmp");
            LuaTable tmp = LuaEnvironment["tmp"] as LuaTable;
            Table["Config"] = tmp;
            LuaEnvironment["tmp"] = null;
            CallHook("LoadDefaultConfig", null);
            Utility.SetConfigFromTable(Config, tmp);
        }

        /// <summary>
        /// Loads the config file for this plugin
        /// </summary>
        protected override void LoadConfig()
        {
            base.LoadConfig();
            if (Table != null)
            {
                Table["Config"] = Utility.TableFromConfig(Config, LuaEnvironment);
            }
        }

        /// <summary>
        /// Saves the config file for this plugin
        /// </summary>
        protected override void SaveConfig()
        {
            if (Config == null) return;
            if (Table == null) return;
            LuaTable configtable = Table["Config"] as LuaTable;
            if (configtable != null)
            {
                Utility.SetConfigFromTable(Config, configtable);
            }
            base.SaveConfig();
        }

        #endregion

        /// <summary>
        /// Loads this plugin
        /// </summary>
        public void Load()
        {
            // Load the plugin into a table
            string source = File.ReadAllText(Filename);
            LuaFunction pluginfunc = LuaEnvironment.LoadString(source, Path.GetFileName(Filename));
            if (pluginfunc == null) throw new Exception("LoadString returned null for some reason");
            LuaEnvironment.NewTable("PLUGIN");
            Table = LuaEnvironment["PLUGIN"] as LuaTable;
            Table["Name"] = Name;
            Name = Path.GetFileNameWithoutExtension(Filename);
            pluginfunc.Call();

            // Read plugin attributes
            if (Table["Title"] == null || !(Table["Title"] is string)) throw new Exception("Plugin is missing title");
            if (Table["Author"] == null || !(Table["Author"] is string)) throw new Exception("Plugin is missing author");
            if (Table["Version"] == null || !(Table["Version"] is VersionNumber)) throw new Exception("Plugin is missing version");
            Title = (string)Table["Title"];
            Author = (string)Table["Author"];
            Version = (VersionNumber)Table["Version"];
            if (Table["HasConfig"] is bool) HasConfig = (bool)Table["HasConfig"];

            // Set attributes
            Table["Object"] = this;

            // Get all functions and hook them
            functions = new Dictionary<string, LuaFunction>();
            foreach (var keyobj in Table.Keys)
            {
                string key = keyobj as string;
                if (key != null)
                {
                    object value = Table[key];
                    LuaFunction func = value as LuaFunction;
                    if (func != null)
                    {
                        functions.Add(key, func);
                    }
                }
            }

            // Bind any base methods (we do it here because we don't want them to be hooked)
            BindBaseMethods();

            // Clean up
            LuaEnvironment["PLUGIN"] = null;
        }

        /// <summary>
        /// Binds base methods to the PLUGIN table
        /// </summary>
        private void BindBaseMethods()
        {
            BindBaseMethod("lua_SaveConfig", "SaveConfig");
        }

        /// <summary>
        /// Binds the specified base method to the PLUGIN table
        /// </summary>
        /// <param name="methodname"></param>
        /// <param name="luaname"></param>
        private void BindBaseMethod(string methodname, string luaname)
        {
            MethodInfo method = GetType().GetMethod(methodname, BindingFlags.Static | BindingFlags.NonPublic);
            LuaEnvironment.RegisterFunction(string.Format("PLUGIN.{0}", luaname), method);
        }

        #region Base Methods

        /// <summary>
        /// Saves the config file for the specified plugin
        /// </summary>
        /// <param name="plugintable"></param>
        private static void lua_SaveConfig(LuaTable plugintable)
        {
            LuaPlugin plugin = plugintable["Object"] as LuaPlugin;
            if (plugin == null) return;
            plugin.SaveConfig();
        }

        #endregion

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            // Call base
            base.HandleAddedToManager(manager);

            // Subscribe all our hooks
            foreach (string key in functions.Keys)
                Subscribe(key);

            // Add us to the watcher
            watcher.AddMapping(Filename, this);

            // Let the plugin know that it's loading
            CallFunction("Init", null);
        }

        /// <summary>
        /// Called when this plugin has been removed from the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleRemovedFromManager(PluginManager manager)
        {
            // Let plugin know that it's unloading
            CallFunction("Unload", null);

            // Remove us from the watcher
            watcher.RemoveMappings(this);

            // Call base
            base.HandleRemovedFromManager(manager);
        }

        /// <summary>
        /// Called when it's time to call a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override object OnCallHook(string hookname, object[] args)
        {
            // Call it
            try
            {
                return CallFunction(hookname, args);
            }
            catch (NLua.Exceptions.LuaScriptException luaex)
            {
                if (luaex.IsNetException)
                {
                    // TODO: Throw a better exception?
                    
                }
                throw;
            }
        }

        #region Lua CallFunction Hack

        // An empty object array
        private static readonly object[] emptyargs;

        // The method used to call a lua function
        private static MethodInfo LuaCallFunctionMethod;

        static LuaPlugin()
        {
            // Initialise
            emptyargs = new object[0];

            // Load the method
            Type[] sig = new Type[] { typeof(object), typeof(object[]), typeof(Type[]) };
            LuaCallFunctionMethod = typeof(NLua.Lua).GetMethod("CallFunction", BindingFlags.NonPublic | BindingFlags.Instance, null, sig, null);
            if (LuaCallFunctionMethod == null) throw new Exception("Lua CallFunction hack failed!");
        }

        /// <summary>
        /// Calls a Lua function with the specified arguments
        /// </summary>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private object CallLuaFunction(LuaFunction func, object[] args)
        {
            object[] invokeargs = new object[3] { func, args, null };
            try
            {
                object[] returnvalues = LuaCallFunctionMethod.Invoke(LuaEnvironment, invokeargs) as object[];
                if (returnvalues == null || returnvalues.Length == 0)
                    return null;
                else
                    return returnvalues[0];
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }


        #endregion

        /// <summary>
        /// Calls a lua function by the given name and returns the output
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private object CallFunction(string name, object[] args)
        {
            LuaFunction func;
            if (!functions.TryGetValue(name, out func)) return null;
            object[] realargs;
            if (args == null)
            {
                realargs = new object[] { Table };
            }
            else
            {
                realargs = new object[args.Length + 1];
                realargs[0] = Table;
                Array.Copy(args, 0, realargs, 1, args.Length);
            }
            return CallLuaFunction(func, realargs);
        }

        
    }
}
