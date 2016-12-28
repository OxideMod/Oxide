using System;
using System.Net;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.World;

namespace Oxide.Game.SpaceEngineers.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class SpaceEngineersServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return MySandboxGame.ConfigDedicated.ServerName; } // TODO: Empty?!
            set { MySandboxGame.ConfigDedicated.ServerName = value; }
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
        public ushort Port => (ushort)MySandboxGame.ConfigDedicated.ServerPort;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => MyPerGameSettings.BasicGameInfo.GameVersion.ToString();

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => MyMultiplayer.Static.MemberCount;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return MyMultiplayer.Static.MemberLimit; }
            set { MyMultiplayer.Static.MemberLimit = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return MySession.Static.GameDateTime; }
            set { MySession.Static.GameDateTime = value; }
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
            MyMultiplayer.Static.BanClient(ulong.Parse(id), true);
            //if (IsConnected) Kick(reason); // TODO: Needed?
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
        public bool IsBanned(string id) => MySandboxGame.ConfigDedicated.Banned.Contains(ulong.Parse(id));

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => MySession.Static.Save();

        /// <summary>
        /// Unbans the user
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check not banned
            if (!IsBanned(id)) return;

            // Set to unbanned
            MyMultiplayer.Static.BanClient(ulong.Parse(id), false);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => MyMultiplayer.Static.SendChatMessage(message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Not possible yet?
        }

        #endregion
    }
}
