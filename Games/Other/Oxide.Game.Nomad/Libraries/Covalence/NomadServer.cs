using System;
using System.Net;

using TNet;

using Oxide.Core.Libraries.Covalence;

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
            get { return LobbyServerLink.mGameServer.name; }
            set { LobbyServerLink.mGameServer.name = value; }
        }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => Tools.externalAddress;

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => Convert.ToUInt16(LobbyServerLink.mGameServer.tcpPort);

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => LobbyServerLink.mGameServer.clientVersion;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => LobbyServerLink.mGameServer.playerCount;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return LobbyServerLink.mGameServer.playerLimit; }
            set { LobbyServerLink.mGameServer.playerLimit = (ushort)value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return new DateTime(DateTime.UtcNow.Ticks / 10000); } // TODO
            set { throw new NotImplementedException(); } // TODO
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => LobbyServerLink.mGameServer.SaveTo("server.dat");

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            // TODO
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO
        }

        #endregion
    }

    public static class ExtensionMethods
    {
        /// <summary>
        /// Adds compatible style formatting to text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Stylize(this string text)
        {
            // TODO: Strip styling, the game doesn't support it?
            return text;
        }
    }
}
