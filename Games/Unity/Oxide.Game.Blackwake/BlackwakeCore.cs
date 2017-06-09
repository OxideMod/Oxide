using System;
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

        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        private bool serverInitialized;

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

        #region Plugin Hooks

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
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
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

        #endregion

        #region Server Hooks

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized() => BlackwakeExtension.ServerConsole();

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion
    }
}
