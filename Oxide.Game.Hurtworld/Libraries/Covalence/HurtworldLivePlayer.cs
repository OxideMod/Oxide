using System;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    class HurtworldLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly Guid guid;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => HurtworldCovalenceProvider.Instance.PlayerManager.GetPlayer(guid.ToString());

        /// <summary>
        /// Gets this player's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of this character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        public object Object { get; private set; }

        private PlayerIdentity player;

        internal HurtworldLivePlayer(PlayerIdentity player)
        {
            this.player = player;
            guid = player.PlayerGuid; // TODO: Switch to steamid once implemented
            Object = player;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            GameManager.Instance?.KickPlayer(guid.ToString(), reason);
        }

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill()
        {
            // TODO
            //GameManager.Instance?.KillPlayer(info);
        }

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            // TODO
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string message)
        {
            // TODO
        }

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RunCommand(string command, params object[] args)
        {
            // TODO
        }

        #endregion

        #region Location

        public void GetPosition(out float x, out float y, out float z)
        {
            // TODO
        }

        public GenericPosition GetPosition()
        {
            // TODO
        }

        #endregion
    }
}
