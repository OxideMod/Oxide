using System;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a Rust connected player
    /// </summary>
    public class RustLivePlayer : ILivePlayer, IPlayerCharacter
    {
        private ulong steamid;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer { get { return RustCovalenceProvider.Instance.PlayerManager.GetPlayer(steamid.ToString()); } }

        /// <summary>
        /// Gets this player's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of this character
        /// </summary>
        public ILivePlayer Owner { get { return this; } }

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        public object Object { get; private set; }

        private BasePlayer rustPlayer;

        internal RustLivePlayer(BasePlayer rustPlayer)
        {
            this.rustPlayer = rustPlayer;
            steamid = rustPlayer.net.connection.userid;
            Object = rustPlayer;
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

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void GetPosition(out float x, out float y, out float z)
        {
            var pos = rustPlayer.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <returns></returns>
        public GenericPosition GetPosition()
        {
            var pos = rustPlayer.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #region Manipulation

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill()
        {
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
            if (rustPlayer.IsSpectating()) return;
            var dest = new UnityEngine.Vector3(x, y, z);
            rustPlayer.transform.position = dest;
            rustPlayer.ClientRPCPlayer(null, rustPlayer, "ForcePositionTo", dest);
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
