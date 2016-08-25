using System.Collections.Generic;

using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class RustLegacyCommandSystem : ICommandSystem
    {
        // The covalence provider
        private readonly RustLegacyCovalenceProvider rustCovalence = RustLegacyCovalenceProvider.Instance;

        // The console player
        private RustLegacyConsolePlayer consolePlayer;

        // Chat command handler
        private ChatCommandHandler chatCommandHandler;

        // All registered commands
        private IDictionary<string, CommandCallback> registeredCommands;

        // Default constructor
        public RustLegacyCommandSystem()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        private void Initialize()
        {
            registeredCommands = new Dictionary<string, CommandCallback>();
            chatCommandHandler = new ChatCommandHandler(ChatCommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new RustLegacyConsolePlayer();
        }

        private bool ChatCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            CommandCallback callback;
            return registeredCommands.TryGetValue(cmd, out callback) && callback(caller, cmd, args);
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            // Initialize if needed
            if (registeredCommands == null) Initialize();

            // Convert to lowercase
            var commandName = command.ToLowerInvariant();

            // Setup console command name
            var split = commandName.Split('.');
            var parent = split.Length >= 2 ? split[0].Trim() : "global";
            var name = split.Length >= 2 ? split[1].Trim() : split[0].Trim();
            var fullname = $"{parent}.{name}";
            
            // Check if it already exists
            if (registeredCommands.ContainsKey(commandName) || Command.ChatCommands.ContainsKey(commandName) || Command.ConsoleCommands.ContainsKey(fullname))
                throw new CommandAlreadyExistsException(commandName);
            
            // Register it
            registeredCommands.Add(commandName, callback);
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        public void UnregisterCommand(string command, Plugin plugin) => registeredCommands.Remove(command);

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string str) => chatCommandHandler.HandleChatMessage(player, str);

        /// <summary>
        /// Handles a console message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleConsoleMessage(IPlayer player, string str)=> chatCommandHandler.HandleConsoleMessage(player ?? consolePlayer, str);
    }
}
