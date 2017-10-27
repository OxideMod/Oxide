using Oxide.Core.Libraries;
using System;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Server : Library
    {
        #region Initialization

        internal static readonly BanManager BanManager = BanManager.Instance;
        internal static readonly ChatManagerServer ChatManager = ChatManagerServer.Instance;
        internal static readonly ConsoleManager ConsoleManager = ConsoleManager.Instance;

        #endregion Initialization

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            if (!IsBanned(id)) BanManager.AddBan(Convert.ToUInt64(id));
        }

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => BanManager.IsBanned(Convert.ToUInt64(id));

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            if (IsBanned(id)) BanManager.RemoveBan(Convert.ToUInt64(id));
        }

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>ServerInstance.Broadcast();
        /// <param name="prefix"></param>
        public void Broadcast(string message, string prefix = null)
        {
            ConsoleManager.SendLog($"[Chat] {message}");
#if ITEMV2
            ChatManager.SendChatMessage(new ServerChatMessage(prefix != null ? $"{prefix} {message}" : message));
#else
            ChatManager.RPC("RelayChat", uLink.RPCMode.Others, prefix != null ? $"{prefix} {message}" : message);
#endif
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

        #endregion Chat and Commands
    }
}
