using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Game.ReignOfKings.Libraries
{
    /// <summary>
    /// A library containing functions for adding console and chat commands
    /// </summary>
    public class Command : Library
    {
        public override bool IsGlobal => false;

        private struct PluginCallback
        {
            public Plugin Plugin;
            public string Name;

            public PluginCallback(Plugin plugin, string name)
            {
                Plugin = plugin;
                Name = name;
            }
        }

        private class ChatCommand
        {
            public string Name;
            public Plugin Plugin;
            public string CallbackName;

            public ChatCommand(string name, Plugin plugin, string callback_name)
            {
                Name = name;
                Plugin = plugin;
                CallbackName = callback_name;
            }
        }

        // All chat commands that plugins have registered
        private Dictionary<string, ChatCommand> chatCommands;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            chatCommands = new Dictionary<string, ChatCommand>();
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
                var msg = $"{new_plugin_name} has replaced the '{command_name}' chat command previously registered by {previous_plugin_name}";
                Interface.Oxide.LogWarning(msg);
            }

            cmd = new ChatCommand(command_name, plugin, callback_name);

            // Add the new command to collections
            chatCommands[command_name] = cmd;

            var commandAttribute = new CommandAttribute("/" + command_name, string.Empty);
            var action = (Action<CommandInfo>)Delegate.CreateDelegate(typeof(Action<CommandInfo>), this, GetType().GetMethod("HandleCommand", BindingFlags.NonPublic | BindingFlags.Instance));
            commandAttribute.Method = action;
            if (CommandManager.RegisteredCommands.ContainsKey(command_name))
            {
                var new_plugin_name = plugin?.Name ?? "An unknown plugin";
                var msg = $"{new_plugin_name} has replaced the '{command_name}' chat command";
                Interface.Oxide.LogWarning(msg);
            }
            CommandManager.RegisteredCommands[command_name] = commandAttribute;

            // Hook the unload event
            if (plugin) plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;
        }

        private void HandleCommand(CommandInfo cmdInfo)
        {
            ChatCommand cmd;
            if (!chatCommands.TryGetValue(cmdInfo.Label.ToLowerInvariant(), out cmd)) return;
            cmd.Plugin.CallHook(cmd.CallbackName, cmdInfo.Player, cmdInfo.Label, cmdInfo.Args);
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(Player sender, string name, string[] args)
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
            // Remove all chat commands which were registered by the plugin
            foreach (var cmd in chatCommands.Values.Where(c => c.Plugin == sender).ToArray())
            {
                chatCommands.Remove(cmd.Name);
                CommandManager.RegisteredCommands.Remove(cmd.Name);
            }

            // Unhook the event
            sender.OnRemovedFromManager -= plugin_OnRemovedFromManager;
        }
    }
}
