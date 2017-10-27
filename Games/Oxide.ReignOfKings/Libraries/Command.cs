using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.ReignOfKings.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Oxide.Game.ReignOfKings.Libraries
{
    /// <summary>
    /// A library containing functions for adding console and chat commands
    /// </summary>
    public class Command : Library
    {
        public override bool IsGlobal => false;

        internal class ChatCommand
        {
            public readonly string Name;
            public readonly Plugin Plugin;
            public readonly Action<Player, string, string[]> Callback;
            public CommandAttribute OriginalCallback;

            public ChatCommand(string name, Plugin plugin, Action<Player, string, string[]> callback)
            {
                Name = name;
                Plugin = plugin;
                Callback = callback;
            }

            public void HandleCommand(Player sender, string name, string[] args)
            {
                Plugin?.TrackStart();
                Callback?.Invoke(sender, name, args);
                Plugin?.TrackEnd();
            }
        }

        // All chat commands that plugins have registered
        internal Dictionary<string, ChatCommand> ChatCommands;

        // A reference to the plugin removed callbacks
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> pluginRemovedFromManager;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            ChatCommands = new Dictionary<string, ChatCommand>();
            pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();
        }

        /// <summary>
        /// Adds a chat command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        [LibraryFunction("AddChatCommand")]
        public void AddChatCommand(string name, Plugin plugin, string callback) => AddChatCommand(name, plugin, (player, command, args) => plugin.CallHook(callback, player, command, args));

        /// <summary>
        /// Adds a chat command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void AddChatCommand(string command, Plugin plugin, Action<Player, string, string[]> callback)
        {
            // Convert command to lowercase and remove whitespace
            var commandName = command.ToLowerInvariant().Trim();

            // Setup a new Chat command
            var newCommand = new ChatCommand(commandName, plugin, callback);

            // Check if the command can be overridden
            if (!CanOverrideCommand(commandName, "chat"))
            {
                var pluginName = plugin?.Name ?? "An unknown plugin";
                Interface.Oxide.LogError("{0} tried to register command '{1}', this command already exists and cannot be overridden!", pluginName, commandName);
                return;
            }

            // Check if command already exists in another Reign of Kings plugin
            ChatCommand cmd;
            if (ChatCommands.TryGetValue(commandName, out cmd))
            {
                if (cmd.OriginalCallback != null) newCommand.OriginalCallback = cmd.OriginalCallback;

                var previousPluginName = cmd.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{commandName}' chat command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
            }

            // Check if command already exists in a Covalence plugin
            ReignOfKingsCommandSystem.RegisteredCommand covalenceCommand;
            if (ReignOfKingsCore.Covalence.CommandSystem.registeredCommands.TryGetValue(commandName, out covalenceCommand))
            {
                if (covalenceCommand.OriginalCallback != null) newCommand.OriginalCallback = covalenceCommand.OriginalCallback;
                var previousPluginName = covalenceCommand.Source?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var message = $"{newPluginName} has replaced the '{commandName}' command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(message);
                ReignOfKingsCore.Covalence.CommandSystem.UnregisterCommand(commandName, covalenceCommand.Source);
            }

            // Check if command is a vanilla Reign of Kings command
            if (CommandManager.RegisteredCommands.ContainsKey(commandName))
            {
                if (newCommand.OriginalCallback == null) newCommand.OriginalCallback = CommandManager.RegisteredCommands[commandName];
                CommandManager.RegisteredCommands.Remove(commandName);
                if (cmd == null && covalenceCommand == null)
                {
                    var newPluginName = plugin?.Name ?? "An unknown plugin";
                    var message =
                        $"{newPluginName} has replaced the '{commandName}' command previously registered by Reign of Kings";
                    Interface.Oxide.LogWarning(message);
                }
            }

            // Add the new command to collections
            ChatCommands[commandName] = newCommand;
            CommandManager.RegisteredCommands[commandName] = new CommandAttribute("/" + commandName, string.Empty)
            {
                Method = (Action<CommandInfo>)Delegate.CreateDelegate(typeof(Action<CommandInfo>), this, GetType().GetMethod("HandleCommand", BindingFlags.NonPublic | BindingFlags.Instance))
            };

            // Hook the unload event
            if (plugin != null && !pluginRemovedFromManager.ContainsKey(plugin))
                pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(plugin_OnRemovedFromManager);
        }

        private void HandleCommand(CommandInfo cmdInfo)
        {
            ChatCommand cmd;
            if (!ChatCommands.TryGetValue(cmdInfo.Label.ToLowerInvariant(), out cmd)) return;
            cmd.HandleCommand(cmdInfo.Player, cmdInfo.Label, cmdInfo.Args);
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
            if (!ChatCommands.TryGetValue(name.ToLowerInvariant(), out cmd)) return false;
            cmd.HandleCommand(sender, name, args);
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
            foreach (var cmd in ChatCommands.Values.Where(c => c.Plugin == sender).ToArray())
            {
                // This command is no longer registered by any plugins
                ChatCommands.Remove(cmd.Name);

                // If this was originally a vanilla Reign of Kings command then restore it, otherwise remove it
                if (cmd.OriginalCallback != null)
                    CommandManager.RegisteredCommands[cmd.Name] = cmd.OriginalCallback;
                else
                    CommandManager.RegisteredCommands.Remove(cmd.Name);
            }

            // Unhook the event
            Event.Callback<Plugin, PluginManager> callback;
            if (pluginRemovedFromManager.TryGetValue(sender, out callback))
            {
                callback.Remove();
                pluginRemovedFromManager.Remove(sender);
            }
        }

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command, string type)
        {
            ReignOfKingsCommandSystem.RegisteredCommand cmd;
            if (ReignOfKingsCore.Covalence.CommandSystem.registeredCommands.TryGetValue(command, out cmd))
                if (cmd.Source.IsCorePlugin)
                    return false;

            if (type == "chat")
            {
                ChatCommand chatCommand;
                if (ChatCommands.TryGetValue(command, out chatCommand))
                    if (chatCommand.Plugin.IsCorePlugin)
                        return false;
            }

            return !ReignOfKingsCore.RestrictedCommands.Contains(command);
        }
    }
}
