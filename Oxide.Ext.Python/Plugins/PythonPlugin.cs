﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.Python.Plugins
{
    /// <summary>
    /// Represents a Python plugin
    /// </summary>
    public class PythonPlugin : Plugin
    {
        /// <summary>
        /// Gets the Python engine
        /// </summary>
        public ScriptEngine PythonEngine { get; private set; }

        /// <summary>
        /// Gets this plugin's Python class
        /// </summary>
        public object Class { get; private set; }

        /// <summary>
        /// Gets this plugin's scope
        /// </summary>
        public ScriptScope Scope { get; private set; }

        /// <summary>
        /// Gets the object associated with this plugin
        /// </summary>
        public override object Object { get { return Class; } }

        /// <summary>
        /// Gets the filename of this plugin
        /// </summary>
        public new string Filename { get; private set; }

        public IList<string> Globals;

        // The plugin change watcher
        private readonly FSWatcher watcher;

        /// <summary>
        /// Initializes a new instance of the PythonPlugin class
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="engine"></param>
        /// <param name="watcher"></param>
        internal PythonPlugin(string filename, ScriptEngine engine, FSWatcher watcher)
        {
            // Store filename
            Filename = filename;
            PythonEngine = engine;
            this.watcher = watcher;
        }

        #region Config

        /// <summary>
        /// Populates the config with default settings
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            if (Class != null)
            {
                PythonEngine.Operations.SetMember(Class, "Config", new PythonDictionary());
            }
            CallHook("LoadDefaultConfig", null);
        }

        /// <summary>
        /// Loads the config file for this plugin
        /// </summary>
        protected override void LoadConfig()
        {
            base.LoadConfig();
            if (Class != null)
            {
                PythonEngine.Operations.SetMember(Class, "Config", Utility.DictionaryFromConfig(Config, PythonEngine));
            }
        }

        /// <summary>
        /// Saves the config file for this plugin
        /// </summary>
        protected override void SaveConfig()
        {
            if (Config == null) return;
            if (Class == null) return;
            if (!PythonEngine.Operations.ContainsMember(Class, "Config")) return;
            var configtable = PythonEngine.Operations.GetMember<PythonDictionary>(Class, "Config");
            if (configtable != null)
            {
                Utility.SetConfigFromDictionary(Config, configtable);
            }
            base.SaveConfig();
        }

        #endregion

        /// <summary>
        /// Loads this plugin
        /// </summary>
        public void Load()
        {
            // Load the plugin
            string code = File.ReadAllText(Filename);
            Name = Path.GetFileNameWithoutExtension(Filename);
            Scope = PythonEngine.CreateScope();
            var source = PythonEngine.CreateScriptSourceFromString(code, Path.GetFileName(Filename), SourceCodeKind.Statements);
            var compiled = source.Compile();
            compiled.Execute(Scope);
            if (!Scope.ContainsVariable(Name)) throw new Exception("Plugin is missing main class");
            Class = PythonEngine.Operations.CreateInstance(Scope.GetVariable(Name));
            PythonEngine.Operations.SetMember(Class, "Name", Name);

            // Read plugin attributes
            if (!PythonEngine.Operations.ContainsMember(Class, "Title") || PythonEngine.Operations.GetMember<string>(Class, "Title") == null) throw new Exception("Plugin is missing title");
            if (!PythonEngine.Operations.ContainsMember(Class, "Author") || PythonEngine.Operations.GetMember<string>(Class, "Author") == null) throw new Exception("Plugin is missing author");
            if (!PythonEngine.Operations.ContainsMember(Class, "Version") || PythonEngine.Operations.GetMember(Class, "Version").GetType() != typeof(VersionNumber)) throw new Exception("Plugin is missing version");
            Title = PythonEngine.Operations.GetMember<string>(Class, "Title");
            Author = PythonEngine.Operations.GetMember<string>(Class, "Author");
            Version = PythonEngine.Operations.GetMember<VersionNumber>(Class, "Version");
            if (PythonEngine.Operations.ContainsMember(Class, "ResourceId")) ResourceId = PythonEngine.Operations.GetMember<int>(Class, "ResourceId");
            HasConfig = PythonEngine.Operations.ContainsMember(Class, "HasConfig") && PythonEngine.Operations.GetMember<bool>(Class, "HasConfig") || PythonEngine.Operations.ContainsMember(Class, "LoadDefaultConfig");

            // Set attributes
            PythonEngine.Operations.SetMember(Class, "Plugin", this);

            Globals = PythonEngine.Operations.GetMemberNames(Class);

            // Bind any base methods (we do it here because we don't want them to be hooked)
            BindBaseMethods();
        }

        /// <summary>
        /// Binds base methods
        /// </summary>
        private void BindBaseMethods()
        {
            BindBaseMethod("SaveConfig", "SaveConfig");
        }

        /// <summary>
        /// Binds the specified base method
        /// </summary>
        /// <param name="methodname"></param>
        /// <param name="pyname"></param>
        private void BindBaseMethod(string methodname, string pyname)
        {
            MethodInfo method = GetType().GetMethod(methodname, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
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
            PythonEngine.Operations.SetMember(Class, pyname, Delegate.CreateDelegate(delegateType, this, method));
        }

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            // Call base
            base.HandleAddedToManager(manager);

            // Subscribe all our hooks
            foreach (string key in Globals)
                Subscribe(key);

            // Add us to the watcher
            watcher.AddMapping(Name);

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
            watcher.RemoveMapping(Name);

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
            return CallFunction(hookname, args);
        }

        /// <summary>
        /// Calls a lua function by the given name and returns the output
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private object CallFunction(string name, object[] args)
        {
            object func;
            if (!Globals.Contains(name) || !PythonEngine.Operations.TryGetMember(Class, name, out func) || !PythonEngine.Operations.IsCallable(func)) return null;
            return PythonEngine.Operations.InvokeMember(Class, name, args ?? new object[]{});
        }
    }
}
