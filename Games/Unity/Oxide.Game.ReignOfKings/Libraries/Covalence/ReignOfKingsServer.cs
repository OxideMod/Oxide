using System;
using System.Net;
using CodeHatch.Build;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events;
using CodeHatch.Networking.Events.WorldEvents.TimeEvents;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class ReignOfKingsServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return DedicatedServerBypass.Settings.ServerName; }
            set { DedicatedServerBypass.Settings.ServerName = value; }
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
        public ushort Port => CodeHatch.Engine.Core.Gaming.Game.ServerData.Port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => GameInfo.VersionString;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => GameInfo.VersionName;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => Server.PlayerCount;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return Server.PlayerLimit; }
            set { Server.PlayerLimit = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return DateTime.Today.AddHours(GameClock.Instance.TimeOfDay); }
            set { EventManager.CallEvent(new TimeSetEvent(value.Hour, GameClock.Instance.DaySpeed)); }
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
            Server.Ban(ulong.Parse(id), (int)duration.TotalSeconds, reason);
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => new DateTime(Server.GetBannedPlayerFromPlayerId(ulong.Parse(id)).ExpireDate) - DateTime.Now;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => Server.IdIsBanned(ulong.Parse(id));

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => CodeHatch.Engine.Core.Gaming.Game.Save();

        /// <summary>
        /// Unbans the user
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check not banned
            if (!IsBanned(id)) return;

            // Set to unbanned
            Server.Unban(ulong.Parse(id));
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => Server.BroadcastMessage(message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, $"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
        }

        #endregion
    }
}
