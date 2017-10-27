using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Game hooks and wrappers for the core 7 Days to Die plugin
    /// </summary>
    public partial class SevenDaysCore : CSPlugin
    {
        #region Player Hooks

        /// <summary>
        /// Called when the player attempts to use a door
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        [HookMethod("ICanUseDoor")]
        private void ICanUseDoor(string id, TileEntitySecure entity) => Interface.Call("CanUseDoor", ConsoleHelper.ParseParamIdOrName(id), entity);

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
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(client.playerId, client.playerName);
                var defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(client.playerId, defaultGroups.Players)) permission.AddUserGroup(client.playerId, defaultGroups.Players);
                // TODO: Admin group automation
            }

            // Let covalence know
            Covalence.PlayerManager.PlayerJoin(client.steamId.m_SteamID, client.playerName); // TODO: Move to OnUserApprove hook once available
            Covalence.PlayerManager.PlayerConnected(client);
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
            Covalence.PlayerManager.PlayerDisconnected(client);
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(ClientInfo client) => Interface.Call("OnUserSpawn", Covalence.PlayerManager.FindPlayerById(client.playerId));

        #endregion Player Hooks
    }
}
