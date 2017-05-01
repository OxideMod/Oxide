using System;
using System.Globalization;
using System.Net;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class RustServer : IServer
    {
        #region Initialiation

        internal readonly Server Server = new Server();

        #endregion

        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return ConVar.Server.hostname; }
            set { ConVar.Server.hostname = value; }
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
        public ushort Port => (ushort)ConVar.Server.port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => BuildInfo.Current.Build.Number;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => global::Rust.Protocol.printable;

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

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
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            if (IsBanned(id)) return;

            ServerUsers.Set(ulong.Parse(id), ServerUsers.UserGroup.Banned, Name, reason);
            ServerUsers.Save();
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => IsBanned(id) ? TimeSpan.MaxValue : TimeSpan.Zero;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => ServerUsers.Is(ulong.Parse(id), ServerUsers.UserGroup.Banned);

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            ConVar.Server.save(null);
            ConVar.Server.writecfg(null);
        }

        /// <summary>
        /// Unbans the user
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            if (!IsBanned(id)) return;

            ServerUsers.Remove(ulong.Parse(id));
            ServerUsers.Save();
        }

        #endregion

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

        #endregion
    }
}
