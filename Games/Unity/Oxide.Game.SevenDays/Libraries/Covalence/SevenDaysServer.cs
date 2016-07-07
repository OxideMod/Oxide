using System;
using System.Net;

using Steamworks;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.SevenDays.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class SevenDaysServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => GamePrefs.GetString(EnumGamePrefs.ServerName);

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
        public ushort Port => Convert.ToUInt16(GamePrefs.GetInt(EnumGamePrefs.ServerPort));

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => GamePrefs.GetString(EnumGamePrefs.GameVersion);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            GameManager.Instance.GameMessageServer(null, EnumGameMessages.Chat, message, null, false, null, false);
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => SdtdConsole.Instance.ExecuteSync(string.Concat(command, " ", args), null);

        #endregion
    }
}
