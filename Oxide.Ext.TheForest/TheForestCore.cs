using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.TheForest.Plugins
{
    /// <summary>
    /// The core The Forest plugin
    /// </summary>
    public class TheForestCore : CSPlugin
    {
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the TheForestCore class
        /// </summary>
        public TheForestCore()
        {
            // Set attributes
            Name = "theforestcore";
            Title = "The Forest Core";
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
