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
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => HideHoldOutCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

        /// <summary>
        /// Gets this player's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets this player's average network ping
        /// </summary>
        public int Ping => player.NetPlayer.averagePing;

        /// <summary>
        /// Gets the owner of this character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Gets this player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

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
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => uLink.Network.CloseConnection(player.NetPlayer, true);

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill() => NetworkController.Player_ctrl_.TakeDamage(100, Vector3.zero, string.Empty, true, true, string.Empty);

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => player.Transfo.position = new Vector3(x, y, z);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => HideHoldOutCore.ChatNetView.RPC("NET_Receive_msg", player.NetPlayer, "\n" + message, chat_msg_type.standard, player.account_id);

        /// <summary>
        /// Replies to the user
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message);

        /// <summary>
        /// Runs the specified console command on this player's client
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
        /// Gets the position of this character
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
        /// Gets the position of this character
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
