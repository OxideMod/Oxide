using Oxide.Core;
using Oxide.Core.Plugins;

using Oxide.Ext.Unity.Logging;

namespace Oxide.Ext.Unity.Plugins
{
    /// <summary>
    /// The core Unity plugin
    /// </summary>
    public class UnityCore : CSPlugin
    {
        // The logger
        private UnityLogger logger;

        /// <summary>
        /// Initializes a new instance of the UnityCore class
        /// </summary>
        public UnityCore()
        {
            // Set attributes
            Name = "unitycore";
            Title = "Unity Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);
        }

        /// <summary>
        /// Called when the it's safe to initialize logging
        /// </summary>
        [HookMethod("InitLogging")]
        private void InitLogging()
        {
            // Create our logger and add it to the compound logger
            Interface.Oxide.NextTick(() =>
            {
                logger = new UnityLogger();
                Interface.Oxide.RootLogger.AddLogger(logger);
                Interface.Oxide.RootLogger.DisableCache();
            });
        }

        #region Console/Logging

        /// <summary>
        /// Prints an info message to the server console/log
        /// </summary>
        /// <param name="message"></param>
        public void Print(string message) => UnityEngine.Debug.Log(message);

        /// <summary>
        /// Prints a warning message to the server console/log
        /// </summary>
        /// <param name="message"></param>
        public void PrintWarning(string message) => UnityEngine.Debug.LogWarning(message);

        /// <summary>
        /// Prints an error message to the server console/log
        /// </summary>
        /// <param name="message"></param>
        public void PrintError(string message) => UnityEngine.Debug.LogError(message);

        #endregion
    }
}
