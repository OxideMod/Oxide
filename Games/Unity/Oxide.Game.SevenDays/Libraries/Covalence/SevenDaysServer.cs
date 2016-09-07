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
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return GamePrefs.GetString(EnumGamePrefs.ServerName); }
            set { GamePrefs.Set(EnumGamePrefs.ServerName, value); }
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
        public ushort Port => Convert.ToUInt16(GamePrefs.GetInt(EnumGamePrefs.ServerPort));

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => GamePrefs.GetString(EnumGamePrefs.GameVersion);

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => GameManager.Instance.World.Players.Count;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return GamePrefs.GetInt(EnumGamePrefs.ServerMaxPlayerCount); }
            set { GamePrefs.Set(EnumGamePrefs.ServerMaxPlayerCount, value); }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get
            {
                var time = GameManager.Instance.World.worldTime;
                return Convert.ToDateTime($"{GameUtils.WorldTimeToHours(time)}:{GameUtils.WorldTimeToMinutes(time)}");
            }
            set { GameUtils.DayTimeToWorldTime(value.Day, value.Hour, value.Minute); }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            GameManager.Instance.SaveLocalPlayerData();
            GameManager.Instance.SaveWorld();
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => GameManager.Instance.GameMessageServer(null, EnumGameMessages.Chat, message, null, false, null, false);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            SdtdConsole.Instance.ExecuteSync($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", null);
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
            // [FFFFFF]
            return text;
        }
    }
}
