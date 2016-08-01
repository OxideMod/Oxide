using System.Net;

using SDG.Unturned;
using Steamworks;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Unturned.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class UnturnedServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => Provider.serverName;

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
        public ushort Port => Provider.port;

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => Provider.APP_VERSION;

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => ChatManager.sendChat(EChatMode.GLOBAL, message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => Commander.execute(CSteamID.Nil, string.Format(command, args));

        #endregion
    }
}
