using Oxide.Core.Libraries;

namespace Oxide.Game.Rust.Libraries
{
    public class Server : Library
    {
        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="userId"></param>
        /// <param name="prefix"></param>
        public void Broadcast(string message, string prefix = null, ulong userId = 0)
        {
            ConsoleNetwork.BroadcastToAllClients("chat.add", userId, prefix != null ? $"{prefix} {message}" : message, 1.0);
        }

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="userId"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix = null, ulong userId = 0, params object[] args) => Broadcast(string.Format(message, args), prefix, userId);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ConsoleSystem.Run(ConsoleSystem.Option.Server, command, args);

        #endregion Chat and Commands
    }
}
