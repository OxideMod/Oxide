using System;
using System.Net;
using Bolt;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Steamworks;
using TheForest.UI.Multiplayer;
using TheForest.Utils;
using UnityEngine;

namespace Oxide.Game.TheForest.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class TheForestServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return CoopLobby.Instance.Info.Name; }
            set
            {
                PlayerPrefs.SetString("MpGameName", value);
                CoopLobby.Instance.Info.Name = value;
                SteamGameServer.SetServerName(value);
            }
        }

        private static IPAddress address;

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
                        address = IPAddress.Parse(webClient.DownloadString("http://api.ipify.org"));
                        return address;
                    }
                    return address;
                }
                catch (Exception ex)
                {
                    RemoteLogger.Exception("Couldn't get server IP address", ex);
                    return new IPAddress(0);
                }
            }
        }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => CoopDedicatedServerStarter.EndPoint.Port;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => TheForestExtension.GameVersion;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => CoopLobby.Instance.Info.CurrentMembers;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return CoopLobby.Instance.Info.MemberLimit; }
            set
            {
                PlayerPrefs.SetInt("MpGamePlayerCount", value);
                CoopLobby.Instance.SetMemberLimit(value);
                SteamGameServer.SetMaxPlayerCount(value);
            }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return DateTime.Today.AddMinutes(TheForestAtmosphere.Instance.TimeOfDay); }
            set { TheForestAtmosphere.Instance.TimeOfDay = value.Minute; }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            LevelSerializer.SaveGame("Game"); // TODO: Verify both are needed
            LevelSerializer.Checkpoint();
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
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
            // TODO: Implement when possible
        }

        #endregion
    }
}
