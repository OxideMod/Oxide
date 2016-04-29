using System.Collections.Generic;

using CodeHatch.Engine.Core.Commands;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class ReignOfKingsCommandSystem : ICommandSystem
    { 
        // Default constructor
        public ReignOfKingsCommandSystem()
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

        private bool ChatCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            CommandCallback callback;
            return registeredCommands.TryGetValue(cmd, out callback) && callback(caller, cmd, args);
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandCallback callback)
        {
            // No console command support so no need to register the command as console command
            // Register the command as a chat command
            // Convert to lowercase
            var commandName = cmd.ToLowerInvariant();

            // Check if it already exists
            if (CommandManager.RegisteredCommands.ContainsKey(commandName) || registeredCommands.ContainsKey(commandName))
                throw new CommandAlreadyExistsException(commandName);

            registeredCommands.Add(commandName, callback);
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        public void UnregisterCommand(string cmd) => CommandManager.RegisteredCommands.Remove(cmd);

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleChatMessage(ILivePlayer player, string str) => commandHandler.HandleChatMessage(player, str);
    }
}
