using System.Net;

using ConVar;
using Rust;
using Steamworks;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class RustServer : IServer
    {
        private ServerMgr mgr;

        /// <summary>
        /// Initializes a new instance of the RustServer class
        /// </summary>
        public RustServer()
        {
            mgr = ServerMgr.Instance;
        }

        #region Server Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => Server.hostname;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                var ip = SteamGameServer.GetPublicIP();
                return ip == 0 ? null : new IPAddress(ip >> 24 | ((ip & 0xff0000) >> 8) | ((ip & 0xff00) << 8) | ((ip & 0xff) << 24));
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)Server.port;

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => Protocol.printable;

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => ConsoleSystem.Broadcast("chat.add", 0, message, 1.0);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ConsoleSystem.Run.Server.Normal(command, args);

        #endregion
    }
}
