using System;
using System.Net;

using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class RustLegacyServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return server.hostname; }
            set { server.hostname = value; }
        }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => IPAddress.Parse(Rust.Steam.Server.SteamServer_GetPublicIP().ToString());

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)uLink.MasterServer.port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => Rust.Defines.Connection.protocol.ToString();

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => NetCull.connections.Length;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return NetCull.maxConnections; }
            set { server.maxplayers = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return DateTime.Today.AddHours(EnvironmentControlCenter.Singleton.GetTime()); }
            set { EnvironmentControlCenter.Singleton.SetTime(value.Hour); }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => ServerSaveManager.AutoSave();

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            UnityEngine.Debug.Log($"[Broadcast] {message}");
            ConsoleNetworker.Broadcast($"chat.add Server {message.Quote()}");
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ConsoleSystem.Run($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");

        #endregion
    }
}
