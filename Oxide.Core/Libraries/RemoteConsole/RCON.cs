using Oxide.Core.Configuration;
using Oxide.Core.ServerConsole;
using System;

namespace Oxide.Core.Libraries.RemoteConsole
{
    /// <summary>
    /// A Library to manage Oxides Remote Console
    /// </summary>
    public class RCON : Library
    {
        #region Fields

        public override bool IsGlobal => false;

        internal static readonly OxideMod Oxide = Interface.Oxide;

        /// <summary>
        /// The Config Instance for Oxide's Rcon Class
        /// </summary>
        public readonly OxideConfig.OxideRcon config = Interface.Oxide.Config.RCON;

        // Instance of Oxides Websocket Server
        private ServerManager Server { get; set; }

        /// <summary>
        /// The Current ServerConsole Instance for Oxide
        /// </summary>
        public static readonly ServerConsole.ServerConsole OxideConsole = Interface.Oxide.ServerConsole;

        #endregion Fields

        #region Library Management

        /// <summary>
        /// Shutdown the RCON Server
        /// </summary>
        public override void Shutdown()
        {
            if (Server != null)
            {
                LogInfo("Shutting down Oxide's Remote Console");
                Server.Shutdown("Oxide is shutting down");
                Server = null;
            }
            if (OxideConsole != null)
            {
                LogInfo("Unsubscribing from console messages");
                OxideConsole.OnConsoleMessage -= HandleConsoleMessage;
            }
        }

        /// <summary>
        /// Initalize the RCON Server
        /// </summary>
        public void Initalize()
        {
            if (config == null)
            {
                LogError("oxide.config.json is not loaded unable to continue loading");
                return;
            }

            if (!config.Enabled)
                return;

            if (config.Password == "ChangeMe")
            {
                LogWarning("The password for Oxide's Remote console is still default. Please change it for RCON to work");
                return;
            }

            Server = new ServerManager(this);
            try
            {
                OxideConsole.OnConsoleMessage += HandleConsoleMessage;
                LogInfo("Subscribed to all Console Output");
            }
            catch (Exception ex)
            {
                LogException("Failed to subscribe to Console Output", ex);
            }
        }

        #endregion Library Management

        #region Message Handling

        // Filters Console Messages and Sends them to the remote clients
        private void HandleConsoleMessage(ConsoleSystemEventArgs e)
        {
            if (e.Color == ConsoleColor.Red)
            { Server?.SendMessage(RConMessage.CreateMessage(e.Message, 0, "Error")); return; }
            if (e.Color == ConsoleColor.Yellow)
            { Server?.SendMessage(RConMessage.CreateMessage(e.Message, 0, "Warning")); return; }
            if (e.Message.Contains("[Chat]") || e.Message.Contains("[Better Chat]"))
            { Server?.SendMessage(RConMessage.CreateMessage(e.Message, 0, "Chat")); return; }
            Server?.SendMessage(RConMessage.CreateMessage(e.Message));
        }

        /// <summary>
        /// Broadcast a Chat Formatted RCON message to all connected clients
        /// </summary>
        /// <param name="Message"></param>
        public void LogChatMessage(string Message) => Server?.SendMessage(RConMessage.CreateMessage(Message, 0, "Chat"));

        #endregion Message Handling

        #region Helpers

        public static void LogInfo(string format, params object[] args) => Interface.Oxide.LogInfo("[RCON]:" + format, args);

        public static void LogWarning(string format, params object[] args) => Interface.Oxide.LogWarning("[RCON]:" + format, args);

        public static void LogError(string format, params object[] args) => Interface.Oxide.LogError("[RCON]:" + format, args);

        public static void LogDebug(string format, params object[] args) => Interface.Oxide.LogDebug("[RCON]:" + format, args);

        public static void LogException(string message, Exception ex) => Interface.Oxide.LogException("[RCON]:" + message, ex);

        #endregion Helpers
    }
}