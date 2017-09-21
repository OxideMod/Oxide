using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace Oxide.Core.RemoteConsole
{
    public class RemoteConsole
    {
        #region Initialization

        private readonly Covalence covalence = Interface.Oxide.GetLibrary<Covalence>();
        private readonly OxideConfig.OxideRcon config = Interface.Oxide.Config.Rcon;
        private RconListener listener;
        private WebSocketServer server;

        /// <summary>
        /// Initalizes the RCON server
        /// </summary>
        public void Initalize()
        {
            if (!config.Enabled || listener != null || server != null) return;

            if (string.IsNullOrEmpty(config.Password))
            {
                Interface.Oxide.LogWarning("[Rcon] Remote console password is not set, disabling");
                return;
            }

            try
            {
                server = new WebSocketServer(config.Port);
                server.WaitTime = TimeSpan.FromSeconds(5.0);
                server.ReuseAddress = true;
                server.AddWebSocketService($"/{config.Password}", () => listener = new RconListener(this));
                server.Start();

                Interface.Oxide.LogInfo($"[Rcon] Server started successfully on port {server.Port}");
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"[Rcon] Failed to start server on port {server.Port}", ex);
                RemoteLogger.Exception($"Failed to start RCON server on port {server.Port}", ex);
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
        /// Broadcast a message to all connected clients
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message, int identifier)
        {
            if (string.IsNullOrEmpty(message) || server == null || !server.IsListening || listener == null) return;

            listener.SendMessage(RemoteMessage.CreateMessage(message, identifier));
        }

        /// <summary>
        /// Broadcast a message to connected client
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        public void SendMessage(WebSocketContext connection, string message, int identifier)
        {
            if (string.IsNullOrEmpty(message) || server == null || !server.IsListening || listener == null) return;

            connection?.WebSocket?.Send(RemoteMessage.CreateMessage(message, identifier).ToJSON());
        }

        /// <summary>
        /// Handles messages sent from the clients
        /// </summary>
        /// <param name="e"></param>
        private void OnMessage(MessageEventArgs e, WebSocketContext connection)
        {
            var message = RemoteMessage.GetMessage(e.Data);
            message.Message = message.Message.Replace("\"", string.Empty);

            if (message == null || covalence == null || string.IsNullOrEmpty(message.Message))
            {
                Interface.Oxide.LogError($"[Rcon] Failed to process command {(message == null ? "RemoteMessage" : "Covalence")} is null");
                return;
            }

            var msg = message.Message.Split(' ');
            var cmd = msg[0].ToLower();
            var args = msg.Skip(1).ToArray();

            if (Interface.CallHook("OnRconCommand", connection.UserEndPoint.Address, cmd, args) != null) return;

            switch (cmd)
            {
                case "broadcast":
                case "chat.say":
                case "global.say":
                case "say":
                    BroadcastMessage(cmd, args, message.Identifier, connection);
                    break;

                case "global.playerlist":
                case "playerlist":
                    PlayerListCommand(cmd, args, message.Identifier, connection);
                    break;

                case "hostname":
                case "server.hostname":
                    HostnameCommand(cmd, args, message.Identifier, connection);
                    break;

                case "global.kick":
                case "kick":
                    KickCommand(cmd, args, message.Identifier, connection);
                    break;

                case "save":
                case "server.save":
                    covalence.Server.Save();
                    SendMessage(connection, "Server saved", message.Identifier);
                    break;

                case "ban":
                case "banid":
                case "global.ban":
                case "global.banid":
                    BanCommand(cmd, args, message.Identifier, connection);
                    break;

                case "global.unban":
                case "unban":
                    UnbanCommand(cmd, args, message.Identifier, connection);
                    break;

                case "server.version":
                case "version":
                    SendMessage(connection, $"{covalence.Game} {covalence.Server.Version} - Protocol {covalence.Server.Protocol} with Oxide v{OxideMod.Version}", message.Identifier);
                    break;

                case "global.teleport":
                case "global.teleportpos":
                case "teleport":
                case "teleportpos":
                    TeleportCommand(cmd, args, message.Identifier, connection);
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
                ConnectedSeconds = 0; // TODO: Implement when support is added
                VoiationLevel = 0.0f; // Needed for Rust compatability
                CurrentLevel = 0.0f; // Needed for Rust compatability
                UnspentXp = 0.0f; // Needed for Rust compatability
            }
        }

        #endregion

        #region Listener

        public class RconListener : WebSocketBehavior
        {
            private readonly RemoteConsole Parent;
            private IPAddress Address;

            public RconListener(RemoteConsole parent)
            {
                IgnoreExtensions = true;
                Parent = parent;
            }

            public void SendMessage(RemoteMessage message) => Sessions.Broadcast(message.ToJSON());

            protected override void OnClose(CloseEventArgs e)
            {
                var reason = string.IsNullOrEmpty(e.Reason) ? "Unknown" : e.Reason;
                Interface.Oxide.LogInfo($"[Rcon] Connection from {Address} closed: {reason} ({e.Code})");
            }

            protected override void OnError(ErrorEventArgs e) => Interface.Oxide.LogException(e.Message, e.Exception);

            protected override void OnMessage(MessageEventArgs e) => Parent?.OnMessage(e, Context);

            protected override void OnOpen() 
            {
                Address = Context.UserEndPoint.Address;
                Interface.Oxide.LogInfo($"[Rcon] New connection from {Address}");
            }
        }

        #endregion

        #region Command Processing

        // Broacasts a message into the game chat
        private void BroadcastMessage(string command, string[] args, int identifier, WebSocketContext connection)
        {
            var message = string.Join(" ", args);
            var msg = $"{config.ChatPrefix} {message}";
            covalence?.Server.Broadcast(msg);
            msg = Regex.Replace(msg, @"<[^>]*>", string.Empty); // TODO: Make pattern static

            SendMessage($"[Chat] {msg}", identifier);
        }

        // Returns the playerlist to the requesting socket
        private void PlayerListCommand(string command, string[] args, int identifier, WebSocketContext connection)
        {
            var players = new List<RconPlayer>();
            foreach (var player in covalence.Players.Connected) players.Add(new RconPlayer(player));

            SendMessage(connection, JsonConvert.SerializeObject(players.ToArray(), Formatting.Indented), identifier);
        }

        // Returns or sets the hostname via rcon
        private void HostnameCommand(string command, string[] args, int identifier, WebSocketContext connection)
        {
            var hostname = string.Join(" ", args);
            if (!string.IsNullOrEmpty(hostname)) covalence.Server.Name = hostname;

            SendMessage(connection, $"server.hostname: \"{covalence.Server.Name}\"", identifier);
        }

        // Kicks a currently connected user from the server
        private void KickCommand(string command, string[] args, int identifier, WebSocketContext connection)
        {
            // TODO: Handle multiple players
            // TODO: Only find connected players
            var player = covalence.Players.FindPlayer(args[0]);
            if (player != null && player.IsConnected)
            {
                var reason = string.Join(" ", args.Skip(1).ToArray());
                player.Kick(reason);
                SendMessage(connection, $"Player kicked {player} - {reason}", identifier);
                return;
            }

            SendMessage(connection, $"Player not found {args[0]}", identifier);
        }

        // Bans the player/id from the server
        private void BanCommand(string command, string[] args, int identifier, WebSocketContext connection)
        {
            var id = 0ul;
            if (ulong.TryParse(args[0], out id))
            {
                if (covalence.Server.IsBanned(id.ToString()))
                {
                    SendMessage(connection, $"Player already banned: {id}", identifier);
                    return;
                }

                var reason = string.Join(" ", args.Skip(1).ToArray());
                covalence.Server.Ban(id.ToString(), reason);
                SendMessage(connection, $"Player banned: {id}", identifier);
                return;
            }

            // TODO: Handle multiple players
            var player = covalence.Players.FindPlayer(args[0]);
            if (player == null)
            {
                SendMessage(connection, $"Unable to find player: {args[0]}", identifier);
                return;
            }

            player.Ban(string.Join(" ", args.Skip(1).ToArray()));
            SendMessage(connection, $"Player {player.Name} banned", identifier);
        }

        // Unban a banned player
        private void UnbanCommand(string command, string[] args, int identifier, WebSocketContext connection)
        {
            var lookup = string.Join(" ", args);
            var id = 0ul;
            if (ulong.TryParse(lookup, out id))
            {
                if (covalence.Server.IsBanned(lookup))
                {
                    covalence.Server.Unban(lookup);
                    SendMessage(connection, $"Unbanned ID {lookup}", identifier);
                    return;
                }

                SendMessage(connection, $"ID {lookup} is not banned", identifier);
            }
            else
            {
                // TODO: Handle multiple players
                // TODO: Only find connected players
                var player = covalence.Players.FindPlayer(lookup);
                if (player == null) return;

                if (!player.IsBanned)
                {
                    SendMessage(connection, $"{player.Name} is not banned", identifier);
                    return;
                }

                player.Unban();
                SendMessage(connection, $"{player.Name} was unbanned successfully", identifier);
            }
        }

        // Teleport the player to another player
        private void TeleportCommand(string command, string[] args, int identifier, WebSocketContext connection)
        {
            if (args.Length != 2 && args.Length != 4)
            {
                SendMessage(connection, "Usage: teleport <player> <target player>", identifier);
                return;
            }

            if (args.Length == 2)
            {
                // TODO: Handle multiple players
                // TODO: Only find connected players
                var player1 = covalence.Players.FindPlayer(args[0]);
                var player2 = covalence.Players.FindPlayer(args[1]);
                if (player1 == null || player2 == null)
                {
                    SendMessage(connection, "Unable to find target players", identifier);
                    return;
                }

                player1.Teleport(player2.Position().X, player2.Position().Y, player2.Position().Z);
                SendMessage(connection, $"{player1.Name} was teleported to {player2.Name}", identifier);
            }
            else
            {
                // TODO: Handle multiple players
                // TODO: Only find connected players
                var player1 = covalence.Players.FindPlayer(args[0]);
                if (player1 == null)
                {
                    SendMessage(connection, "Unable to find target player", identifier);
                    return;
                }

                var X = -1f;
                var Y = -1f;
                var Z = -1f;
                if (!float.TryParse(args[1], out X) && !float.TryParse(args[2], out Y) && !float.TryParse(args[3], out Z))
                {
                    SendMessage(connection, $"Unable to parse coordinates X: {args[1]} Y: {args[2]} Z: {args[3]}", identifier);
                    return;
                }

                player1.Teleport(X, Y, Z);
                SendMessage($"{player1.Name} was teleported to X: {X} Y: {Y} Z: {X}", identifier);
            }
        }

        #endregion
    }
}
