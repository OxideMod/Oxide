using System;
using System.Net;

using Oxide.Core.Plugins;

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

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        void Save();

        #endregion

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

        #endregion

        #region Logging

        /// <summary>
        /// Logs a string of text to a file
        /// </summary>
        /// <param name="text"></param>
        /// <param name="owner"></param>
        void Log(string text, Plugin owner);

        #endregion
    }
}
