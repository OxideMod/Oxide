using System;
using System.Globalization;
using System.Net;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic server hosting the game instance
    /// </summary>
    public interface IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        IPAddress Address { get; }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        ushort Port { get; }

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        string Protocol { get; }

        /// <summary>
        /// Gets the server's language
        /// </summary>
        CultureInfo Language { get; }

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        int Players { get; }

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        int MaxPlayers { get; set; }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        DateTime Time { get; set; }

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        void Ban(string id, string reason, TimeSpan duration = default(TimeSpan));

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        TimeSpan BanTimeRemaining(string id);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        bool IsBanned(string id);

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        void Save();

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        void Unban(string id);

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        void Broadcast(string message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        void Command(string command, params object[] args);

        #endregion Chat and Commands
    }
}
