using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        private readonly OxideConfig.OxideRcon config = Interface.Oxide.Config.Rcon;
        private readonly Covalence covalence = Interface.Oxide.GetLibrary<Covalence>();
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
        private void OnMessage(MessageEventArgs e, WebSocketContext context = null)
        {
            var message = RemoteMessage.GetMessage(e.Data);
            message.Message = message.Message.Replace("\"", string.Empty);

            if (message == null || covalence == null || string.IsNullOrEmpty(message.Message))
            {
                Interface.Oxide.LogError($"[Rcon] Failed to process command {(message == null ? "RemoteMessage" : "Covalence")} is null");
                return;
            }

            var msg = message.Message.Split(' ');
            var cmd = msg[0];
            var args = msg.Skip(1).ToArray();
            switch (cmd.ToLower())
            {
                case "broadcast":
                case "chat.say":
                case "global.say":
                case "say":
                    BroadcastMessage(cmd, args, message.Identifier, context);
                    break;

                case "global.playerlist":
                case "playerlist":
                    PlayerListCommand(cmd, args, message.Identifier, context);
                    break;

                case "hostname":
                case "server.hostname":
                    HostnameCommand(cmd, args, message.Identifier, context);
                    break;

                case "global.kick":
                case "kick":
                    KickCommand(cmd, args, message.Identifier, context);
                    break;

                case "save":
                case "server.save":
                    covalence.Server.Save();
                    SendMessage(RemoteMessage.CreateMessage("Server Saved"));
                    break;

                case "ban":
                case "banid":
                case "global.ban":
                case "global.banid":
                    BanCommand(cmd, args, message.Identifier, context);
                    break;

                case "global.unban":
                case "unban":
                    UnbanCommand(cmd, args, message.Identifier, context);
                    break;

                case "server.version":
                case "version":
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage($"{covalence.Game} {covalence.Server.Version} - Protocol {covalence.Server.Protocol} with Oxide v{OxideMod.Version}", message.Identifier).ToJSON());
                    break;

                case "global.teleport":
                case "global.teleportpos":
                case "teleport":
                case "teleportpos":
                    TeleportCommand(cmd, args, message.Identifier, context);
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
                VoiationLevel = 0.0f; // Needed for compatability
                CurrentLevel = 0.0f; // Needed for compatability
                UnspentXp = 0.0f; // Needed for compatability
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

            protected override void OnMessage(MessageEventArgs e) => Parent?.OnMessage(e, Context);

            protected override void OnOpen() => Interface.Oxide.LogInfo($"[Rcon] New connection from {Context.UserEndPoint.Address}");
        }

        #endregion

        #region Command Processing

        // Broacasts a message into the game chat
        private void BroadcastMessage(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), command, args) != null) return;

            var message = string.Join(" ", args);

            var msg = $"{config.ChatPrefix} {message}";
            covalence?.Server.Broadcast(msg);
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"<[^>]*>", string.Empty);
            SendMessage(RemoteMessage.CreateMessage($"[Chat][Rcon]{msg}"));
        }

        // Returns the playerlist to the requesting socket
        private void PlayerListCommand(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), command, args) != null) return;

            var players = new List<RconPlayer>();

            foreach (var player in covalence.Players.Connected) players.Add(new RconPlayer(player));

            context?.WebSocket?.Send(RemoteMessage.CreateMessage(JsonConvert.SerializeObject(players.ToArray(), Formatting.Indented), identifier).ToJSON());
        }

        // returns or sets the hostname via rcon
        private void HostnameCommand(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), command, args) != null) return;

            var hostname = string.Join(" ", args);

            if (!string.IsNullOrEmpty(hostname)) covalence.Server.Name = hostname;

            context?.WebSocket?.Send(RemoteMessage.CreateMessage($"server.hostname: \"{covalence.Server.Name}\"", identifier).ToJSON());
        }

        // Kicks a currently connected user from the server
        private void KickCommand(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), context, args) != null) return;

            var player = covalence.Players.FindPlayer(args[0]);
            if (player != null && player.IsConnected)
            {
                var reason = string.Join(" ", args.Skip(1).ToArray());
                player.Kick(reason);
                SendMessage(RemoteMessage.CreateMessage($"User Kicked {player} - {reason}"));
                return;
            }

            context?.WebSocket?.Send(RemoteMessage.CreateMessage($"User not found {args[0]}", identifier).ToJSON());
        }

        // Bans a player/id from the server
        private void BanCommand(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), command, args) != null) return;

            var Id = 0ul;
            if (ulong.TryParse(args[0], out Id))
            {
                if (covalence.Server.IsBanned(Id.ToString()))
                {
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage($"User already banned: {Id}", identifier).ToJSON());
                    return;
                }

                var reason = string.Join(" ", args.Skip(1).ToArray());
                covalence.Server.Ban(Id.ToString(), reason);
                context?.WebSocket?.Send(RemoteMessage.CreateMessage($"UserID Banned: {Id}", identifier).ToJSON());
                return;
            }

            var player = covalence.Players.FindPlayer(args[0]);

            if (player == null)
            {
                context?.WebSocket?.Send(RemoteMessage.CreateMessage($"Unable to find player: {args[0]}", identifier).ToJSON());
                return;
            }

            player.Ban(string.Join(" ", args.Skip(1).ToArray()));
            SendMessage(RemoteMessage.CreateMessage($"Player {player.Name} banned"));
        }

        // Unban a banned player
        private void UnbanCommand(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), context, args) != null) return;

            var lookup = string.Join(" ", args);
            var Id = 0ul;
            if (ulong.TryParse(lookup, out Id))
            {
                if (covalence.Server.IsBanned(lookup))
                {
                    covalence.Server.Unban(lookup);
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage($"Unbanned ID {lookup}", identifier).ToJSON());
                    return;
                }

                context?.WebSocket?.Send(RemoteMessage.CreateMessage($"ID {lookup} is not banned", identifier).ToJSON());
            }
            else
            {
                var player = covalence.Players.FindPlayer(lookup);
                if (player == null) return;

                if (!player.IsBanned)
                {
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage($"{player.Name} is not banned", identifier).ToJSON());
                    return;
                }

                player.Unban();
                context?.WebSocket?.Send(RemoteMessage.CreateMessage($"{player.Name} was unbanned successfully", identifier).ToJSON());
            }
        }

        // Teleport a player to another player
        private void TeleportCommand(string command, string[] args, int identifier, WebSocketContext context)
        {
            if (Interface.CallHook("OnIServerCommand", context.UserEndPoint.Address.ToString(), command, args) != null) return;

            if ((args.Length != 2) && (args.Length != 4))
            {
                context?.WebSocket?.Send(RemoteMessage.CreateMessage("Invalid format - teleport [player] [targetplayer]").ToJSON());
                return;
            }

            if (args.Length == 2)
            {
                var player1 = covalence.Players.FindPlayer(args[0]);
                var player2 = covalence.Players.FindPlayer(args[1]);

                if (player1 == null || player2 == null)
                {
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage("Unable to find target players").ToJSON());
                    return;
                }

                player1.Teleport(player2.Position().X, player2.Position().Y, player2.Position().Z);
                context?.WebSocket?.Send(RemoteMessage.CreateMessage($"{player1.Name} was teleported to {player2.Name}").ToJSON());
            }
            else
            {
                var player1 = covalence.Players.FindPlayer(args[0]);

                if (player1 == null)
                {
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage("Unable to find target player").ToJSON());
                    return;
                }

                var X = -1f;
                var Y = -1f;
                var Z = -1f;

                if (!float.TryParse(args[1], out X) && !float.TryParse(args[2], out Y) && !float.TryParse(args[3], out Z))
                {
                    context?.WebSocket?.Send(RemoteMessage.CreateMessage($"Unable to parse coordinates X: {args[1]} Y: {args[2]} Z: {args[3]}").ToJSON());
                    return;
                }

                player1.Teleport(X, Y, Z);
                context?.WebSocket?.Send(RemoteMessage.CreateMessage($"{player1.Name} was teleported to X: {args[1]} Y: {args[2]} Z: {args[3]}").ToJSON());
            }
        }

        #endregion
    }
}
