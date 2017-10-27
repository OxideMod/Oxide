using Oxide.Core.Libraries;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using System;

namespace Oxide.Game.MedievalEngineers.Libraries
{
    public class Server : Library
    {
        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            if (!IsBanned(id)) MyMultiplayer.Static.BanClient(Convert.ToUInt64(id), true);
        }

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => MySandboxGame.ConfigDedicated.Banned.Contains(Convert.ToUInt64(id));

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            if (IsBanned(id)) MyMultiplayer.Static.BanClient(Convert.ToUInt64(id), false);
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
        public void Command(string command, params object[] args)
        {
            // TODO: Implement when possible
            //ConsoleManager.ExecuteCommand($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
        }

        #endregion Chat and Commands
    }
}
