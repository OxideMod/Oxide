using System;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic connected player within a game
    /// </summary>
    public interface ILivePlayer
    {
        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        IPlayer BasePlayer { get; }

        /// <summary>
        /// Gets a reference to this player's character, if available
        /// </summary>
        object Character { get; }

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        void Kick(string reason);

        #endregion

        #region Manipulation

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        void Kill();

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void Teleport(float x, float y, float z);

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        void SendChatMessage(string message);

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        void RunCommand(string command, params object[] args);

        #endregion
    }
}
