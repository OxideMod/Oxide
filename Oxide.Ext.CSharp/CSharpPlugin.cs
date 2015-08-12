using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Plugins
{
    public class PluginLoadFailure : Exception
    {
        public PluginLoadFailure(string reason) : base()
        {
        }
    }

    /// <summary>
    /// Allows configuration of plugin info using an attribute above the plugin class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class InfoAttribute : Attribute
    {
        public string Title { get; }
        public string Author { get; }
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
    /// Allows plugins to specify a description of the plugin using an attribute above the plugin class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; }

        public DescriptionAttribute(string Description)
        {
            this.Description = Description;
        }
    }

    /// <summary>
    /// Indicates that the specified field should be a reference to another plugin when it is loaded
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PluginReferenceAttribute : Attribute
    {
        public string Name { get; }

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
    /// Indicates that the specified Hash field should be used to automatically track online players
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class OnlinePlayersAttribute : Attribute
    {
        public OnlinePlayersAttribute()
        {
        }
    }

    /// <summary>
    /// Base class which all dynamic CSharp plugins must inherit
    /// </summary>
    public abstract partial class CSharpPlugin : CSPlugin
    {
        /// <summary>
        /// Wrapper for dynamically managed plugin fields
        /// </summary>
        public class PluginFieldInfo
        {
            public Plugin Plugin;
            public FieldInfo Field;
            public Type FieldType;
            public Type[] GenericArguments;
            public Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();

            public PluginFieldInfo(Plugin plugin, FieldInfo field)
            {
                this.Plugin = plugin;
                this.Field = field;
                this.FieldType = field.FieldType;
                this.GenericArguments = FieldType.GetGenericArguments();
            }

            public bool HasValidConstructor(params Type[] argument_types)
            {
                var type = GenericArguments[1];
                return type.GetConstructor(new Type[0]) != null || type.GetConstructor(argument_types) != null;
            }

            public object Value => Field.GetValue(Plugin);

            public bool LookupMethod(string method_name, params Type[] argument_types)
            {
                var method = FieldType.GetMethod(method_name, argument_types);
                if (method == null) return false;
                Methods[method_name] = method;
                return true;
            }

            public object Call(string method_name, params object[] args)
            {
                MethodInfo method;
                if (!Methods.TryGetValue(method_name, out method))
                {
                    method = FieldType.GetMethod(method_name, BindingFlags.Instance | BindingFlags.Public);
                    Methods[method_name] = method;
                }
                if (method == null) throw new MissingMethodException(FieldType.Name, method_name);
                return method.Invoke(Value, args);
            }
        }

        public FSWatcher Watcher;

        protected Core.Libraries.Plugins plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>("Plugins");
        protected PluginTimers timer;

        protected HashSet<PluginFieldInfo> onlinePlayerFields = new HashSet<PluginFieldInfo>();
        private Dictionary<string, FieldInfo> pluginReferenceFields = new Dictionary<string, FieldInfo>();

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
                    pluginReferenceFields[plugin_reference.Name ?? field.Name] = field;
                }
            }
            foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var info_attributes = method.GetCustomAttributes(typeof(HookMethod), true);
                if (info_attributes.Length > 0) continue;
                if (method.Name == "OnFrame") HookedOnFrame = true;
                // Assume all private instance methods which are not explicitly hooked could be hooks
                if (method.DeclaringType.Name == type.Name) AddHookMethod(method.Name, method);
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

            var description_attributes = GetType().GetCustomAttributes(typeof(DescriptionAttribute), true);
            if (description_attributes.Length > 0)
            {
                var info = description_attributes[0] as DescriptionAttribute;
                Description = info.Description;
            }

            var method = GetType().GetMethod("LoadDefaultConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            HasConfig = method.DeclaringType != typeof(Plugin);
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            base.HandleAddedToManager(manager);

            if (Filename != null) Watcher.AddMapping(Name);

            foreach (var name in pluginReferenceFields.Keys)
                pluginReferenceFields[name].SetValue(this, manager.GetPlugin(name));

            try
            {
                OnCallHook("Loaded", new object[0]);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to initialize plugin '{Name} v{Version}'", ex);
                Loader.PluginErrors[Name] = ex.Message;
            }
        }

        public override void HandleRemovedFromManager(PluginManager manager)
        {
            if (IsLoaded)
            {
                CallHook("Unloaded", null);
                CallHook("Unload", null);
            }

            Watcher.RemoveMapping(Name);

            foreach (var name in pluginReferenceFields.Keys)
                pluginReferenceFields[name].SetValue(this, null);

            base.HandleRemovedFromManager(manager);
        }

        /// <summary>
        /// Called from Init/Loaded callback to set a failure reason and unload the plugin
        /// </summary>
        /// <param name="reason"></param>
        public void SetFailState(string reason)
        {
            throw new PluginLoadFailure(reason);
        }

        [HookMethod("OnPluginLoaded")]
        void base_OnPluginLoaded(Plugin plugin)
        {
            FieldInfo field;
            if (pluginReferenceFields.TryGetValue(plugin.Name, out field))
                field.SetValue(this, plugin);
        }

        [HookMethod("OnPluginUnloaded")]
        void base_OnPluginUnloaded(Plugin plugin)
        {
            FieldInfo field;
            if (pluginReferenceFields.TryGetValue(plugin.Name, out field))
                field.SetValue(this, null);
        }

        /// <summary>
        /// Print an info message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Puts(string format, params object[] args)
        {
            Interface.Oxide.LogInfo("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print a warning message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintWarning(string format, params object[] args)
        {
            Interface.Oxide.LogWarning("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print an error message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintError(string format, params object[] args)
        {
            Interface.Oxide.LogError("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Queue a callback to be called in the next server tick
        /// </summary>
        /// <param name="callback"></param>
        protected void NextTick(Action callback)
        {
            Interface.Oxide.NextTick(callback);
        }

        /// <summary>
        /// Queues a callback to be called from a thread pool worker thread
        /// </summary>
        /// <param name="callback"></param>
        protected void QueueWorkerThread(Action<object> callback)
        {
            ThreadPool.QueueUserWorkItem(context =>
            {
                try
                {
                    callback(context);
                }
                catch (Exception ex)
                {
                    RaiseError($"Exception in '{Name} v{Version}' plugin worker thread: {ex.ToString()}");
                }
            });
        }
    }
}
