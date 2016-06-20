namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic connected player within a game
    /// </summary>
    public interface ILivePlayer
    {
        /// <summary>
        /// Gets the base player of the user
        /// </summary>
        IPlayer BasePlayer { get; }

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        IPlayerCharacter Character { get; }

        /// <summary>
        /// Gets the player's last used command type
        /// </summary>
        CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        int Ping { get; }

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        bool IsAdmin { get; }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        void Kick(string reason);

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        void Kill();

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void Teleport(float x, float y, float z);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void Message(string message, params object[] args);

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void Reply(string message, params object[] args);

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        void Command(string command, params object[] args);

        #endregion
    }
}
