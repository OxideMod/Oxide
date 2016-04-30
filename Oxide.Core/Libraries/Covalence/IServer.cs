using System.Net;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic server hosting the game instance
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        IPAddress Address { get; }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        ushort Port { get; }

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        string Version { get; }

        // TODO: Add string Protocol
        // TODO: Add int MaxPlayers

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
        void RunCommand(string command, params object[] args);
    }
}
