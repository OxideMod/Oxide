using System.Threading;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Unity.Logging
{
    using Oxide.Core.Logging;

    /// <summary>
    /// A logger that writes to the Unity console
    /// </summary>
    public sealed class UnityLogger : Logger
    {
        private Thread mainThread = Thread.CurrentThread;

        /// <summary>
        /// Initializes a new instance of the UnityLogger class
        /// </summary>
        public UnityLogger()
            : base(true)
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
                    Debug.Log(string.Format("[Oxide] {0}", message.Message));
                    break;
                case LogType.Warning:
                    Debug.LogWarning(string.Format("[Oxide] {0}", message.Message));
                    break;
                case LogType.Error:
                    Debug.LogError(string.Format("[Oxide] {0}", message.Message));
                    break;
            }
        }
    }
}
