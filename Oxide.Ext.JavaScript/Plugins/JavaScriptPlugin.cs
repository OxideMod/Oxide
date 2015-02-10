using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Jint;
using Jint.Native;
using Jint.Native.Error;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Ext.JavaScript.Plugins
{
    /// <summary>
    /// Represents a JavaScript plugin
    /// </summary>
    public class JavaScriptPlugin : Plugin
    {
        /// <summary>
        /// Gets the JavaScript Engine
        /// </summary>
        public Engine JavaScriptEngine { get; private set; }

        /// <summary>
        /// Gets this plugin's JavaScript Class
        /// </summary>
        public ObjectInstance Class { get; private set; }

        /// <summary>
        /// Gets the object associated with this plugin
        /// </summary>
        public override object Object { get { return Class; } }

        /// <summary>
        /// Gets the filename of this plugin
        /// </summary>
        public string Filename { get; private set; }

        public IList<string> Globals;

        // The plugin change watcher
        private readonly FSWatcher watcher;

        /// <summary>
        /// Initialises a new instance of the JavaScriptPlugin class
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="engine"></param>
        /// <param name="watcher"></param>
        internal JavaScriptPlugin(string filename, Engine engine, FSWatcher watcher)
        {
            // Store filename
            Filename = filename;
            JavaScriptEngine = engine;
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
                if (Class.HasProperty("Config"))
                {
                    Class.Put("Config", new ObjectInstance(JavaScriptEngine) { Extensible = true }, true);
                }
                else
                {
                    Class.FastAddProperty("Config", new ObjectInstance(JavaScriptEngine) { Extensible = true }, true, false, true);
                }
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
                if (Class.HasProperty("Config"))
                {
                    Class.Put("Config", Utility.ObjectFromConfig(Config, JavaScriptEngine), true);
                }
                else
                {
                    Class.FastAddProperty("Config", Utility.ObjectFromConfig(Config, JavaScriptEngine), true, false, true);
                }
            }
        }

        /// <summary>
        /// Saves the config file for this plugin
        /// </summary>
        protected override void SaveConfig()
        {
            if (Config == null) return;
            if (Class == null) return;
            if (Class.HasProperty("Config"))
            {
                Utility.SetConfigFromObject(Config, Class.Get("Config").AsObject());
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
            JavaScriptEngine.Execute(code);
            if (JavaScriptEngine.GetValue(Name).TryCast<ObjectInstance>() == null) throw new Exception("Plugin is missing main object");
            Class = JavaScriptEngine.GetValue(Name).AsObject();
            if (!Class.HasProperty("Name"))
                Class.FastAddProperty("Name", Name, true, false, true);
            else
                Class.Put("Name", Name, true);

            // Read plugin attributes
            if (!Class.HasProperty("Title") || string.IsNullOrEmpty(Class.Get("Title").AsString())) throw new Exception("Plugin is missing title");
            if (!Class.HasProperty("Author") || string.IsNullOrEmpty(Class.Get("Author").AsString())) throw new Exception("Plugin is missing author");
            if (!Class.HasProperty("Version") || Class.Get("Version").ToObject() == null) throw new Exception("Plugin is missing version");
            Title = Class.Get("Title").AsString();
            Author = Class.Get("Author").AsString();
            Version = (VersionNumber) Class.Get("Version").ToObject();
            if (Class.HasProperty("ResourceId")) ResourceId = (int)Class.Get("ResourceId").AsNumber();
            HasConfig = Class.HasProperty("HasConfig") && Class.Get("HasConfig").AsBoolean();

            // Set attributes
            Class.FastAddProperty("Plugin", JsValue.FromObject(JavaScriptEngine, this), true, false, true);

            Globals = new List<string>();
            foreach (var property in Class.Properties)
            {
                if (property.Value.Value != null)
                {
                    var callable = property.Value.Value.Value.TryCast<ICallable>();
                    if (callable != null) Globals.Add(property.Key);
                }
            }

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
        /// <param name="jsname"></param>
        private void BindBaseMethod(string methodname, string jsname)
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
            Class.FastAddProperty(jsname, new DelegateWrapper(JavaScriptEngine, Delegate.CreateDelegate(delegateType, this, method)), true, false, true);
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

        protected override void RaiseError(Exception ex)
        {
            var jintEx = ex as JavaScriptException;
            if (jintEx != null)
            {
                var obj = jintEx.Error.ToObject() as ErrorInstance;
                if (obj != null) RaiseError(string.Format("Line: {0} Column: {1} {2}: {3}", jintEx.LineNumber, jintEx.Column, obj.Get("name").AsString(), obj.Get("message").AsString()));
                else RaiseError(string.Format("Line: {0} Column: {1} {2}: {3}", jintEx.LineNumber, jintEx.Column, jintEx.Message, jintEx.StackTrace)); ;
            }
            else
            {
                base.RaiseError(ex);
            }
        }

        /// <summary>
        /// Calls a function by the given name and returns the output
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private object CallFunction(string name, object[] args)
        {
            var callable = Class.Get(name).TryCast<ICallable>();
            if (!Globals.Contains(name) || !Class.HasProperty(name) || callable == null) return null;
            return callable.Call(Class, args != null ? args.Select(x => JsValue.FromObject(JavaScriptEngine, x)).ToArray() : new JsValue[] {});
        }
    }
}
