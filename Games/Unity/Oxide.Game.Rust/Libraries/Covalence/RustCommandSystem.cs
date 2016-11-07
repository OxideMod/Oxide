using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class RustCommandSystem : ICommandSystem
    {
        #region Initialization

        // The covalence provider
        private readonly RustCovalenceProvider rustCovalence = RustCovalenceProvider.Instance;

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The console player
        private readonly RustConsolePlayer consolePlayer;

        // A reference to Rust's internal command dictionary
        private IDictionary<string, ConsoleSystem.Command> rustCommands;

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
            /// The Rust console command
            /// </summary>
            public ConsoleSystem.Command RustCommand;

            /// <summary>
            /// The original callback
            /// </summary>
            public Action<ConsoleSystem.Arg> OriginalCallback;

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
        public RustCommandSystem()
        {
            registeredCommands = new Dictionary<string, RegisteredCommand>();
            commandHandler = new CommandHandler(CommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new RustConsolePlayer();
        }

        private bool CommandCallback(IPlayer caller, string cmd, string[] args)
        {
            RegisteredCommand command;
            return registeredCommands.TryGetValue(cmd, out command) && command.Callback(caller, cmd, args);
        }

        #endregion

        #region Command Registration

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (rustCommands == null) rustCommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary<string, ConsoleSystem.Command>;

            // Convert command to lowercase and remove whitespace
            command = command.ToLowerInvariant().Trim();

            // Setup console command name
            var split = command.Split('.');
            var parent = split.Length >= 2 ? split[0].Trim() : "global";
            var name = split.Length >= 2 ? string.Join(".", split.Skip(1).ToArray()) : split[0].Trim();
            var fullname = $"{parent}.{name}";

            if (parent == "global") command = name;

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

                rustCommands.Remove(fullname);
                ConsoleSystem.Index.GetAll().Remove(cmd.RustCommand);
            }

            // Check if command already exists in a Rust plugin as a chat command
            Command.ChatCommand chatCommand;
            if (cmdlib.chatCommands.TryGetValue(command, out chatCommand))
            {
                var previousPluginName = chatCommand.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var message = $"{newPluginName} has replaced the '{command}' chat command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(message);

                cmdlib.chatCommands.Remove(command);
            }

            // Check if command already exists in a Rust plugin as a console command
            Command.ConsoleCommand consoleCommand;
            if (cmdlib.consoleCommands.TryGetValue(fullname, out consoleCommand))
            {
                if (consoleCommand.OriginalCallback != null) newCommand.OriginalCallback = consoleCommand.OriginalCallback;

                var previousPluginName = consoleCommand.Callback.Plugin?.Name ?? "an unknown plugin";
                var newPluginName = plugin?.Name ?? "An unknown plugin";
                var message = $"{newPluginName} has replaced the '{fullname}' console command previously registered by {previousPluginName}";
                Interface.Oxide.LogWarning(message);

                rustCommands.Remove(consoleCommand.RustCommand.namefull);
                ConsoleSystem.Index.GetAll().Remove(consoleCommand.RustCommand);
                cmdlib.consoleCommands.Remove(consoleCommand.RustCommand.namefull);
            }

            // Check if command is a vanilla Rust command
            ConsoleSystem.Command rustCommand;
            if (rustCommands.TryGetValue(fullname, out rustCommand))
            {
                if (rustCommand.isVariable)
                {
                    var newPluginName = plugin?.Name ?? "An unknown plugin";
                    Interface.Oxide.LogError($"{newPluginName} tried to register the {fullname} console variable as a command!");
                    return;
                }
                newCommand.OriginalCallback = rustCommand.Call;
            }

            // Create a new Rust console command
            newCommand.RustCommand = new ConsoleSystem.Command
            {
                name = name,
                parent = parent,
                namefull = command,
                isCommand = true,
                isUser = true,
                isAdmin = true,
                GetString = () => string.Empty,
                SetString = s => { },
                isVariable = false,
                Call = arg =>
                {
                    if (arg == null) return;

                    if (arg.connection != null)
                    {
                        if (arg.Player())
                        {
                            var iplayer = rustCovalence.PlayerManager.FindPlayer(arg.connection.userid.ToString()) as RustPlayer;
                            if (iplayer == null) return;
                            iplayer.LastCommand = CommandType.Console;
                            callback(iplayer, command, ExtractArgs(arg));
                            return;
                        }
                    }
                    callback(consolePlayer, command, ExtractArgs(arg));
                }
            };

            // Register the command as a console command
            rustCommands[fullname] = newCommand.RustCommand;
            ConsoleSystem.Index.GetAll().Add(newCommand.RustCommand);

            // Register the command as a chat command
            registeredCommands[command] = newCommand;
        }

        #endregion

        #region Command Unregistration

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            // Initialize if needed
            if (rustCommands == null) rustCommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary<string, ConsoleSystem.Command>;

            RegisteredCommand cmd;
            if (!registeredCommands.TryGetValue(command, out cmd)) return;

            // Check if the command belongs to the plugin
            if (plugin != cmd.Source) return;

            // Setup console command name
            var split = command.Split('.');
            var parent = split.Length >= 2 ? split[0].Trim() : "global";
            var name = split.Length >= 2 ? string.Join(".", split.Skip(1).ToArray()) : split[0].Trim();
            var fullname = $"{parent}.{name}";

            // Remove the chat command
            registeredCommands.Remove(command);

            // If this was originally a vanilla Rust command then restore it, otherwise remove it
            if (cmd.OriginalCallback != null)
            {
                rustCommands[fullname].Call = cmd.OriginalCallback;
            }
            else
            {
                rustCommands.Remove(fullname);
                ConsoleSystem.Index.GetAll().Remove(cmd.RustCommand);
            }
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        #endregion

        #region Command Overriding

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command)
        {
            var split = command.Split('.');
            var parent = split.Length >= 2 ? split[0].Trim() : "global";
            var name = split.Length >= 2 ? string.Join(".", split.Skip(1).ToArray()) : split[0].Trim();
            var fullname = $"{parent}.{name}";

            RegisteredCommand cmd;
            if (registeredCommands.TryGetValue(command, out cmd))
                if (cmd.Source.IsCorePlugin)
                    return false;

            Command.ChatCommand chatCommand;
            if (cmdlib.chatCommands.TryGetValue(command, out chatCommand))
                if (chatCommand.Plugin.IsCorePlugin)
                    return false;

            Command.ConsoleCommand consoleCommand;
            if (cmdlib.consoleCommands.TryGetValue(fullname, out consoleCommand))
                if (consoleCommand.Callback.Plugin.IsCorePlugin)
                    return false;

            return !RustCore.RestrictedCommands.Contains(command) && !RustCore.RestrictedCommands.Contains(fullname);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Extract the arguments from a ConsoleSystem.Arg object
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static string[] ExtractArgs(ConsoleSystem.Arg arg)
        {
            if (arg == null) return new string[0];

            var argsList = new List<string>();
            var i = 0;
            while (arg.HasArgs(++i)) argsList.Add(arg.GetString(i - 1));
            return argsList.ToArray();
        }

        #endregion
    }
}
