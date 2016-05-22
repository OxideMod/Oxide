using System;
using System.Collections.Generic;
using System.Linq;

using Facepunch.Util;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Game.RustLegacy.Libraries
{
    /// <summary>
    /// A library containing functions for adding console and chat commands
    /// </summary>
    public class Command : Library
    {
        public override bool IsGlobal => false;

        internal struct PluginCallback
        {
            public readonly Plugin Plugin;
            public readonly string Name;

            public PluginCallback(Plugin plugin, string name)
            {
                Plugin = plugin;
                Name = name;
            }
        }

        internal class ConsoleCommand
        {
            public readonly string Name;
            public readonly List<PluginCallback> PluginCallbacks = new List<PluginCallback>();

            public ConsoleCommand(string name)
            {
                Name = name;
            }
            public void AddCallback(Plugin plugin, string name)
            {
                PluginCallbacks.Add(new PluginCallback(plugin, name));
            }

            public bool HandleCommand(ConsoleSystem.Arg arg)
            {
                var result = true;
                foreach (var callback in PluginCallbacks)
                {
                    var callbackResult = callback.Plugin.CallHook(callback.Name, arg);
                    if (callbackResult != null && !(bool)callbackResult)
                        result = (bool)callbackResult;
                }
                return result;
            }
        }

        internal class ChatCommand
        {
            public readonly string Name;
            public readonly Plugin Plugin;
            public readonly string CallbackName;

            public ChatCommand(string name, Plugin plugin, string callbackName)
            {
                Name = name;
                Plugin = plugin;
                CallbackName = callbackName;
            }
        }

        // All console commands that plugins have registered
        internal static Dictionary<string, ConsoleCommand> ConsoleCommands;

        // All chat commands that plugins have registered
        internal static Dictionary<string, ChatCommand> ChatCommands;

        // All of the default console commands from the server
        internal static List<string> DefaultCommands;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            ConsoleCommands = new Dictionary<string, ConsoleCommand>();
            ChatCommands = new Dictionary<string, ChatCommand>();
            DefaultCommands = new List<string>();
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assem.GetTypes())
                {
                    if (!type.IsSubclassOf(typeof(ConsoleSystem))) continue;
                    foreach (var field in type.GetFields())
                    {
                        if (!field.IsStatic || !Reflection.HasAttribute(field, typeof(ConsoleSystem.Admin))) continue;
                        DefaultCommands.Add(string.Concat(type.Name, ".", field.Name));
                    }
                    foreach (var prop in type.GetProperties())
                    {
                        if (!prop.GetGetMethod().IsStatic || !Reflection.HasAttribute(prop, typeof(ConsoleSystem.Admin))) continue;
                        DefaultCommands.Add(string.Concat(type.Name, ".", prop.Name));
                    }
                    foreach (var method in type.GetMethods())
                    {
                        if (!method.IsStatic || !Reflection.HasAttribute(method, typeof(ConsoleSystem.Admin))) continue;
                        DefaultCommands.Add(string.Concat(type.Name, ".", method.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Adds a console command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callbackName"></param>
        [LibraryFunction("AddConsoleCommand")]
        public void AddConsoleCommand(string name, Plugin plugin, string callbackName)
        {
            // Hook the unload event
            if (plugin) plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;

            var fullName = name.Trim();

            ConsoleCommand cmd;
            if (ConsoleCommands.TryGetValue(fullName, out cmd))
            {
                // This is a custom command which was already registered by another plugin
                var previousPluginName = cmd.PluginCallbacks[0].Plugin?.Name ?? "An unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{name}' console command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
                ConsoleCommands.Remove(fullName);
            }

            // The command either does not already exist or is replacing a previously registered command
            cmd = new ConsoleCommand(fullName);
            cmd.AddCallback(plugin, callbackName);

            // Add the new command to collections
            ConsoleCommands[fullName] = cmd;
        }

        /// <summary>
        /// Adds a chat command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callbackName"></param>
        [LibraryFunction("AddChatCommand")]
        public void AddChatCommand(string name, Plugin plugin, string callbackName)
        {
            var commandName = name.ToLowerInvariant();

            ChatCommand cmd;
            if (ChatCommands.TryGetValue(commandName, out cmd))
            {
                var previousPluginName = cmd.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{commandName}' chat command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
            }

            cmd = new ChatCommand(commandName, plugin, callbackName);

            // Add the new command to collections
            ChatCommands[commandName] = cmd;

            // Hook the unload event
            if (plugin) plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(NetUser sender, string name, string[] args)
        {
            ChatCommand cmd;
            if (!ChatCommands.TryGetValue(name.ToLowerInvariant(), out cmd)) return false;
            cmd.Plugin.CallHook(cmd.CallbackName, sender, name, args);

            return true;
        }

        internal object HandleConsoleCommand(ConsoleSystem.Arg arg, bool wantsReply)
        {
            ConsoleCommand cmd;
            if (!ConsoleCommands.TryGetValue($"{arg.Class}.{arg.Function}".ToLowerInvariant(), out cmd)) return null;

            return cmd.HandleCommand(arg);
        }

        /// <summary>
        /// Called when a plugin has been removed from manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            // Find all console commands which were registered by the plugin
            var commands = ConsoleCommands.Values.Where(c => c.PluginCallbacks.Any(cb => cb.Plugin == sender)).ToArray();
            foreach (var cmd in commands)
            {
                cmd.PluginCallbacks.RemoveAll(cb => cb.Plugin == sender);
                if (cmd.PluginCallbacks.Count > 0) continue;

                // This command is no longer registered by any plugins
                ConsoleCommands.Remove(cmd.Name);
            }

            // Remove all chat commands which were registered by the plugin
            foreach (var cmd in ChatCommands.Values.Where(c => c.Plugin == sender).ToArray())
                ChatCommands.Remove(cmd.Name);

            // Unhook the event
            sender.OnRemovedFromManager -= plugin_OnRemovedFromManager;
        }
    }
}
