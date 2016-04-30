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

        /// <summary>
        /// Gets the player's last used command type
        /// </summary>
        CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets this player's average network ping
        /// </summary>
        int Ping { get; }

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        void Kick(string reason);

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

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        void Message(string message);

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        void RunCommand(string command, params object[] args);

        /// <summary>
        /// Replies to the user
        /// </summary>
        /// <param name="message"></param>
        void Reply(string message);

        #endregion
    }
}
