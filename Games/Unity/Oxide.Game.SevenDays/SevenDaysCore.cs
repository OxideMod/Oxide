using System;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// The core 7 Days to Die plugin
    /// </summary>
    public class SevenDaysCore : CSPlugin
    {
        #region Initialization

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the SevenDaysCore class
        /// </summary>
        public SevenDaysCore()
        {
            // Set attributes
            Name = "SevenDaysCore";
            Title = "7 Days to Die";
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

        #endregion

        #region Plugins Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("version", GamePrefs.GetString(EnumGamePrefs.GameVersion));

            // Setup the default permission groups
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
            RemoteLogger.SetTag("hostname", GamePrefs.GetString(EnumGamePrefs.ServerName));

            // Update server console window and status bars
            SevenDaysExtension.ServerConsole();
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(ClientInfo client)
        {
            // Do permission stuff
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(client.playerId, client.playerName);

                // Add player to default group
                if (!permission.UserHasGroup(client.playerId, DefaultGroups[0])) permission.AddUserGroup(client.playerId, DefaultGroups[0]);
            }

            // Let covalence know
            //covalence.PlayerManager.NotifyPlayerConnect(client);
            //Interface.CallHook("OnUserConnected", covalence.PlayerManager.GetPlayer(client.playerId));
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ClientInfo client)
        {
            // Let covalence know
            //covalence.PlayerManager.NotifyPlayerDisconnect(client);
            //Interface.CallHook("OnUserDisconnected", covalence.PlayerManager.GetPlayer(client.playerId), "Unknown");
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        [HookMethod("OnPlayerChat")]
        private void OnPlayerChat(ClientInfo client, string message)
        {
            // Call covalence hook
            //Interface.CallHook("OnUserChat", covalence.PlayerManager.GetPlayer(client.playerId), message);
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(ClientInfo client)
        {
            // Call covalence hook
            //Interface.CallHook("OnUserSpawn", covalence.PlayerManager.GetPlayer(client.playerId));
        }

        #endregion

        #region Deprecated Hooks

        [HookMethod("OnServerCommand")]
        private object OnServerCommand(ClientInfo client, string command)
        {
            return Interface.CallDeprecatedHook("OnRunCommand", "OnServerCommand", new DateTime(2016, 8, 1), client, command);
        }

        #endregion
    }
}
