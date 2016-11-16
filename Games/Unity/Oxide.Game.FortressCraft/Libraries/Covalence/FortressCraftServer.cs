using System;
using System.Net;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.FortressCraft.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class FortressCraftServer : IServer
    {
        #region Information

        private static IPAddress address;

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return ServerConsole.WorldName; }
            set { ServerConsole.WorldName = value; }
        }

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
                        address = IPAddress.Parse(webClient.DownloadString("https://api.ipify.org"));
                        return address;
                    }
                    return address;
                }
                catch
                {
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
        public string Version => HUDManager.Version;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => NetworkServerThread.mnPublicPlayerCount;

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
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            // TODO: Find Save

        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            ServerConsole.DebugLog($"[Broadcast] {message}");
            ServerConsole.SendGlobalMessage(message);

        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            ServerConsole.DoServerString(command);
            ServerConsole.DebugLog($"[Broadcast] {command}");
        }

        #endregion
    }
}
