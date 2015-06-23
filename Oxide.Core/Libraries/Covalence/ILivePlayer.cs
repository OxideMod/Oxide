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
        /// Gets this player's in-game character, if available
        /// </summary>
        IPlayerCharacter Character { get; }

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        void Kick(string reason);

        #endregion

        #region Manipulation

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
