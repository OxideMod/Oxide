using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using System.Runtime.InteropServices;

namespace Oxide.Core.RemoteConsole
{
    public class RemoteConsole
    {
        #region Initialization

        private readonly OxideConfig.OxideRcon config = Interface.Oxide.Config.Rcon;
        private readonly Covalence covalence = Interface.Oxide.GetLibrary<Covalence>();
        private RconListener listener;
        private WebSocketServer server;

        /// <summary>
        /// Initalizes the RCON server
        /// </summary>
        public void Initalize()
        {
            if (!config.Enabled) return;
            if (listener != null || server != null)
                return;

            if (string.IsNullOrEmpty(config.Password))
            {
                Interface.Oxide.LogWarning("[Rcon] Remote console password is not set, disabling");
                return;
            }

            try
            {
                server = new WebSocketServer(config.Port);
                server.WaitTime = TimeSpan.FromSeconds(5.0);
                server.AddWebSocketService($"/{config.Password}", () => listener = new RconListener(this));
                server.Start();

                Interface.Oxide.LogInfo($"[Rcon] Server started successfully on port {server.Port}");
            }
            catch (Exception ex)
            {
                RemoteLogger.Exception($"Failed to start RCON server on port {server.Port}", ex);
                Interface.Oxide.LogException($"[Rcon] Failed to start server on port {server.Port}", ex);
            }
        }

        /// <summary>
        /// Shuts down the RCON server
        /// </summary>
        public void Shutdown(string reason = "Server shutting down", CloseStatusCode code = CloseStatusCode.Normal)
        {
            if (server == null) return;

            server.Stop(code, reason);
            server = null;
            listener = null;
            Interface.Oxide.LogInfo($"[Rcon] Service has stopped: {reason} ({code})");
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Broadcast a message to all connected clients
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(RemoteMessage message)
        {
            if (message == null || server == null || !server.IsListening || listener == null) return;

            listener.SendMessage(message);
        }

        /// <summary>
        /// Handles messages sent from the clients
        /// </summary>
        /// <param name="e"></param>
        private void OnMessage(MessageEventArgs e)
        {
            var message = RemoteMessage.GetMessage(e.Data);
            if (message == null)
            {
                Interface.Oxide.LogError("[Rcon]: Failed OnMessage Received Message is Null");
                return;
            }
            if (covalence == null)
            {
                Interface.Oxide.LogError("[Rcon]: Failed OnMessage Covalence is Null");
                return;
            }
            if (string.IsNullOrEmpty(message.Message))
                return;

            var messagearray = message.Message.Split(' ');
            var cmd = messagearray[0];
            var args = messagearray.Skip(1).ToArray();
            switch (cmd.ToLower())
            {
                case "broadcast":
                case "say":
                case "global.say":
                case "chat.say":
                    if (Interface.CallHook("OnRconBroadcast", args) != null)
                        return;
                    covalence.Server.Broadcast(string.Format(config.ChatPrefix + " {0}", string.Join(" ", args)));
                    break;

                case "playerlist":
                case "global.playerlist":
                    List<RconPlayer> players = new List<RconPlayer>();
                    foreach (var pl in covalence.Players.Connected)
                        players.Add(new RconPlayer(pl));
                    SendMessage(RemoteMessage.CreateMessage(JsonConvert.SerializeObject(players.ToArray(), Formatting.Indented)));
                    break;

                case "server.hostname":
                case "hostname":
                    if (args.Length != 0)
                        covalence.Server.Name = string.Join(" ", args);
                    SendMessage(RemoteMessage.CreateMessage(covalence.Server.Name));
                    break;

                case "global.kick":
                case "kick":
                    if (args.Length == 0)
                        return;
                    if (Interface.CallHook("OnRconKick", args) != null)
                        return;
                    covalence.Players.FindPlayer(args[0])?.Kick(string.Join(" ", args.Skip(1).ToArray()));
                    break;

                case "server.save":
                case "save":
                    covalence.Server.Save();
                    break;

                case "ban":
                case "banid":
                case "global.ban":
                case "global.banid":
                    if (args.Length == 0)
                        return;
                    if (Interface.CallHook("OnRconBan", args) != null)
                        return;
                    var banplayer = covalence.Players.FindPlayer(args[0]);
                    if (banplayer != null)
                        banplayer.Ban(string.Join(" ", args.Skip(1).ToArray()));
                    else
                        covalence.Server.Ban(args[0], string.Join(" ", args.Skip(1).ToArray()));
                    break;

                case "unban":
                case "global.unban":
                    if (args.Length == 0)
                        return;
                    if (Interface.CallHook("OnRconUnban", args) != null)
                        return;
                    var unbanplayer = covalence.Players.FindPlayer(string.Join(" ", args));
                    if (unbanplayer != null)
                        unbanplayer.Unban();
                    else
                        covalence.Server.Unban(args[0]);
                    break;

                case "version":
                case "server.version":
                    var serverversion = covalence.Server.Version;
                    var Game = covalence.Game;
                    var oxideversion = OxideMod.Version;
                    SendMessage(RemoteMessage.CreateMessage($"{Game} - {serverversion} Protocol {covalence.Server.Protocol} with OxideMod v{oxideversion.Major}.{oxideversion.Minor}.{oxideversion.Patch}"));
                    break;

                default:
                    covalence.Server.Command(cmd, args);
                    break;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RconPlayer
        {
            public string SteamID { get; private set; }
            public string OwnerSteamID { get; private set; }
            public string DisplayName { get; private set; }
            public int Ping { get; private set; }
            public string Address { get; private set; }
            public int ConnectedSeconds { get; private set; }
            public float VoiationLevel { get; private set; }
            public float CurrentLevel { get; private set; }
            public float UnspentXp { get; private set; }
            public float Health { get; private set; }

            public RconPlayer(IPlayer player)
            {
                SteamID = player.Id;
                OwnerSteamID = "0";
                DisplayName = player.Name;
                Address = player.Address;
                Health = player.Health;
                Ping = player.Ping;
                ConnectedSeconds = 0; // Todo when support is added
                VoiationLevel = 0; // Needed For Compat
                CurrentLevel = 0; // Needed For Compat
                UnspentXp = 0; // Needed For Compat
            }
        }

        #endregion

        #region Listener

        public class RconListener : WebSocketBehavior
        {
            private readonly RemoteConsole Parent;

            public RconListener(RemoteConsole parent)
            {
                IgnoreExtensions = true;
                Parent = parent;
            }

            public void SendMessage(RemoteMessage message) => Sessions.Broadcast(message.ToJSON());

            protected override void OnClose(CloseEventArgs e) => Interface.Oxide.LogInfo($"[Rcon] Connection from {Context.UserEndPoint.Address} closed: {e.Reason} ({e.Code}");

            protected override void OnError(ErrorEventArgs e) => Interface.Oxide.LogException(e.Message, e.Exception);

            protected override void OnMessage(MessageEventArgs e) => Parent?.OnMessage(e);

            protected override void OnOpen() => Interface.Oxide.LogInfo($"[Rcon] New connection from {Context.UserEndPoint.Address}");
        }

        #endregion
    }
}