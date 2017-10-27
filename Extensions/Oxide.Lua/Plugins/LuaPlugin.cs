using NLua;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Oxide.Core.Lua.Plugins
{
    /// <summary>
    /// Represents a Lua plugin
    /// </summary>
    public class LuaPlugin : Plugin
    {
        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        internal NLua.Lua LuaEnvironment { get; }

        /// <summary>
        /// Gets this plugin's Lua table
        /// </summary>
        private LuaTable Table { get; set; }

        /// <summary>
        /// Gets the object associated with this plugin
        /// </summary>
        public override object Object => Table;

        // All functions in this plugin
        private IDictionary<string, LuaFunction> functions;

        // The plugin change watcher
        private FSWatcher watcher;

        // The Lua extension
        private LuaExtension luaExt;

        /// <summary>
        /// Initializes a new instance of the LuaPlugin class
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="luaExt"></param>
        /// <param name="watcher"></param>
        internal LuaPlugin(string filename, LuaExtension luaExt, FSWatcher watcher)
        {
            // Store filename
            Filename = filename;
            Name = Core.Utility.GetFileNameWithoutExtension(Filename);
            this.luaExt = luaExt;
            LuaEnvironment = luaExt.LuaEnvironment;
            this.watcher = watcher;
        }

        #region Config

        /// <summary>
        /// Populates the config with default settings
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            LuaEnvironment.NewTable("tmp");
            var tmp = LuaEnvironment["tmp"] as LuaTable;
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
                Table["Config"] = Utility.TableFromConfig(Config, LuaEnvironment);
        }

        /// <summary>
        /// Saves the config file for this plugin
        /// </summary>
        protected override void SaveConfig()
        {
            if (Config == null) return;
            if (Table == null) return;
            var configtable = Table["Config"] as LuaTable;
            if (configtable != null)
                Utility.SetConfigFromTable(Config, configtable);
            base.SaveConfig();
        }

        #endregion Config

        /// <summary>
        /// Loads this plugin
        /// </summary>
        public override void Load()
        {
            // Load the plugin into a table
            var source = File.ReadAllText(Filename);
            if (Regex.IsMatch(source, @"PLUGIN\.Version\s*=\s*[\'|""]", RegexOptions.IgnoreCase)) throw new Exception("Plugin version is a string, Oxide 1.18 plugins are not compatible with Oxide 2");
            var pluginfunc = LuaEnvironment.LoadString(source, Path.GetFileName(Filename));
            if (pluginfunc == null) throw new Exception("LoadString returned null for some reason");
            LuaEnvironment.NewTable("PLUGIN");
            Table = (LuaTable)LuaEnvironment["PLUGIN"];
            ((LuaFunction)LuaEnvironment["setmetatable"]).Call(Table, luaExt.PluginMetatable);
            Table["Name"] = Name;
            pluginfunc.Call();

            // Read plugin attributes
            if (!(Table["Title"] is string)) throw new Exception("Plugin is missing title");
            if (!(Table["Author"] is string)) throw new Exception("Plugin is missing author");
            if (!(Table["Version"] is VersionNumber)) throw new Exception("Plugin is missing version");
            Title = (string)Table["Title"];
            Author = (string)Table["Author"];
            Version = (VersionNumber)Table["Version"];
            if (Table["Description"] is string) Description = (string)Table["Description"];
            if (Table["ResourceId"] is double) ResourceId = (int)(double)Table["ResourceId"];
            if (Table["HasConfig"] is bool) HasConfig = (bool)Table["HasConfig"];

            // Set attributes
            Table["Object"] = this;
            Table["Plugin"] = this;

            // Get all functions and hook them
            functions = new Dictionary<string, LuaFunction>();
            foreach (var keyobj in Table.Keys)
            {
                var key = keyobj as string;
                if (key == null) continue;
                var value = Table[key];
                var func = value as LuaFunction;
                if (func != null)
                    functions.Add(key, func);
            }
            if (!HasConfig) HasConfig = functions.ContainsKey("LoadDefaultConfig");

            // Bind any base methods (we do it here because we don't want them to be hooked)
            BindBaseMethods();

            // Deal with any attributes
            var attribs = Table["_attribArr"] as LuaTable;
            if (attribs != null)
            {
                var i = 0;
                while (attribs[++i] != null)
                {
                    var attrib = attribs[i] as LuaTable;
                    var attribName = attrib["_attribName"] as string;
                    var attribFunc = attrib["_func"] as LuaFunction;
                    if (attribFunc != null && !string.IsNullOrEmpty(attribName))
                    {
                        HandleAttribute(attribName, attribFunc, attrib);
                    }
                }
            }

            // Clean up
            LuaEnvironment["PLUGIN"] = null;
        }

        /// <summary>
        /// Handles a method attribute
        /// </summary>
        /// <param name="name"></param>
        /// <param name="method"></param>
        /// <param name="data"></param>
        private void HandleAttribute(string name, LuaFunction method, LuaTable data)
        {
            // What type of attribute is it?
            switch (name)
            {
                case "Command":
                    // Parse data out of it
                    var cmdNames = new List<string>();
                    var i = 0;
                    while (data[++i] != null) cmdNames.Add(data[i] as string);
                    var cmdNamesArr = cmdNames.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    string[] cmdPermsArr;
                    if (data["permission"] is string)
                    {
                        cmdPermsArr = new[] { data["permission"] as string };
                    }
                    else if (data["permission"] is LuaTable || data["permissions"] is LuaTable)
                    {
                        var permsTable = (data["permission"] as LuaTable) ?? (data["permissions"] as LuaTable);
                        var cmdPerms = new List<string>();
                        i = 0;
                        while (permsTable[++i] != null) cmdPerms.Add(permsTable[i] as string);
                        cmdPermsArr = cmdPerms.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    }
                    else
                        cmdPermsArr = new string[0];

                    // Register it
                    AddCovalenceCommand(cmdNamesArr, cmdPermsArr, (caller, cmd, args) =>
                    {
                        HandleCommandCallback(method, caller, cmd, args);
                        return true;
                    });

                    break;
            }
        }

        private void HandleCommandCallback(LuaFunction func, IPlayer caller, string cmd, string[] args)
        {
            LuaEnvironment.NewTable("tmp");
            var argsTable = LuaEnvironment["tmp"] as LuaTable;
            LuaEnvironment["tmp"] = null;
            for (var i = 0; i < args.Length; i++)
            {
                argsTable[i + 1] = args[i];
            }
            try
            {
                func.Call(Table, caller, cmd, argsTable);
            }
            catch (Exception)
            {
                // TODO: Error handling and stuff
                throw;
            }
        }

        /// <summary>
        /// Binds base methods to the PLUGIN table
        /// </summary>
        private void BindBaseMethods() => BindBaseMethod("lua_SaveConfig", "SaveConfig");

        /// <summary>
        /// Binds the specified base method to the PLUGIN table
        /// </summary>
        /// <param name="methodname"></param>
        /// <param name="luaname"></param>
        private void BindBaseMethod(string methodname, string luaname)
        {
            var method = GetType().GetMethod(methodname, BindingFlags.Static | BindingFlags.NonPublic);
            LuaEnvironment.RegisterFunction($"PLUGIN.{luaname}", method);
        }

        #region Base Methods

        /// <summary>
        /// Saves the config file for the specified plugin
        /// </summary>
        /// <param name="plugintable"></param>
        private static void lua_SaveConfig(LuaTable plugintable)
        {
            var plugin = plugintable["Object"] as LuaPlugin;
            plugin?.SaveConfig();
        }

        #endregion Base Methods

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            // Call base
            base.HandleAddedToManager(manager);

            // Subscribe all our hooks
            foreach (var key in functions.Keys) Subscribe(key);

            // Add us to the watcher
            watcher.AddMapping(Name);

            // Let the plugin know that it's loading
            OnCallHook("Init", null);
        }

        /// <summary>
        /// Called when this plugin has been removed from the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleRemovedFromManager(PluginManager manager)
        {
            // Let plugin know that it's unloading
            OnCallHook("Unload", null);

            // Remove us from the watcher
            watcher.RemoveMapping(Name);

            // Call base
            base.HandleRemovedFromManager(manager);

            Table.Dispose();
            LuaEnvironment.DeleteObject(this);
            LuaEnvironment.DoString("collectgarbage()");
        }

        /// <summary>
        /// Called when it's time to call a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override object OnCallHook(string hookname, object[] args)
        {
            LuaFunction func;
            if (!functions.TryGetValue(hookname, out func)) return null;
            try
            {
                object[] returnvalues;
                if (args != null && args.Length > 0)
                {
                    var realargs = new object[args.Length + 1];
                    realargs[0] = Table;
                    Array.Copy(args, 0, realargs, 1, args.Length);
                    returnvalues = func.Call(realargs);
                }
                else
                    returnvalues = func.Call(Table);
                if (returnvalues == null || returnvalues.Length == 0)
                    return null;
                return returnvalues[0];
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
