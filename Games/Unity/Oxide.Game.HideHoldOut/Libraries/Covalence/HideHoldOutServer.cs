using System;
using System.Net;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.HideHoldOut.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class HideHoldOutServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return NetworkController.NetManager_.ServManager.Server_NAME; }
            set { NetworkController.NetManager_.ServManager.Server_NAME = value; }
        }

        private static IPAddress address;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                try
                {
                    if (address == null)
                    {
                        var webClient = new WebClient();
                        address = IPAddress.Parse(webClient.DownloadString("http://api.ipify.org"));
                        return address;
                    }
                    return address;
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server IP address", ex);
                    return new IPAddress(0);
                }
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)uLink.MasterServer.port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => NetworkController.NetManager_.get_GAME_VERSION;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => NetworkController.NetManager_.get_GAME_VERSION_steam;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => NetworkController.NetManager_.ServManager.CheckPlayerCount();

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return NetworkController.NetManager_.ServManager.NumberOfSlot; }
            set { NetworkController.NetManager_.ServManager.NumberOfSlot = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return DateTime.Today.AddMinutes(NetworkController.NetManager_.TimeManager.TIME_display); }
            set { NetworkController.NetManager_.TimeManager.SetTime(value.Minute); }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => NetworkController.NetManager_.ServManager.StartCoroutine("Manual_DataBase_Storing");

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => NetworkController.NetManager_.chatManager.Send_msg(message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Implement when possible
        }

        #endregion
    }
}
