﻿using System;
using System.Globalization;
using System.Linq;
using System.Net;
using Bolt;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Steamworks;
using TheForest.Utils;
using UdpKit;
using UnityEngine;

namespace Oxide.Game.TheForest.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class TheForestServer : IServer
    {
        internal NetworkId NetworkId = new NetworkId();

        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return CoopServerInfo.Instance.state.ServerName; }
            set
            {
                //SteamDSConfig.ServerName = value;
                CoopServerInfo.Instance.state.ServerName = value;
                //SteamGameServer.SetServerName(value); // TODO: Check if needed
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
                    if (address != null) return address;

                    var webClient = new WebClient();
                    IPAddress.TryParse(webClient.DownloadString("http://api.ipify.org"), out address);
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
        public ushort Port => SteamDSConfig.ServerGamePort;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => SteamDSConfig.ServerVersion;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Version;

        /// <summary>
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.EnglishName == Localization.language);

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => CoopServerInfo.Instance.state.PlayerCount;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return CoopServerInfo.Instance.state.MaxPlayers; }
            set
            {
                //SteamDSConfig.ServerPlayers = value;
                CoopServerInfo.Instance.state.MaxPlayers = value;
                //SteamGameServer.SetMaxPlayerCount(value); // TODO: Check if needed
            }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return DateTime.Today.AddMinutes(CoopWeatherProxy.Instance.state.TimeOfDay); } // TODO: Update
            set { CoopWeatherProxy.Instance.state.TimeOfDay = value.Minute; }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            if (IsBanned(id)) return;

            Scene.HudGui.MpPlayerList.Ban(Convert.ToUInt64(id));
            CoopKick.SaveList();
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id)
        {
            var kickedPlayer = CoopKick.Instance.KickedPlayers.First(p => p.SteamId == Convert.ToUInt64(id));
            return kickedPlayer != null ? TimeSpan.FromTicks(kickedPlayer.BanEndTime) : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => CoopKick.IsBanned(new UdpSteamID(Convert.ToUInt64(id)));

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save() => SteamDSConfig.SaveGame();

        /// <summary>
        /// Unbans the user
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            if (!IsBanned(id)) return;

            CoopKick.UnBanPlayer(ulong.Parse(id));
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message)
        {
            CoopServerInfo.Instance.entity.GetState<IPlayerState>().name = "Server";

            var chatEvent = ChatEvent.Create(GlobalTargets.AllClients);
            chatEvent.Message = message;
            chatEvent.Sender = CoopServerInfo.Instance.entity.networkId;
            chatEvent.Send();
            //CoopServerInfo.Broadcast

            Debug.Log($"[Chat] {message}");
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            var adminCommand = AdminCommand.Create(GlobalTargets.OnlyServer);
            adminCommand.Command = command;
            adminCommand.Data = string.Concat(args.Select(o => o.ToString()));
            adminCommand.Send();
            //CoopServerInfo.Instance.ExecuteCommand
        }

        #endregion
    }
}
