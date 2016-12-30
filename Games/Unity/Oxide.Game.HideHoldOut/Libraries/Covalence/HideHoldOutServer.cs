using System;
using System.Globalization;
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
                    if (address != null) return address;

                    var webClient = new WebClient();
                    IPAddress.TryParse(webClient.DownloadString("http://api.ipify.org"), out address);
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
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

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
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned(id)) return;

            // Ban and kick user
            NetworkController.NetManager_.DBManager.DB_AddBan(id);
            //if (IsConnected) Kick(reason); // TODO: Implement if possible
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => TimeSpan.MaxValue;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => NetworkController.NetManager_.DBManager.DB_Check_BAN(id);

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => NetworkController.NetManager_.ServManager.StartCoroutine("Manual_DataBase_Storing");

        /// <summary>
        /// Unbans the user
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check not banned
            if (!IsBanned(id)) return;

            // Set to unbanned
            NetworkController.NetManager_.DBManager.DB_RemoveBan(id);
        }

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
