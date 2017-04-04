using System;
using Oxide.Core.Libraries;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Server : Library
    {
        // Game references
        internal static readonly BanManager BanManager = BanManager.Instance;
        internal static readonly ChatManagerServer ChatManager = ChatManagerServer.Instance;
        internal static readonly ConsoleManager ConsoleManager = ConsoleManager.Instance;

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>ServerInstance.Broadcast();
        /// <param name="prefix"></param>
        public void Broadcast(string message, string prefix = null)
        {
            ConsoleManager.SendLog($"[Chat] {message}");
            ChatManager.RPC("RelayChat", uLink.RPCMode.Others, prefix != null ? $"{prefix} {message}" : message);
        }

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix = null, params object[] args) => Broadcast(string.Format(message, args), prefix);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ConsoleManager.ExecuteCommand($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");

        #endregion
    }
}
