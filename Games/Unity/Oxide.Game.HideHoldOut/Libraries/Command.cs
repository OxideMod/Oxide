using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Game.HideHoldOut.Libraries
{
    /// <summary>
    /// A library containing functions for adding console and chat commands
    /// </summary>
    public class Command : Library
    {
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

            public ConsoleCommand(string name)
            {
                Name = name;
            }
            public void AddCallback(Plugin plugin, string name)
            {
                PluginCallbacks.Add(new PluginCallback(plugin, name));
            }

            public bool HandleCommand(string commandString)
            {
                var result = true;
                foreach (var callback in PluginCallbacks)
                {
                    var callbackResult = callback.Plugin.CallHook(callback.Name, commandString);
                    if (callbackResult != null && !(bool)callbackResult) result = (bool)callbackResult;
                }
                return result;
            }
        }

        private class ChatCommand
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
        private readonly Dictionary<string, ConsoleCommand> consoleCommands;

        // All chat commands that plugins have registered
        private readonly Dictionary<string, ChatCommand> chatCommands;

        // A reference to the plugin removed callbacks
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> pluginRemovedFromManager;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            consoleCommands = new Dictionary<string, ConsoleCommand>();
            chatCommands = new Dictionary<string, ChatCommand>();
            pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();
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
            ConsoleCommand cmd;
            if (consoleCommands.TryGetValue(fullName, out cmd))
            {
                // This is a custom command which was already registered by another plugin
                var previousPluginName = cmd.PluginCallbacks[0].Plugin?.Name ?? "An unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{name}' console command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
                consoleCommands.Remove(fullName);
            }

            // The command either does not already exist or is replacing a previously registered command
            cmd = new ConsoleCommand(fullName);
            cmd.AddCallback(plugin, callbackName);

            // Add the new command to collections
            consoleCommands[fullName] = cmd;
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
            if (chatCommands.TryGetValue(commandName, out cmd))
            {
                var previousPluginName = cmd.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{commandName}' chat command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
            }
            cmd = new ChatCommand(commandName, plugin, callbackName);

            // Add the new command to collections
            chatCommands[commandName] = cmd;

            // Hook the unload event
            if (plugin != null && !pluginRemovedFromManager.ContainsKey(plugin))
                pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(plugin_OnRemovedFromManager);
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(PlayerInfos player, string command, string[] args)
        {
            ChatCommand cmd;
            if (!chatCommands.TryGetValue(command.ToLowerInvariant(), out cmd)) return false;
            cmd.Plugin.CallHook(cmd.CallbackName, player, command, args);

            return true;
        }

        internal object HandleConsoleCommand(string commandString)
        {
            ConsoleCommand cmd;
            if (!consoleCommands.TryGetValue($"{commandString.Split(' ')[0]}".ToLowerInvariant(), out cmd)) return null;

            return cmd.HandleCommand(commandString);
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
            }

            // Remove all chat commands which were registered by the plugin
            foreach (var cmd in chatCommands.Values.Where(c => c.Plugin == sender).ToArray()) chatCommands.Remove(cmd.Name);

            // Unhook the event
            Event.Callback<Plugin, PluginManager> event_callback;
            if (pluginRemovedFromManager.TryGetValue(sender, out event_callback))
            {
                event_callback.Remove();
                pluginRemovedFromManager.Remove(sender);
            }
        }
    }
}
