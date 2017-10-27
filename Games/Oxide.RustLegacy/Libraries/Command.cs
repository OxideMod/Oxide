using Facepunch.Util;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    if (callbackResult != null && !(bool)callbackResult) result = (bool)callbackResult;
                }
                return result;
            }
        }

        internal class ChatCommand
        {
            public readonly string Name;
            public readonly Plugin Plugin;
            public readonly string CallbackName;

            public ChatCommand(string name, Plugin plugin, string callback)
            {
                Name = name;
                Plugin = plugin;
                CallbackName = callback;
            }
        }

        // All console commands that plugins have registered
        internal static Dictionary<string, ConsoleCommand> ConsoleCommands;

        // All chat commands that plugins have registered
        internal static Dictionary<string, ChatCommand> ChatCommands;

        // All of the default console commands from the server
        internal static List<string> DefaultCommands;

        // A reference to the plugin removed callbacks
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> pluginRemovedFromManager;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            ConsoleCommands = new Dictionary<string, ConsoleCommand>();
            ChatCommands = new Dictionary<string, ChatCommand>();
            DefaultCommands = new List<string>();
            pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();

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
            if (plugin != null && !pluginRemovedFromManager.ContainsKey(plugin))
                pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(plugin_OnRemovedFromManager);

            var fullName = name.Trim();

            ConsoleCommand command;
            if (ConsoleCommands.TryGetValue(fullName, out command))
            {
                // This is a custom command which was already registered by another plugin
                var previousPluginName = command.PluginCallbacks[0].Plugin?.Name ?? "An unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{name}' console command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
                ConsoleCommands.Remove(fullName);
            }

            // The command either does not already exist or is replacing a previously registered command
            command = new ConsoleCommand(fullName);
            command.AddCallback(plugin, callbackName);

            // Add the new command to collections
            ConsoleCommands[fullName] = command;
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

            ChatCommand command;
            if (ChatCommands.TryGetValue(commandName, out command))
            {
                var previousPluginName = command.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{commandName}' chat command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
            }

            command = new ChatCommand(commandName, plugin, callbackName);

            // Add the new command to collections
            ChatCommands[commandName] = command;

            // Hook the unload event
            if (plugin != null && !pluginRemovedFromManager.ContainsKey(plugin))
                pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(plugin_OnRemovedFromManager);
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(NetUser netUser, string command, string[] args)
        {
            ChatCommand cmd;
            if (!ChatCommands.TryGetValue(command.ToLowerInvariant(), out cmd)) return false;
            cmd.Plugin.CallHook(cmd.CallbackName, netUser, command, args);

            return true;
        }

        internal bool HandleConsoleCommand(ConsoleSystem.Arg arg, bool wantsReply)
        {
            ConsoleCommand command;
            if (!ConsoleCommands.TryGetValue($"{arg.Class}.{arg.Function}".ToLowerInvariant(), out command)) return false;

            return command.HandleCommand(arg);
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
            foreach (var command in commands)
            {
                command.PluginCallbacks.RemoveAll(cb => cb.Plugin == sender);
                if (command.PluginCallbacks.Count > 0) continue;

                // This command is no longer registered by any plugins
                ConsoleCommands.Remove(command.Name);
            }

            // Remove all chat commands which were registered by the plugin
            foreach (var command in ChatCommands.Values.Where(c => c.Plugin == sender).ToArray())
                ChatCommands.Remove(command.Name);

            // Unhook the event
            Event.Callback<Plugin, PluginManager> eventCallback;
            if (pluginRemovedFromManager.TryGetValue(sender, out eventCallback))
            {
                eventCallback.Remove();
                pluginRemovedFromManager.Remove(sender);
            }
        }
    }
}
