using System;
using System.Text;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

using Oxide.Unity.Logging;

namespace Oxide.Unity.Plugins
{
    /// <summary>
    /// The core Unity plugin
    /// </summary>
    public class UnityCore : CSPlugin
    {
        // The logger
        private UnityLogger logger;

        /// <summary>
        /// Initialises a new instance of the UnityCore class
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
        /// Called when the it's safe to initialise logging
        /// </summary>
        [HookMethod("OnInitLogging")]
        private void OnInitLogging()
        {
            // Create our logger and add it to the compound logger
            logger = new UnityLogger();
            Interface.GetMod().RootLogger.AddLogger(logger);
        }
    }
}
