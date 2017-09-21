using System;
using System.Globalization;
using System.Net;
using Oxide.Core.Libraries.Covalence;
using TNet;

namespace Oxide.Game.Nomad.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class NomadServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return Application.mGameServer.name; }
            set { Application.mGameServer.name = value; }
        }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => Tools.externalAddress;

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => Convert.ToUInt16(Application.mGameServer.tcpPort);

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => Application.mGameServer.clientVersion;

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
        public int Players => Application.mGameServer.playerCount;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return Application.mGameServer.playerLimit; }
            set { Application.mGameServer.playerLimit = (ushort)value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return new DateTime(DateTime.UtcNow.Ticks / 10000); } // TODO: Implement when possible
            set { throw new NotImplementedException(); } // TODO: Implement when possible
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
            // TODO: Implement when possible
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => TimeSpan.Zero; // TODO: Implement when possible

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => false; // TODO: Implement when possible

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => Application.mGameServer.SaveTo("server.dat");

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (!IsBanned(id)) return;

            // Set to unbanned
            // TODO: Implement when possible
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            // TODO: Implement when possible
        }

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
