using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace Oxide.Game.SpaceEngineers.Libraries
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
            public readonly Plugin Plugin;
            public readonly string CallbackName;

            public ConsoleCommand(string name, Plugin plugin, string callback)
            {
                Name = name;
                Plugin = plugin;
                CallbackName = callback;
            }
        }

        private class ChatCommand
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

        // All chat commands that plugins have registered
        private readonly Dictionary<string, ChatCommand> chatCommands;

        // All console commands that plugins have registered
        private readonly Dictionary<string, ConsoleCommand> consoleCommands;

        // A reference to the plugin removed callback
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> pluginRemovedFromManager;

        /// <summary>
        /// Initializes a new instance of the Command class
        /// </summary>
        public Command()
        {
            chatCommands = new Dictionary<string, ChatCommand>();
            consoleCommands = new Dictionary<string, ConsoleCommand>();
            pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();
        }

        /// <summary>
        /// Adds a chat command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callbackName"></param>
        [LibraryFunction("AddChatCommand")]
        public void AddChatCommand(string command, Plugin plugin, string callbackName)
        {
            var commandName = command.ToLowerInvariant();
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
        /// Adds a console command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callbackName"></param>
        [LibraryFunction("AddConsoleCommand")]
        public void AddConsoleCommand(string command, Plugin plugin, string callbackName)
        {
            var commandName = command.ToLowerInvariant();
            ConsoleCommand cmd;
            if (consoleCommands.TryGetValue(commandName, out cmd))
            {
                var previousPluginName = cmd.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var msg = $"{newPluginName} has replaced the '{commandName}' console command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(msg);
            }
            cmd = new ConsoleCommand(commandName, plugin, callbackName);

            // Add the new command to collections
            consoleCommands[commandName] = cmd;

            // Hook the unload event
            if (plugin != null && !pluginRemovedFromManager.ContainsKey(plugin))
                pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(plugin_OnRemovedFromManager);
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(IMyPlayer session, string command, string[] args)
        {
            ChatCommand cmd;
            if (!chatCommands.TryGetValue(command.ToLowerInvariant(), out cmd)) return false;
            cmd.Plugin.CallHook(cmd.CallbackName, session, command, args);

            return true;
        }

        /// <summary>
        /// Handles the specified console command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal object HandleConsoleCommand(string command, string[] args)
        {
            ConsoleCommand cmd;
            if (!consoleCommands.TryGetValue(command.ToLowerInvariant(), out cmd)) return null;
            cmd.Plugin.CallHook(cmd.CallbackName, command, args);

            return true;
        }

        /// <summary>
        /// Called when a plugin has been removed from manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            // Remove all console commands which were registered by the plugin
            foreach (var cmd in consoleCommands.Values.Where(c => c.Plugin == sender).ToArray()) consoleCommands.Remove(cmd.Name);

            // Remove all chat commands which were registered by the plugin
            foreach (var cmd in chatCommands.Values.Where(c => c.Plugin == sender).ToArray()) chatCommands.Remove(cmd.Name);

            // Unhook the event
            Event.Callback<Plugin, PluginManager> callback;
            if (pluginRemovedFromManager.TryGetValue(sender, out callback))
            {
                callback.Remove();
                pluginRemovedFromManager.Remove(sender);
            }
        }
    }
}
