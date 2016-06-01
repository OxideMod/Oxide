using System;

using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.HideHoldOut.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class HideHoldOutLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamId;

        /// <summary>
        /// Gets the base player of the player
        /// </summary>
        public IPlayer BasePlayer => HideHoldOutCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of the character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs the character, if available
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => player.NetPlayer.ipAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => player.NetPlayer.averagePing;

        private readonly PlayerInfos player;

        internal HideHoldOutLivePlayer(PlayerInfos player)
        {
            this.player = player;
            steamId = Convert.ToUInt64(player.account_id);
            Character = this;
            Object = player.Transfo.gameObject;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin() => player.isADMIN;

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            NetworkController.NetManager_.NetView.RPC("NET_FATAL_ERROR", player.NetPlayer, reason);
            uLink.Network.CloseConnection(player.NetPlayer, true);
        }

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill() => NetworkController.Player_ctrl_.TakeDamage(100, Vector3.zero, string.Empty, true, true, string.Empty);

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => player.Transfo.position = new Vector3(x, y, z);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args)
        {
            HideHoldOutCore.ChatNetView.RPC("NET_Receive_msg", player.NetPlayer, "\n" + string.Format(message, args), chat_msg_type.standard, player.account_id);
        }

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args) => Message(string.Format(message, args));

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Is this even possible?
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            var pos = player.Transfo.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = player.Transfo.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
