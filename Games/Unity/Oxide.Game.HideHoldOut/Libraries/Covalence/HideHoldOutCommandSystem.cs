using System.Collections.Generic;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.HideHoldOut.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class HideHoldOutCommandSystem : ICommandSystem
    {
        // Default constructor
        public HideHoldOutCommandSystem()
        {
            Initialize();
        }

        // Chat command handler
        private ChatCommandHandler commandHandler;

        // All registered commands
        private IDictionary<string, CommandCallback> registeredCommands;

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        private void Initialize()
        {
            registeredCommands = new Dictionary<string, CommandCallback>();
            commandHandler = new ChatCommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
        }

        private bool ChatCommandCallback(string cmd, CommandType type, IPlayer caller, string[] args)
        {
            CommandCallback callback;
            return registeredCommands.TryGetValue(cmd, out callback) && callback(cmd, type, caller, args);
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandType type, CommandCallback callback)
        {
            // Convert to lowercase
            var commandName = cmd.ToLowerInvariant();

            // Check if it already exists
            if (registeredCommands.ContainsKey(commandName))
                throw new CommandAlreadyExistsException(commandName);

            registeredCommands.Add(commandName, callback);
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        public void UnregisterCommand(string cmd, CommandType type) => registeredCommands.Remove(cmd);

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleChatMessage(ILivePlayer player, string str) => commandHandler.HandleChatMessage(player, str);
    }
}
