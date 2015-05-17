using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.BoP.Plugins
{
    /// <summary>
    /// The core BoP plugin
    /// </summary>
    public class BoPCore : CSPlugin
    {
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the BoPCore class
        /// </summary>
        public BoPCore()
        {
            // Set attributes
            Name = "bopcore";
            Title = "BoP Core";
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
