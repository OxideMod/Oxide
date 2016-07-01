using System.Collections.Generic;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class HurtworldCommandSystem : ICommandSystem
    {
        // Chat command handler
        private ChatCommandHandler commandHandler;

        // The console player
        public HurtworldConsolePlayer consolePlayer;

        // All registered commands
        private IDictionary<string, CommandCallback> registeredCommands;

        // Default constructor
        public HurtworldCommandSystem()
        {
            Initialize();
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, CommandCallback callback)
        {
            // Convert to lowercase
            var commandName = command.ToLowerInvariant();

            // Check if it already exists
            if (registeredCommands.ContainsKey(commandName))
                throw new CommandAlreadyExistsException(commandName);

            registeredCommands.Add(commandName, callback);
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        public void UnregisterCommand(string command) => registeredCommands.Remove(command);

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        private void Initialize()
        {
            registeredCommands = new Dictionary<string, CommandCallback>();
            commandHandler = new ChatCommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new HurtworldConsolePlayer();
        }

        private bool ChatCommandCallback(IPlayer caller, string command, string[] args)
        {
            CommandCallback callback;
            return registeredCommands.TryGetValue(command, out callback) && callback(caller, command, args);
        }

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(ILivePlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleConsoleMessage(ILivePlayer player, string message) => commandHandler.HandleConsoleMessage(player, message);
    }
}
