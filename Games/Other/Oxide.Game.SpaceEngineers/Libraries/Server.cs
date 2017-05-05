using Oxide.Core.Libraries;
using Sandbox.Engine.Multiplayer;
using System;

namespace Oxide.Game.SpaceEngineers.Libraries
{
    public class Server : Library
    {
        // Game references
        //internal static readonly BanManager BanManager = BanManager.Instance;
        //internal static readonly ChatManagerServer ChatManager = ChatManagerServer.Instance;
        //internal static readonly ConsoleManager ConsoleManager = ConsoleManager.Instance;

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned(id)) return;

            // Ban and kick user
            //BanManager.AddBan(Convert.ToUInt64(id));
        }

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => false;// BanManager.IsBanned(Convert.ToUInt64(id));

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (!IsBanned(id)) return;

            // Set to unbanned
            //BanManager.RemoveBan(Convert.ToUInt64(id));
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>ServerInstance.Broadcast();
        /// <param name="prefix"></param>
        public void Broadcast(string message, string prefix = null)
        {

            MyMultiplayer.Static.SendChatMessage(prefix != null ? $"{prefix} {message}" : message);
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
        //public void Command(string command, params object[] args) => ConsoleManager.ExecuteCommand($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");


        #endregion
    }
}
