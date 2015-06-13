using System;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a Rust connected player
    /// </summary>
    public class RustLivePlayer : ILivePlayer
    {
        private ulong steamid;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer { get { return RustCovalenceProvider.Instance.PlayerManager.GetPlayer(steamid.ToString()); } }

        /// <summary>
        /// Gets a reference to this player's character, if available
        /// </summary>
        public object Character { get; private set; }

        private BasePlayer rustPlayer;

        internal RustLivePlayer(BasePlayer rustPlayer)
        {
            this.rustPlayer = rustPlayer;
            steamid = rustPlayer.net.connection.userid;
            Character = rustPlayer;
        }

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            rustPlayer.Kick(reason);
        }

        #endregion

        #region Manipulation

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill()
        {
            //var hitInfo = new HitInfo(rustPlayer, global::Rust.DamageType.Generic, rustPlayer.health, rustPlayer.GetComponent<UnityEngine.Transform>().position);
            //rustPlayer.Hurt(hitInfo, false);
            rustPlayer.Die();
        }

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            // Presumably we can't just set the transform.position
        }

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string message)
        {
            rustPlayer.ChatMessage(message);
        }

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        public void RunCommand(string command, params object[] args)
        {
            rustPlayer.SendConsoleCommand(command, args);
        }

        #endregion
    }
}
