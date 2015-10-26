using System;

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
        /// Loads the default config for this plugin
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            // No config yet, we might use it later
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {

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
    }
}
