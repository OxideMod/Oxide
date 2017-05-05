using Oxide.Core;
using Oxide.Core.Logging;
using System.Threading;
using VRage.Utils;
using LogType = Oxide.Core.Logging.LogType;

namespace Oxide.Game.SpaceEngineers.Logging
{
    /// <summary>
    /// A logger that writes to the Unity console
    /// </summary>
    public sealed class SpaceEngineersLogger : Logger
    {
        private readonly Thread mainThread = Thread.CurrentThread;

        /// <summary>
        /// Initializes a new instance of the UnityLogger class
        /// </summary>
        public SpaceEngineersLogger() : base(true)
        {
        }

        /// <summary>
        /// Immediately writes a message to the unity console
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessMessage(LogMessage message)
        {
            if (Thread.CurrentThread != mainThread)
            {
                Interface.Oxide.NextTick(() => ProcessMessage(message));
                return;
            }

            if (MyLog.Default == null)
                return;
            switch (message.Type)
            {
                case LogType.Info:
                    MyLog.Default.WriteLineAndConsole(message.ConsoleMessage);
                    break;
                case LogType.Debug:
                    MyLog.Default.WriteLineAndConsole(message.ConsoleMessage);
                    break;
                case LogType.Warning:
                    MyLog.Default.WriteLineAndConsole(message.ConsoleMessage);
                    break;
                case LogType.Error:
                    MyLog.Default.WriteLineAndConsole(message.ConsoleMessage);
                    break;
                case LogType.Stacktrace:
                    if (Interface.Oxide.Config.Console.ShowStacktraces)
                        MyLog.Default.WriteLineAndConsole(message.ConsoleMessage);
                    break;
            }
        }
    }
}
