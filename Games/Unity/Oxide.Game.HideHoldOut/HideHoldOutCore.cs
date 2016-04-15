using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Game.HideHoldOut
{
    /// <summary>
    /// The core Hide & Hold Out plugin
    /// </summary>
    public class HideHoldOutCore : CSPlugin
    {
        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the HideHoldOutCore class
        /// </summary>
        public HideHoldOutCore()
        {
            // Set attributes
            Name = "HideHoldOutCore";
            Title = "HideHoldOut Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "hide & hold out");
            RemoteLogger.SetTag("version", NetworkController.NetManager_.get_GAME_VERSION);
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore") InitializeLogging();
        }

        #endregion

        #region Server Hooks

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", NetworkController.NetManager_.ServManager.Server_NAME);
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="approval"></param>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(uLink.NetworkPlayerApproval approval)
        {
            // Get the PlayerInfos
            var player = FindPlayerById(approval.loginData.ReadString());

            //UnityEngine.Debug.LogWarning(approval.loginData.ReadUInt64().ToString());
            UnityEngine.Debug.LogWarning(approval.loginData.ReadString());
            UnityEngine.Debug.LogWarning(player.account_id); // not defined
            UnityEngine.Debug.LogWarning(player.Nickname); // not defined

            // Reject invalid connections
            /*if (player.account_id == "0" || string.IsNullOrEmpty(player.Nickname))
            {
                approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                return false;
            }*/

            // Call out and see if we should reject
            /*var canlogin = Interface.CallHook("CanClientLogin", player);
            if (canlogin is uLink.NetworkConnectionError)
            {
                approval.Deny((uLink.NetworkConnectionError)canlogin);
                return true;
            }*/

            return Interface.CallHook("OnUserApprove", approval, player);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="netPlayer"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(uLink.NetworkPlayer netPlayer)
        {
            // Get the PlayerInfos
            var player = FindPlayer(netPlayer);

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var userId = player.account_id;
                permission.UpdateNickname(userId, player.Nickname);

                // Add player to default group
                if (!permission.UserHasAnyGroup(userId)) permission.AddUserGroup(userId, DefaultGroups[0]);
            }

            Interface.Oxide.CallHook("OnPlayerConnected", player);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="netPlayer"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(uLink.NetworkPlayer netPlayer)
        {
            // Get the PlayerInfos
            var player = FindPlayer(netPlayer);

            Interface.Oxide.CallHook("OnPlayerDisconnected", player);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Returns the PlayerInfos for the specified uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private PlayerInfos FindPlayer(uLink.NetworkPlayer player) => NetworkController.NetManager_.ServManager.GetPlayerInfos(player);

        /// <summary>
        /// Returns the PlayerInfos for the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private PlayerInfos FindPlayerById(ulong id) => NetworkController.NetManager_.ServManager.GetPlayerInfos_accountID(id.ToString());

        /// <summary>
        /// Returns the PlayerInfos for the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private PlayerInfos FindPlayerById(string id) => NetworkController.NetManager_.ServManager.GetPlayerInfos_accountID(id);

        #endregion
    }
}
