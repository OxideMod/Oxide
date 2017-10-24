using System;
using System.Globalization;
using System.Net;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.FortressCraft.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class FortressCraftServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return ServerConsole.WorldName; }
            set { ServerConsole.WorldName = value; }
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
        public ushort Port => NetworkServerThread.GAME_PORT;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version
        {
            get
            {
                var index = HUDManager.Version.IndexOf(" -", StringComparison.Ordinal);
                return index > 0 ? HUDManager.Version.Substring(0, index) : "Unknown";
            }
        }

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => NetworkManager.instance.mServerThread.GetNumPlayers();

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return NetworkServerThread.mnPublicMaxPlayerCount; }
            set { ServerConsole.MaxPlayers = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return DateTime.FromOADate(WorldScript.instance.mWorldData.mrCurrentTimeOfDay); }
            set { WorldScript.instance.mWorldData.mrCurrentTimeOfDay = value.Second; }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned(id)) return;

            // Ban and kick user
            NetworkManager.instance.mBanListManager.UnbanPlayer(ulong.Parse(id));
            //NetworkManager.instance.mServerThread.KickPlayer(player.mConnection, reason); // TODO: Implement if possible
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
        public bool IsBanned(string id) => NetworkManager.instance.mBanListManager.CheckBan(ulong.Parse(id));

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => WorldScript.instance.SaveWorldSettings();

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (!IsBanned(id)) return;

            // Set to unbanned
            NetworkManager.instance.mBanListManager.UnbanPlayer(ulong.Parse(id));
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            var chatLine = new ChatLine
            {
                mPlayer = -1,
                mPlayerName = "", // TODO: Test if prefix can be left out
                mText = message,
                mType = ChatLine.Type.Normal
            };
            NetworkManager.instance.QueueChatMessage(chatLine);
            ServerConsole.DebugLog($"[SERVER] {message}");
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ServerConsole.DoServerString($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");

        #endregion
    }
}
