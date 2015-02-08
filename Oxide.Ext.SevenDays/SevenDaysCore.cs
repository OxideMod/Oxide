using System;
using System.Text;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.SevenDays.Plugins
{
    /// <summary>
    /// The core 7 Days to Die plugin
    /// </summary>
    public class SevenDaysCore : CSPlugin
    {
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the SevenDaysCore class
        /// </summary>
        public SevenDaysCore()
        {
            // Set attributes
            Name = "sevendayscore";
            Title = "Seven Days Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (!loggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
        }

        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }
    }
}
