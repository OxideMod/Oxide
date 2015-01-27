using System;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using Oxide.Core.Libraries;
using Oxide.Rust.Libraries;

using UnityEngine;

namespace Oxide.Plugins
{
    /// <summary>
    /// Allows configuration of plugin info using an attribute above the plugin class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class Info : Attribute
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public VersionNumber Version { get; private set; }

        public Info(string title, string author, string version)
        {
            Title = title;
            Author = author;
            setVersion(version);
        }

        public Info(string title, string author, double version)
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
    public class ConsoleCommand : Attribute
    {
        public string Command { get; private set; }

        public ConsoleCommand(string command)
        {
            Command = command.Contains('.') ? command : ("global." + command);
        }
    }

    /// <summary>
    /// Indicates that the specified method should be a handler for a chat command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ChatCommand : Attribute
    {
        public string Command { get; private set; }

        public ChatCommand(string command)
        {
            Command = command;
        }
    }

    /// <summary>
    /// Base class which all dynamic CSharp plugins must inherit
    /// </summary>
    public abstract class CSharpPlugin : CSPlugin
    {
        public string Filename;

        public FSWatcher Watcher;

        protected Command cmd;
        protected Permissions permissions;

        public CSharpPlugin() : base()
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // Assume all private instance methods could be hooks
                if (!hooks.ContainsKey(method.Name)) hooks[method.Name] = method;
            }
        }

        public void SetPluginInfo(string name, string path)
        {
            Name = name;
            Filename = path;

            var info_attributes = GetType().GetCustomAttributes(typeof(Info), true);
            if (info_attributes.Length > 0)
            {
                var info = info_attributes[0] as Info;
                Title = info.Title;
                Author = info.Author;
                Version = info.Version;
            }

            var method = GetType().GetMethod("LoadDefaultConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            HasConfig = method.DeclaringType != typeof(Plugin);

            cmd = Interface.GetMod().GetLibrary<Command>("Command");
            permissions = Interface.GetMod().GetLibrary<Permissions>("Permissions");
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            base.HandleAddedToManager(manager);

            if (Filename != null) Watcher.AddMapping(Name);

            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ConsoleCommand), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ConsoleCommand;
                    cmd.AddConsoleCommand(attribute.Command, this, method.Name);
                    continue;
                }

                attributes = method.GetCustomAttributes(typeof(ChatCommand), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ChatCommand;
                    cmd.AddChatCommand(attribute.Command, this, method.Name);
                }
            }

            CallHook("Loaded", null);
        }

        public override void HandleRemovedFromManager(PluginManager manager)
        {
            CallHook("Unloaded", null);

            Watcher.RemoveMapping(Name);

            base.HandleRemovedFromManager(manager);
        }

        /// <summary>
        /// Print a message to a players console log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void PrintToConsole(BasePlayer player, string format, params object[] args)
        {
            player.SendConsoleCommand("echo " + string.Format(format, args));
        }

        /// <summary>
        /// Print a message to a players chat log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void PrintToChat(BasePlayer player, string format, params object[] args)
        {
            player.SendConsoleCommand("chat.add \"Oxide\" " + StringExtensions.QuoteSafe(string.Format(format, args)));
        }

        /// <summary>
        /// Send a reply message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendReply(ConsoleSystem.Arg arg, string format, params string[] args)
        {
            var message = string.Format(format, args);

            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (player != null)
                {
                    player.SendConsoleCommand("echo " + message);
                    return;
                }
            }

            Puts(message);
        }

        /// <summary>
        /// Send a reply message in response to a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendReply(BasePlayer player, string format, params string[] args)
        {
            PrintToChat(player, format, args);
        }

        /// <summary>
        /// Send a warning message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendWarning(ConsoleSystem.Arg arg, string format, params string[] args)
        {
            var message = string.Format(format, args);

            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (player != null)
                {
                    player.SendConsoleCommand("echo " + message);
                }
            }

            Debug.LogWarning(message);
        }

        /// <summary>
        /// Send an error message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendError(ConsoleSystem.Arg arg, string format, params string[] args)
        {
            var message = string.Format(format, args);

            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (player != null)
                {
                    player.SendConsoleCommand("echo " + message);
                }
            }

            Debug.LogError(message);
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