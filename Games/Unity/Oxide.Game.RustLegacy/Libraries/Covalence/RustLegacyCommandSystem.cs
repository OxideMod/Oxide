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
        //private ChatCommandHandler chatCommandHandler;

        // All registered chat commands
        //private IDictionary<string, CommandCallback> registeredChatCommands;

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

        /*private bool ChatCommandCallback(string cmd, IPlayer caller, string[] args)
        {
            CommandCallback callback;
            return registeredChatCommands.TryGetValue(cmd, out callback) && callback(caller, cmd, args);
        }*/

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandCallback callback)
        {
            // TODO: Register a covalence command as both chat and console command
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
        public void UnregisterCommand(string cmd)
        {
            // TODO: Unregister a covalence command
        }

        /*/// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleChatMessage(ILivePlayer player, string str) => chatCommandHandler.HandleChatMessage(player, str);*/
    }
}
