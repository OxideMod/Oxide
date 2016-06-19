using System.Net;

using TheForest.UI.Multiplayer;
using TheForest.Utils;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.TheForest.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class TheForestServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => CoopLobby.Instance.Info.Name;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => IPAddress.Parse(CoopDedicatedServerStarter.EndPoint.ToString());

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => CoopDedicatedServerStarter.EndPoint.Port;

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => TheForestExtension.GameVersion;

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all player clients
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => ChatBox.Instance.AddLine(LocalPlayer.Entity.networkId, message);

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
