using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Game.Blackwake
{
    /// <summary>
    /// The core Blackwake plugin
    /// </summary>
    public class BlackwakeCore : CSPlugin
    {
        #region Initialization

        // Libraries
        //internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        //internal static readonly BlackwakeCovalenceProvider Covalence = BlackwakeCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        //internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        internal bool serverInitialized;

        /// <summary>
        /// Initializes a new instance of the BlackwakeCore class
        /// </summary>
        public BlackwakeCore()
        {
            Title = "Blackwake";
            Author = "Oxide Team";
            var assemblyVersion = BlackwakeExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        #endregion

        #region Core Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            RemoteLogger.SetTag("game", Title.ToLower());
            //RemoteLogger.SetTag("hostname", FCNGAAPKKEO.MHBDLHCODIH); // TODO: Use Covalence
            //RemoteLogger.SetTag("version", SteamAuth.NPCPMKJLAJN());

            // Setup default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                foreach (var defaultGroup in Interface.Oxide.Config.Options.DefaultGroups)
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);

                permission.RegisterValidate(s =>
                {
                    ulong temp;
                    if (!ulong.TryParse(s, out temp)) return false;
                    var digits = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(temp) + 1);
                    return digits >= 17;
                });

                permission.CleanUp();
            }
        }

        /// <summary>
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            // Call OnServerInitialized for hotloaded plugins
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;

            Analytics.Collect();
            BlackwakeExtension.ServerConsole();

            serverInitialized = true;
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion
    }
}
