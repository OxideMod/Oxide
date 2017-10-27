using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Globalization;
using System.Net;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class HurtworldServer : IServer
    {
        #region Initialization

        internal readonly Server Server = new Server();
        internal static readonly BanManager BanManager = BanManager.Instance;

        #endregion Initialization

        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return GameManager.Instance.ServerConfig.GameName; }
            set { GameManager.Instance.ServerConfig.GameName = value; }
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
        public ushort Port => (ushort)GameManager.Instance.ServerConfig.Port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => GameManager.Instance.Version.ToString();

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => GameManager.PROTOCOL_VERSION.ToString();

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => GameManager.Instance.GetPlayerCount();

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return GameManager.Instance.ServerConfig.MaxPlayers; }
            set { GameManager.Instance.ServerConfig.MaxPlayers = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get
            {
                var time = TimeManager.Instance.GetCurrentGameTime();
                return Convert.ToDateTime($"{time.Hour}:{time.Minute}:{Math.Floor(time.Second)}");
            }
            set
            {
                var currentOffset = TimeManager.Instance.GetCurrentGameTime().offset;
                var daysPassed = TimeManager.Instance.GetCurrentGameTime().Day + 1;
                var newOffset = 86400 * daysPassed - currentOffset + value.TimeOfDay.TotalSeconds;
                TimeManager.Instance.InitialTimeOffset += (float)newOffset;
            }
        }

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            if (!IsBanned(id)) Server.Ban(id, reason, duration);
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => TimeSpan.MaxValue;

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => Server.IsBanned(id);

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => Command("saveserver");

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            if (IsBanned(id)) Server.Unban(id);
        }

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => Server.Broadcast(message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => Server.Command(command, args);

        #endregion Chat and Commands
    }
}
