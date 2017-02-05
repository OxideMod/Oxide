using System;
using Oxide.Core.ServerConsole;
using Oxide.Core;

namespace Oxide.Core.Libraries.RemoteConsole
{
    public class RCON : Library
    {
        #region Fields

        public override bool IsGlobal => false;

        internal static readonly OxideMod Oxide = Interface.Oxide;

        public ConfigFile config { get; private set; }

        private ServerManager Server { get; set; }

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
            if (config != null)
            {
                LogInfo("Unloading Config File");
                config = null;
            }
        }

        /// <summary>
        /// Initalize the RCON Server
        /// </summary>
        public void Initalize()
        {
            LogInfo("Loading Configuration File");
            config = ConfigFile.LoadConfig();
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

        #endregion Library Management

        public static void LogInfo(string format, params object[] args) => Interface.Oxide.LogInfo("[RCON]:" + format, args);

        public static void LogWarning(string format, params object[] args) => Interface.Oxide.LogWarning("[RCON]:" + format, args);

        public static void LogError(string format, params object[] args) => Interface.Oxide.LogError("[RCON]:" + format, args);

        public static void LogDebug(string format, params object[] args) => Interface.Oxide.LogDebug("[RCON]:" + format, args);

        public static void LogException(string message, Exception ex) => Interface.Oxide.LogException("[RCON]:" + message, ex);
    }
}