using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Game.SevenDays.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class SevenDaysCommandSystem : ICommandSystem
    {
        #region Initialization

        // The covalence provider
        private readonly SevenDaysCovalenceProvider sevenDaysCovalence = SevenDaysCovalenceProvider.Instance;

        // The command library
        //private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The console player
        //private readonly SevenDaysConsolePlayer consolePlayer;

        // Command handler
        private readonly CommandHandler commandHandler;

        // All registered commands
        internal IDictionary<string, CommandCallback> registeredCommands;

        /// <summary>
        /// Initializes the command system
        /// </summary>
        public SevenDaysCommandSystem()
        {
            registeredCommands = new Dictionary<string, CommandCallback>();
            commandHandler = new CommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
            //consolePlayer = new SevenDaysConsolePlayer();
        }

        private bool ChatCommandCallback(IPlayer caller, string command, string[] args)
        {
            CommandCallback callback;
            return registeredCommands.TryGetValue(command, out callback) && callback(caller, command, args);
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

            // Check if the command can be overridden
            //if (!CanOverrideCommand(command))
            //    throw new CommandAlreadyExistsException(command);

            // Check if command already exists
            if (registeredCommands.ContainsKey(command))
                throw new CommandAlreadyExistsException(command);

            // Register command
            registeredCommands.Add(command, callback);
        }

        #endregion Command Registration

        #region Command Unregistration

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin) => registeredCommands.Remove(command);

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

        /*/// <summary>
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
                if (consoleCommand.PluginCallbacks[0].Plugin.IsCorePlugin)
                    return false;

            return !SevenDaysCore.RestrictedCommands.Contains(command) && !SevenDaysCore.RestrictedCommands.Contains(fullname);
        }*/

        #endregion Command Overriding
    }
}
