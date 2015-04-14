using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.ReignOfKings.Plugins
{
    /// <summary>
    /// The core 7 Days to Die plugin
    /// </summary>
    public class ReignOfKingsCore : CSPlugin
    {
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsCore class
        /// </summary>
        public ReignOfKingsCore()
        {
            // Set attributes
            Name = "reignofkingscore";
            Title = "Reign of Kings Core";
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
