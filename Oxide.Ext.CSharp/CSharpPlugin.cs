using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Plugins
{
    /// <summary>
    /// Allows configuration of plugin info using an attribute above the plugin class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class InfoAttribute : Attribute
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public VersionNumber Version { get; private set; }
        public int ResourceId { get; set; }

        public InfoAttribute(string Title, string Author, string Version)
        {
            this.Title = Title;
            this.Author = Author;
            setVersion(Version);
        }

        public InfoAttribute(string Title, string Author, double Version)
        {
            this.Title = Title;
            this.Author = Author;
            setVersion(Version.ToString());
        }

        private void setVersion(string version)
        {
            var version_parts = version.Split('.').Select(part =>
            {
                ushort number;
                if (!ushort.TryParse(part, out number)) number = 0;
                return number;
            }).ToList();
            while (version_parts.Count < 3) version_parts.Add(0);
            Version = new VersionNumber(version_parts[0], version_parts[1], version_parts[2]);
        }
    }

    /// <summary>
    /// Indicates that the specified field should be a reference to another plugin when it is loaded
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PluginReferenceAttribute : Attribute
    {
        public string Name { get; private set; }

        public PluginReferenceAttribute()
        {
        }

        public PluginReferenceAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Indicates that the specified method should be a handler for a console command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; private set; }

        public ConsoleCommandAttribute(string command)
        {
            Command = command.Contains('.') ? command : ("global." + command);
        }
    }

    /// <summary>
    /// Indicates that the specified method should be a handler for a chat command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ChatCommandAttribute : Attribute
    {
        public string Command { get; private set; }

        public ChatCommandAttribute(string command)
        {
            Command = command;
        }
    }

    /// <summary>
    /// Base class which all dynamic CSharp plugins must inherit
    /// </summary>
    public abstract partial class CSharpPlugin : CSPlugin
    {
        public string Filename;
        public FSWatcher Watcher;

        protected Core.Libraries.Plugins plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
        protected PluginTimers timer;

        private Dictionary<string, FieldInfo> pluginReferences = new Dictionary<string, FieldInfo>();

        public bool HookedOnFrame
        {
            get; private set;
        }

        public CSharpPlugin() : base()
        {
            timer = new PluginTimers(this);

            var type = GetType();
            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var reference_attributes = field.GetCustomAttributes(typeof(PluginReferenceAttribute), true);
                if (reference_attributes.Length > 0)
                {
                    var plugin_reference = reference_attributes[0] as PluginReferenceAttribute;
                    pluginReferences[plugin_reference.Name ?? field.Name] = field;
                }
            }
            foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var info_attributes = method.GetCustomAttributes(typeof(HookMethod), true);
                if (info_attributes.Length > 0) continue;
                if (method.Name == "OnFrame") HookedOnFrame = true;
                // Assume all private instance methods which are not explicitly hooked could be hooks
                if (method.DeclaringType.Name == type.Name && !hooks.ContainsKey(method.Name))
                    hooks[method.Name] = method;
            }
        }

        public virtual void SetPluginInfo(string name, string path)
        {
            Name = name;
            Filename = path;

            var info_attributes = GetType().GetCustomAttributes(typeof(InfoAttribute), true);
            if (info_attributes.Length > 0)
            {
                var info = info_attributes[0] as InfoAttribute;
                Title = info.Title;
                Author = info.Author;
                Version = info.Version;
                ResourceId = info.ResourceId;
            }

            var method = GetType().GetMethod("LoadDefaultConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            HasConfig = method.DeclaringType != typeof(Plugin);
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            base.HandleAddedToManager(manager);

            if (Filename != null) Watcher.AddMapping(Name);

            foreach (var name in pluginReferences.Keys)
                pluginReferences[name].SetValue(this, manager.GetPlugin(name));

            CallHook("Loaded", null);
        }

        public override void HandleRemovedFromManager(PluginManager manager)
        {
            CallHook("Unloaded", null);
            CallHook("Unload", null);

            Watcher.RemoveMapping(Name);

            foreach (var name in pluginReferences.Keys)
                pluginReferences[name].SetValue(this, null);

            base.HandleRemovedFromManager(manager);
        }

        [HookMethod("OnPluginLoaded")]
        void base_OnPluginLoaded(Plugin plugin)
        {
            FieldInfo field;
            if (pluginReferences.TryGetValue(plugin.Name, out field))
                field.SetValue(this, plugin);
        }

        [HookMethod("OnPluginUnloaded")]
        void base_OnPluginUnloaded(Plugin plugin)
        {
            FieldInfo field;
            if (pluginReferences.TryGetValue(plugin.Name, out field))
                field.SetValue(this, null);
        }

        /// <summary>
        /// Print an info message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void Puts(string format, params object[] args)
        {
            Interface.GetMod().RootLogger.Write(Core.Logging.LogType.Info, format, args);
        }

        /// <summary>
        /// Print a warning message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void PrintWarning(string format, params object[] args)
        {
            Interface.GetMod().RootLogger.Write(Core.Logging.LogType.Warning, format, args);
        }

        /// <summary>
        /// Print an error message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void PrintError(string format, params object[] args)
        {
            Interface.GetMod().RootLogger.Write(Core.Logging.LogType.Error, format, args);
        }

        /// <summary>
        /// Queue a callback to be called in the next server tick
        /// </summary>
        /// <param name="callback"></param>
        protected void NextTick(Action callback)
        {
            Interface.GetMod().NextTick(callback);
        }

        /// <summary>
        /// Queues a callback to be called from a thread pool worker thread
        /// </summary>
        /// <param name="callback"></param>
        protected void QueueWorkerThread(Action<object> callback)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(context =>
            {
                try
                {
                    callback(context);
                }
                catch (Exception ex)
                {
                    RaiseError("Exception in " + Name + " plugin worker thread: " + ex.ToString());
                }
            });
        }
    }
}