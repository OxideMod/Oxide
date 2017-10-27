using System.Threading;
using UnityEngine;

using Logger = Oxide.Core.Logging.Logger;
using LogType = Oxide.Core.Logging.LogType;

namespace Oxide.Core.Unity.Logging
{
    /// <summary>
    /// A logger that writes to the Unity console
    /// </summary>
    public sealed class UnityLogger : Logger
    {
        private readonly Thread mainThread = Thread.CurrentThread;

        /// <summary>
        /// Initializes a new instance of the UnityLogger class
        /// </summary>
        public UnityLogger() : base(true)
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

            switch (message.Type)
            {
                case LogType.Info:
                case LogType.Debug:
                    Debug.Log(message.ConsoleMessage);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(message.ConsoleMessage);
                    break;

                case LogType.Error:
                    Debug.LogError(message.ConsoleMessage);
                    break;

                case LogType.Stacktrace:
                    if (Interface.Oxide.Config.Console.ShowStacktraces)
                        Debug.Log(message.ConsoleMessage);
                    break;
            }
        }
    }
}
