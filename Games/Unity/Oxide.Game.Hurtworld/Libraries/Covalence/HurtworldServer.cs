using System;
using System.IO;
using System.Net;

using Steamworks;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class HurtworldServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return GameManager.Instance.ServerConfig.GameName; }
            set { GameManager.Instance.ServerConfig.GameName = value; }
        }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                var ip = SteamGameServer.GetPublicIP();
                return ip == 0 ? null : new IPAddress(ip >> 24 | ((ip & 0xff0000) >> 8) | ((ip & 0xff00) << 8) | ((ip & 0xff) << 24));
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)uLink.MasterServer.port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => GameManager.Instance.Version.ToString();

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => GameManager.PROTOCOL_VERSION.ToString();

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return GameManager.Instance.ServerConfig.MaxPlayers; }
            set { GameManager.Instance.ServerConfig.MaxPlayers = value; }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => Command("saveserver");

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            ConsoleManager.SendLog(string.Concat("[Broadcast] ", message));
            ChatManagerServer.Instance.RPC("RelayChat", uLink.RPCMode.Others, message);
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            ConsoleManager.Instance.ExecuteCommand($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
        }

        #endregion

        #region Logging

        /// <summary>
        /// Logs a string of text to a file
        /// </summary>
        /// <param name="text"></param>
        /// <param name="owner"></param>
        public void Log(string text, Plugin owner)
        {
            using (var writer = new StreamWriter(Path.Combine(Interface.Oxide.LogDirectory, Utility.CleanPath(owner.Filename + ".txt")), true))
                writer.WriteLine(text);
        }

        #endregion
    }
}
