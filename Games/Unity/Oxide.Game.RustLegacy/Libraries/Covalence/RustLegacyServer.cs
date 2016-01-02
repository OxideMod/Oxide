using System.Net;

using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class RustLegacyServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => server.hostname;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => IPAddress.Parse(uLink.MasterServer.ipAddress);

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)uLink.MasterServer.port;

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => Rust.Defines.Connection.protocol.ToString();

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => ConsoleNetworker.Broadcast($"chat.add \"Server\" {message.Quote()}");

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RunCommand(string command, params object[] args) => ConsoleSystem.Run(command);

        #endregion
    }
}
