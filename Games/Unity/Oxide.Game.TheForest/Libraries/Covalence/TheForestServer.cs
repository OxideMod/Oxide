using System.Net;

using Bolt;
using Steamworks;
using TheForest.UI.Multiplayer;
using TheForest.Utils;
using UnityEngine;

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
        public void Broadcast(string message)
        {
            // Set the sender name to "Server"
            var player = new ChatBox.Player { _name = "Server", _color = Color.cyan };
            ChatBox.Instance.Players[LocalPlayer.Entity.networkId] = player;
            LocalPlayer.Entity.GetState<IPlayerState>().name = "Server";

            // Create and send the chat event
            var entity = ChatEvent.Create(GlobalTargets.Others);
            entity.Message = message;
            entity.Sender = LocalPlayer.Entity.networkId;
            entity.Send();

            Debug.Log($"[Broadcast] {message}");
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
}
