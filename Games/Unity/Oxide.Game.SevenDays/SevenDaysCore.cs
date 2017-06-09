using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.SevenDays.Libraries.Covalence;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// The core 7 Days to Die plugin
    /// </summary>
    public class SevenDaysCore : CSPlugin
    {
        #region Initialization

        // Libraries
        //internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly SevenDaysCovalenceProvider Covalence = SevenDaysCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        private bool serverInitialized;

        /// <summary>
        /// Initializes a new instance of the SevenDaysCore class
        /// </summary>
        public SevenDaysCore()
        {
            // Set attributes
            Title = "7 Days to Die";
            Author = "Oxide Team";
            var assemblyVersion = SevenDaysExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
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
            RemoteLogger.SetTag("game version", Server.Version);

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

            Analytics.Collect();

            // Update server console window and status bars
            SevenDaysExtension.ServerConsole();
        }

        /// <summary>
        /// Called when the server is saving
        /// </summary>
        [HookMethod("OnServerSave")]
        private void OnServerSave() => Analytics.Collect();

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("IOnServerShutdown")]
        private void IOnServerShutdown()
        {
            Interface.Call("OnServerShutdown");
            Interface.Oxide.OnShutdown();
        }

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(ClientInfo client, string message)
        {
            if (client == null || string.IsNullOrEmpty(message)) return null;

            var chatSpecific = Interface.Call("OnPlayerChat", client, message);
            var chatCovalence = Interface.Call("OnUserChat", Covalence.PlayerManager.FindPlayerById(client.playerId), message);
            return chatSpecific ?? chatCovalence;
        }

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
                if (!permission.UserHasGroup(client.playerId, DefaultGroups[0])) permission.AddUserGroup(client.playerId, DefaultGroups[0]);
            }

            // Let covalence know
            Covalence.PlayerManager.NotifyPlayerConnect(client);
            Interface.Call("OnUserConnected", Covalence.PlayerManager.FindPlayerById(client.playerId));
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ClientInfo client)
        {
            // Let covalence know
            Interface.Call("OnUserDisconnected", Covalence.PlayerManager.FindPlayerById(client.playerId), "Unknown");
            Covalence.PlayerManager.NotifyPlayerDisconnect(client);
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(ClientInfo client) => Interface.Call("OnUserSpawn", Covalence.PlayerManager.FindPlayerById(client.playerId));

        /// <summary>
        /// Called when the player attempts to use a door
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        [HookMethod("ICanUseDoor")]
        private void ICanUseDoor(string id, TileEntitySecure entity) => Interface.Call("CanUseDoor", ConsoleHelper.ParseParamIdOrName(id), entity);

        #endregion
    }
}
