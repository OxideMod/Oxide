using System;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Oxide.Core.Libraries.RemoteConsole
{
    public class ServerManager
    {
        #region Fields
        // The WebSocket Server Instance
        private WebSocketServer SOCKServer;

        // The Main Library
        private RCON Manager;

        // The Covalence Instance
        public static readonly Covalence.Covalence covalence = Interface.Oxide.GetLibrary<Covalence.Covalence>();

        // WebsocketBehavior
        private OxideBehavior behavior;
        #endregion

        #region Initalizers
        /// <summary>
        /// Initalizes and starts the RCON Server
        /// </summary>
        /// <param name="e"></param>
        public ServerManager(RCON e)
        {
            Manager = e;
            try
            {
                SOCKServer = new WebSocketServer(e.config.Port);
                SOCKServer.WaitTime = TimeSpan.FromSeconds(5.0);
                SOCKServer.AddWebSocketService<OxideBehavior>($"/{e.config.Password}", () => behavior = new OxideBehavior() { Parent = this });
                SOCKServer.Start();
            }
            catch (Exception ex)
            {
                RemoteLogger.Exception("Failed to start RCON Server", ex);
                RCON.LogException("Failed to Start RCON Server", ex);
            }
            if (SOCKServer != null && SOCKServer.IsListening)
                RCON.LogInfo("Websocket server started successfully on port {0}", SOCKServer.Port.ToString());
        }

        /// <summary>
        /// Shutsdown the RCON Server
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="e"></param>
        public void Shutdown(string reason = "Server Shutting Down", CloseStatusCode e = CloseStatusCode.Normal)
        {
            if (SOCKServer == null)
                return;
            SOCKServer.Stop(e, reason);
            SOCKServer = null;
            Interface.Oxide.LogInfo("[RCON] Service has stopped REASON: {0} CODE: {1}", reason, e.ToString());
        }
        #endregion

        #region Message Handling
        /// <summary>
        /// Handles messages that are sent from the clients
        /// </summary>
        /// <param name="e"></param>
        public void OnMessage(MessageEventArgs e)
        {
            RConMessage msg = RConMessage.GetMessage(e.Data) ?? null;
            if (msg == null)
                return;
            if (msg.Type == "Command")
            {
                if (covalence == null)
                    return;
                string[] commandarray = msg.Message.Split(' ');
                covalence.Server.Command(commandarray[0], commandarray.Skip(1).ToArray());
                return;
            }
            if (msg.Type == "Chat")
            {
                if (covalence == null)
                    return;
                covalence.Server.Broadcast($"{Manager.config.ConsoleName}: {msg.Message}");
            }
        }

        /// <summary>
        /// Broadcast a message to all the connected clients
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(RConMessage message)
        {
            if (message == null)
                return;
            if ((SOCKServer != null && SOCKServer.IsListening && behavior != null))
                behavior.SendMessage(message);
        }
        #endregion

        public class OxideBehavior : WebSocketBehavior
        {
            public ServerManager Parent { get; set; }

            public OxideBehavior()
            {
                base.IgnoreExtensions = true;
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                Parent.OnMessage(e);
            }

            protected override void OnOpen()
            {
                Interface.Oxide.LogInfo("[RCON] New Connection has been started from {0}", base.Context.UserEndPoint.Address.ToString());
            }

            protected override void OnClose(CloseEventArgs e)
            {
                Interface.Oxide.LogInfo("[RCON] Connection has been closed REASON: {0} CODE: {1}", e.Reason, e.Code.ToString());
            }

            protected override void OnError(ErrorEventArgs e)
            {
                Interface.Oxide.LogException(e.Message, e.Exception);
            }

            public void SendMessage(RConMessage message)
            {
                Sessions.Broadcast(message.ToJSON());
            }
        }
    }
}