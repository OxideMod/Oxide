using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Game.Rust.Libraries
{
    /// <summary>
    /// A library containing functions for adding console and chat commands
    /// </summary>
    public class Command : Library
    {
        private static string ReturnEmptyString() => string.Empty;
        private static void DoNothing(string str) { }

        public override bool IsGlobal => false;

        private struct PluginCallback
        {
            public readonly Plugin Plugin;
            public readonly string Name;

            public PluginCallback(Plugin plugin, string name)
            {
                Plugin = plugin;
                Name = name;
            }
        }

        private class ConsoleCommand
        {
            public readonly string Name;
            public readonly List<PluginCallback> PluginCallbacks = new List<PluginCallback>();
            public readonly ConsoleSystem.Command RustCommand;
            public Action<ConsoleSystem.Arg> OriginalCallback;

            public ConsoleCommand(string name)
            {
                Name = name;
                var split_name = Name.Split('.');
                RustCommand = new ConsoleSystem.Command
                {
                    name = split_name[1],
                    parent = split_name[0],
                    namefull = name,
                    isCommand = true,
                    isUser = true,
                    isAdmin = true,
                    GetString = ReturnEmptyString,
                    SetString = DoNothing,
                    Call = HandleCommand
                };
            }

            public void AddCallback(Plugin plugin, string name)
            {
                PluginCallbacks.Add(new PluginCallback(plugin, name));
            }

            private void HandleCommand(ConsoleSystem.Arg arg)
            {
                if (PluginCallbacks.Any(callback => callback.Plugin.CallHook(callback.Name, arg) != null))
                {
                    return;
                }

                // Call rust implemented command handler if the command was not handled by a plugin
                OriginalCallback?.Invoke(arg);
            }
        }

        private class ChatCommand
        {
            public readonly string Name;
            public readonly Plugin Plugin;
            public readonly string CallbackName;

            public ChatCommand(string name, Plugin plugin, string callback_name)
            {
                Name = name;
                Plugin = plugin;
                CallbackName = callback_name;
            }
        }

        // All console commands that plugins have registered
        private readonly Dictionary<string, ConsoleCommand> consoleCommands;

        // All chat commands that plugins have registered
        private readonly Dictionary<string, ChatCommand> chatCommands;

        // A reference to Rust's internal command dictionary
        private IDictionary<string, ConsoleSystem.Command> rustcommands;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            consoleCommands = new Dictionary<string, ConsoleCommand>();
            chatCommands = new Dictionary<string, ChatCommand>();
        }

        /// <summary>
        /// Adds a console command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callback_name"></param>
        [LibraryFunction("AddConsoleCommand")]
        public void AddConsoleCommand(string name, Plugin plugin, string callback_name)
        {
            // Hack us the dictionary
            if (rustcommands == null) rustcommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as IDictionary<string, ConsoleSystem.Command>;

            // Hook the unload event
            if (plugin) plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;

            var full_name = name.Trim();

            ConsoleCommand cmd;
            if (consoleCommands.TryGetValue(full_name, out cmd))
            {
                // Another plugin registered this command
                if (cmd.OriginalCallback != null)
                {
                    // This is a vanilla rust command which has already been pre-hooked by another plugin
                    cmd.AddCallback(plugin, callback_name);
                    return;
                }

                // This is a custom command which was already registered by another plugin
                var previous_plugin_name = cmd.PluginCallbacks[0].Plugin?.Name ?? "an unknown plugin";
                var new_plugin_name = plugin?.Name ?? "An unknown plugin";
                var msg = $"{new_plugin_name} has replaced the {name} console command which was previously registered by {previous_plugin_name}";
                Interface.Oxide.LogWarning(msg);
                consoleCommands.Remove(full_name);
                rustcommands.Remove(full_name);
                ConsoleSystem.Index.GetAll().Remove(cmd.RustCommand);
            }

            // The command either does not already exist or is replacing a previously registered command
            cmd = new ConsoleCommand(full_name);
            cmd.AddCallback(plugin, callback_name);

            ConsoleSystem.Command rust_command;
            if (rustcommands.TryGetValue(full_name, out rust_command))
            {
                // This is a vanilla rust command which has not yet been hooked by a plugin
                if (rust_command.isVariable)
                {
                    var new_plugin_name = plugin?.Name ?? "An unknown plugin";
                    Interface.Oxide.LogError($"{new_plugin_name} tried to register the {name} console variable as a command!");
                    return;
                }
                // Copy some of the original rust commands attributes
                cmd.RustCommand.isUser = rust_command.isUser;
                cmd.RustCommand.isAdmin = rust_command.isAdmin;
                // Store the original rust callback
                cmd.OriginalCallback = rust_command.Call;
            }

            // Add the new command to collections
            consoleCommands[full_name] = cmd;
            rustcommands[cmd.RustCommand.namefull] = cmd.RustCommand;
            ConsoleSystem.Index.GetAll().Add(cmd.RustCommand);
        }

        /// <summary>
        /// Adds a chat command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callback_name"></param>
        [LibraryFunction("AddChatCommand")]
        public void AddChatCommand(string name, Plugin plugin, string callback_name)
        {
            var command_name = name.ToLowerInvariant();

            ChatCommand cmd;
            if (chatCommands.TryGetValue(command_name, out cmd))
            {
                var previous_plugin_name = cmd.Plugin?.Name ?? "an unknown plugin";
                var new_plugin_name = plugin?.Name ?? "An unknown plugin";
                var msg = $"{new_plugin_name} has replaced the {command_name} chat command which was previously registered by {previous_plugin_name}";
                Interface.Oxide.LogWarning(msg);
            }

            cmd = new ChatCommand(command_name, plugin, callback_name);

            // Add the new command to collections
            chatCommands[command_name] = cmd;

            // Hook the unload event
            if (plugin) plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(BasePlayer sender, string name, string[] args)
        {
            ChatCommand cmd;
            if (!chatCommands.TryGetValue(name.ToLowerInvariant(), out cmd)) return false;

            cmd.Plugin.CallHook(cmd.CallbackName, sender, name, args);

            return true;
        }

        /// <summary>
        /// Called when a plugin has been removed from manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            // Find all console commands which were registered by the plugin
            var commands = consoleCommands.Values.Where(c => c.PluginCallbacks.Any(cb => cb.Plugin == sender)).ToArray();
            foreach (var cmd in commands)
            {
                cmd.PluginCallbacks.RemoveAll(cb => cb.Plugin == sender);
                if (cmd.PluginCallbacks.Count > 0) continue;

                // This command is no longer registered by any plugins
                consoleCommands.Remove(cmd.Name);

                if (cmd.OriginalCallback == null)
                {
                    // This is a custom command, remove it completely
                    rustcommands.Remove(cmd.RustCommand.namefull);
                    ConsoleSystem.Index.GetAll().Remove(cmd.RustCommand);
                }
                else
                {
                    // This is a vanilla rust command, restore the original callback
                    cmd.RustCommand.Call = cmd.OriginalCallback;
                }
            }

            // Remove all chat commands which were registered by the plugin
            foreach (var cmd in chatCommands.Values.Where(c => c.Plugin == sender).ToArray())
                chatCommands.Remove(cmd.Name);

            // Unhook the event
            sender.OnRemovedFromManager -= plugin_OnRemovedFromManager;
        }
    }
}
