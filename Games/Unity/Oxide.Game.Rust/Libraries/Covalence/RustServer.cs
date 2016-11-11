using System;
using System.Net;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class RustServer : IServer
    {
        #region Information

        private static IPAddress address;

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return ConVar.Server.hostname; }
            set { ConVar.Server.hostname = value; }
        }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                if (address != null) return address;
                var webClient = new WebClient();
                address = IPAddress.Parse(webClient.DownloadString("https://api.ipify.org"));
                return address;
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)ConVar.Server.port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => BuildInformation.VersionStampDays.ToString();

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => global::Rust.Protocol.network.ToString();

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => BasePlayer.activePlayerList.Count;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return ConVar.Server.maxplayers; }
            set { ConVar.Server.maxplayers = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return TOD_Sky.Instance.Cycle.DateTime; }
            set { TOD_Sky.Instance.Cycle.DateTime = value; }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            ConVar.Server.save(null);
            ConVar.Server.writecfg(null);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => ConsoleNetwork.BroadcastToAllClients("chat.add", 0, message, 1.0);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ConsoleSystem.Run.Server.Normal(command, args);

        #endregion
    }
}
