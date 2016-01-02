using System.Collections.Generic;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class RustLegacyCommandSystem : ICommandSystem
    {
        // Default constructor
        public RustLegacyCommandSystem()
        {
            Initialize();
        }

        // A reference to Rust Legacy's internal command dictionary
        //private IDictionary<string, ConsoleSystem.Command> rustCommands;

        // Chat command handler
        private ChatCommandHandler chatCommandHandler;

        // All registered chat commands
        private IDictionary<string, CommandCallback> registeredChatCommands;

        // The console player
        //private RustLegacyConsolePlayer consolePlayer;

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        private void Initialize()
        {
            /*rustCommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary<string, ConsoleSystem.Command>;
            registeredChatCommands = new Dictionary<string, CommandCallback>();
            chatCommandHandler = new ChatCommandHandler(ChatCommandCallback, registeredChatCommands.ContainsKey);
            consolePlayer = new RustLegacyConsolePlayer();*/
        }

        private bool ChatCommandCallback(string cmd, CommandType type, IPlayer caller, string[] args)
        {
            CommandCallback callback;
            return registeredChatCommands.TryGetValue(cmd, out callback) && callback(cmd, type, caller, args);
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandType type, CommandCallback callback)
        {
            /*// Initialize if needed
            if (rustCommands == null) Initialize();

            // Convert to lowercase
            var command_name = cmd.ToLowerInvariant();

            // Is it a console command?
            if (type == CommandType.Console)
            {
                // Check if it already exists
                if (rustCommands != null && rustCommands.ContainsKey(command_name))
                {
                    throw new CommandAlreadyExistsException(command_name);
                }

                // Register it
                var splitName = command_name.Split('.');
                rustCommands?.Add(command_name, new ConsoleSystem.Command
                {
                    name = splitName.Length >= 2 ? splitName[1] : splitName[0],
                    parent = splitName.Length >= 2 ? splitName[0] : string.Empty,
                    namefull = command_name,
                    isCommand = true,
                    isUser = true,
                    isAdmin = true,
                    GetString = () => string.Empty,
                    SetString = (s) => { },
                    Call = (arg) =>
                    {
                        if (arg == null) return;
                        callback(command_name, CommandType.Console, consolePlayer, ExtractArgs(arg));
                    }
                });
            }
            else if (type == CommandType.Chat)
            {
                registeredChatCommands.Add(command_name, callback);
            }*/
        }

        private static string[] ExtractArgs(ConsoleSystem.Arg arg)
        {
            if (arg == null) return new string[0];
            var argsList = new List<string>();
            var i = 0;
            while (arg.HasArgs(++i)) argsList.Add(arg.GetString(i - 1));
            return argsList.ToArray();
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        public void UnregisterCommand(string cmd, CommandType type)
        {
            /*// Initialize if needed
            if (rustCommands == null) Initialize();

            // Is it a console command?
            if (type == CommandType.Console)
                rustCommands?.Remove(cmd);
            else if (type == CommandType.Chat)
                registeredChatCommands.Remove(cmd);*/
        }

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleChatMessage(ILivePlayer player, string str) => chatCommandHandler.HandleChatMessage(player, str);
    }
}
