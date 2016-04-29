using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class RustCommandSystem : ICommandSystem
    {
        // Default constructor
        public RustCommandSystem()
        {
            Initialize();
        }

        // A reference to Rust's internal command dictionary
        private IDictionary<string, ConsoleSystem.Command> rustCommands;

        // Chat command handler
        private ChatCommandHandler chatCommandHandler;

        // All registered chat commands
        private IDictionary<string, CommandCallback> registeredChatCommands;

        // The console player
        private RustConsolePlayer consolePlayer;

        // The covalence provider
        private readonly RustCovalenceProvider rustCovalence = RustCovalenceProvider.Instance;

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        private void Initialize()
        {
            rustCommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary<string, ConsoleSystem.Command>;
            registeredChatCommands = new Dictionary<string, CommandCallback>();
            chatCommandHandler = new ChatCommandHandler(ChatCommandCallback, registeredChatCommands.ContainsKey);
            consolePlayer = new RustConsolePlayer();
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
            // Initialize if needed
            if (rustCommands == null) Initialize();

            // Convert to lowercase
            var commandName = cmd.ToLowerInvariant();

            // Is it a console command?
            if (type == CommandType.Console)
            {
                // Check if it already exists
                if (rustCommands != null && rustCommands.ContainsKey(commandName))
                    throw new CommandAlreadyExistsException(commandName);

                // Register it
                var splitName = commandName.Split('.');
                rustCommands?.Add(commandName, new ConsoleSystem.Command
                {
                    name = splitName.Length >= 2 ? splitName[1] : splitName[0],
                    parent = splitName.Length >= 2 ? splitName[0] : string.Empty,
                    namefull = commandName,
                    isCommand = true,
                    isUser = true,
                    isAdmin = true,
                    GetString = () => string.Empty,
                    SetString = s => { },
                    Call = arg =>
                    {
                        if (arg == null) return;

                        if (arg.connection != null)
                        {
                            if (arg.Player())
                            {
                                var livePlayer = rustCovalence.PlayerManager.GetOnlinePlayer(arg.connection.userid.ToString()) as RustLivePlayer;
                                livePlayer.LastCommand = CommandType.Console;
                                livePlayer.LastArg = arg;
                                callback(commandName, CommandType.Console, livePlayer?.BasePlayer, ExtractArgs(arg));
                                return;
                            }
                        }
                        callback(commandName, CommandType.Console, consolePlayer, ExtractArgs(arg));
                    }
                });
            }
            else if (type == CommandType.Chat)
                registeredChatCommands.Add(commandName, callback);
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
            // Initialize if needed
            if (rustCommands == null) Initialize();

            // Is it a console command?
            if (type == CommandType.Console)
                rustCommands?.Remove(cmd);
            else if (type == CommandType.Chat)
                registeredChatCommands.Remove(cmd);
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
