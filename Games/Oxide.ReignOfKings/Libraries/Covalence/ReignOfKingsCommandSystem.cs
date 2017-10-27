using CodeHatch.Engine.Core.Commands;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class ReignOfKingsCommandSystem : ICommandSystem
    {
        #region Initialization

        // The covalence provider
        private readonly ReignOfKingsCovalenceProvider reignOfKingsCovalence = ReignOfKingsCovalenceProvider.Instance;

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The console player
        private readonly ReignOfKingsConsolePlayer consolePlayer;

        // Command handler
        private readonly CommandHandler commandHandler;

        // All registered commands
        internal IDictionary<string, RegisteredCommand> registeredCommands;

        // Registered commands
        internal class RegisteredCommand
        {
            /// <summary>
            /// The plugin that handles the command
            /// </summary>
            public readonly Plugin Source;

            /// <summary>
            /// The name of the command
            /// </summary>
            public readonly string Command;

            /// <summary>
            /// The callback
            /// </summary>
            public readonly CommandCallback Callback;

            /// <summary>
            /// The callback
            /// </summary>
            public CommandAttribute OriginalCallback;

            /// <summary>
            /// Initializes a new instance of the RegisteredCommand class
            /// </summary>
            /// <param name="source"></param>
            /// <param name="command"></param>
            /// <param name="callback"></param>
            public RegisteredCommand(Plugin source, string command, CommandCallback callback)
            {
                Source = source;
                Command = command;
                Callback = callback;
            }
        }

        /// <summary>
        /// Initializes the command system
        /// </summary>
        public ReignOfKingsCommandSystem()
        {
            registeredCommands = new Dictionary<string, RegisteredCommand>();
            commandHandler = new CommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new ReignOfKingsConsolePlayer();
        }

        private bool ChatCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            RegisteredCommand command;
            return registeredCommands.TryGetValue(cmd, out command) && command.Callback(caller, cmd, args);
        }

        #endregion Initialization

        #region Command Registration

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            // Convert command to lowercase and remove whitespace
            command = command.ToLowerInvariant().Trim();

            // Setup a new Covalence command
            var newCommand = new RegisteredCommand(plugin, command, callback);

            // Check if the command can be overridden
            if (!CanOverrideCommand(command))
                throw new CommandAlreadyExistsException(command);

            // Check if command already exists in another Covalence plugin
            RegisteredCommand cmd;
            if (registeredCommands.TryGetValue(command, out cmd))
            {
                if (cmd.OriginalCallback != null) newCommand.OriginalCallback = cmd.OriginalCallback;

                var previousPluginName = cmd.Source?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var message = $"{newPluginName} has replaced the '{command}' command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(message);
            }

            // Check if command already exists in a Reign of Kings plugin as a chat command
            Command.ChatCommand chatCommand;
            if (cmdlib.ChatCommands.TryGetValue(command, out chatCommand))
            {
                if (chatCommand.OriginalCallback != null) newCommand.OriginalCallback = chatCommand.OriginalCallback;

                var previousPluginName = chatCommand.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var message = $"{newPluginName} has replaced the '{command}' chat command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(message);

                cmdlib.ChatCommands.Remove(command);
            }

            // Check if command is a vanilla Reign of Kings command
            if (CommandManager.RegisteredCommands.ContainsKey(command))
            {
                if (newCommand.OriginalCallback == null) newCommand.OriginalCallback = CommandManager.RegisteredCommands[command];
                CommandManager.RegisteredCommands.Remove(command);
                if (cmd == null && chatCommand == null)
                {
                    var newPluginName = plugin?.Name ?? "An unknown plugin";
                    var message =
                        $"{newPluginName} has replaced the '{command}' command previously registered by Reign of Kings";
                    Interface.Oxide.LogWarning(message);
                }
            }

            // Register the command as a chat command
            registeredCommands[command] = newCommand;
            CommandManager.RegisteredCommands[command] = new CommandAttribute("/" + command, string.Empty)
            {
                Method = (Action<CommandInfo>)Delegate.CreateDelegate(typeof(Action<CommandInfo>), this, GetType().GetMethod("HandleCommand", BindingFlags.NonPublic | BindingFlags.Instance))
            };
        }

        private void HandleCommand(CommandInfo cmdInfo)
        {
            RegisteredCommand cmd;
            if (!registeredCommands.TryGetValue(cmdInfo.Label.ToLowerInvariant(), out cmd)) return;
            var iplayer = reignOfKingsCovalence.PlayerManager.FindPlayerById(cmdInfo.PlayerId.ToString()) ?? consolePlayer;
            HandleChatMessage(iplayer, $"/{cmdInfo.Label} {string.Join(" ", cmdInfo.Args)}");
        }

        #endregion Command Registration

        #region Command Unregistration

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            RegisteredCommand cmd;
            if (!registeredCommands.TryGetValue(command, out cmd)) return;

            // Check if the command belongs to the plugin
            if (plugin != cmd.Source) return;

            // Remove the chat command
            registeredCommands.Remove(command);

            // If this was originally a vanilla Reign of Kings command then restore it, otherwise remove it
            if (cmd.OriginalCallback != null)
                CommandManager.RegisteredCommands[cmd.Command] = cmd.OriginalCallback;
            else
                CommandManager.RegisteredCommands.Remove(cmd.Command);
        }

        #endregion Command Unregistration

        #region Message Handling

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        /*/// <summary>
        /// Handles a console message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleConsoleMessage(IPlayer player, string message) => commandHandler.HandleConsoleMessage(player ?? consolePlayer, message);*/

        #endregion Message Handling

        #region Command Overriding

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command)
        {
            RegisteredCommand cmd;
            if (registeredCommands.TryGetValue(command, out cmd))
                if (cmd.Source.IsCorePlugin)
                    return false;

            Command.ChatCommand chatCommand;
            if (cmdlib.ChatCommands.TryGetValue(command, out chatCommand))
                if (chatCommand.Plugin.IsCorePlugin)
                    return false;

            return !ReignOfKingsCore.RestrictedCommands.Contains(command);
        }

        #endregion Command Overriding
    }
}
