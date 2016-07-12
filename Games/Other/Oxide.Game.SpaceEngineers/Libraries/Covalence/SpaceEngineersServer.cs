using System.Net;

using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using SteamSDK;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.SpaceEngineers.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class SpaceEngineersServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => MySandboxGame.ConfigDedicated.ServerName;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address
        {
            get
            {
                var ip = SteamServerAPI.Instance.GameServer.GetPublicIP();
                return ip == 0 ? null : new IPAddress(ip >> 24 | ((ip & 0xff0000) >> 8) | ((ip & 0xff00) << 8) | ((ip & 0xff) << 24));
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)MySandboxGame.ConfigDedicated.ServerPort;

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => MyPerGameSettings.BasicGameInfo.GameVersion.ToString();

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
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
            // TODO
        }

        #endregion
    }
}
