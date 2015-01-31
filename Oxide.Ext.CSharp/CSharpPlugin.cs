using System;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using Oxide.Core.Libraries;

using UnityEngine;

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

        public InfoAttribute(string title, string author, string version)
        {
            Title = title;
            Author = author;
            setVersion(version);
        }

        public InfoAttribute(string title, string author, double version)
        {
            Title = title;
            Author = author;
            setVersion(version.ToString());
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

        public bool HookedOnFrame
        {
            get; private set;
        }

        public CSharpPlugin() : base()
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var info_attributes = GetType().GetCustomAttributes(typeof(HookMethod), true);
                if (info_attributes.Length > 0) continue;
                if (method.Name == "OnFrame") HookedOnFrame = true;
                // Assume all private instance methods which are not explicitly hooked could be hooks
                if (!hooks.ContainsKey(method.Name)) hooks[method.Name] = method;
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
            }

            var method = GetType().GetMethod("LoadDefaultConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            HasConfig = method.DeclaringType != typeof(Plugin);
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            base.HandleAddedToManager(manager);

            if (Filename != null) Watcher.AddMapping(Name);

            CallHook("Loaded", null);
        }

        public override void HandleRemovedFromManager(PluginManager manager)
        {
            CallHook("Unloaded", null);
            CallHook("Unload", null);

            Watcher.RemoveMapping(Name);

            base.HandleRemovedFromManager(manager);
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
    }
}