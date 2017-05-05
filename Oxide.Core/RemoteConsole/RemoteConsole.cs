using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

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
            if (message == null || covalence == null) return; // TODO: Return/show why

            switch (message.Type.ToLower())
            {
                case "command":
                    var commands = message.Message.Split(' ');
                    try
                    {
                        if (commands.Count() > 1)
                            covalence.Server.Command(commands[0]);
                        else
                            covalence.Server.Command(commands[0], commands.Skip(1).ToArray());
                    }
                    catch
                    {
                        Interface.Oxide.LogError("[Rcon] Failed to run command {0} - Command might not exist", commands[0]);
                    }
                    break;

                case "chat":
                    covalence.Server.Broadcast($"{config.ChatPrefix}: {message.Message}");
                    break;

                case "players":
                    SendMessage(RemoteMessage.CreateMessage(GetPlayerList(), 0, "players"));
                    break;

                default:
                    SendMessage(RemoteMessage.CreateMessage("Unknown command"));
                    break;
            }
        }

        private class RconPlayer
        {
            public string SteamId { get; private set; }
            public string Name { get; private set; }
            public string Address { get; private set; }
            public float Health { get; private set; }
            public float MaxHealth { get; private set; }
            public int Ping { get; private set; }
            public string Lang { get; private set; }

            public RconPlayer(IPlayer player)
            {
                SteamId = player.Id;
                Name = player.Name;
                Address = player.Address;
                Health = player.Health;
                MaxHealth = player.MaxHealth;
                Ping = player.Ping;
                Lang = player.Language.TwoLetterISOLanguageName;
            }
        }

        private string GetPlayerList()
        {
            try
            {
                List<RconPlayer> playerlist = new List<RconPlayer>();
                foreach (var player in covalence.Players.Connected)
                    playerlist.Add(new RconPlayer(player));

                string json = JsonConvert.SerializeObject(playerlist, Formatting.None);
                playerlist = null;
                return json;
            }
            catch
            {
                Interface.Oxide.LogError("[Rcon] Covalence API is not loaded yet");
                return string.Empty;
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
