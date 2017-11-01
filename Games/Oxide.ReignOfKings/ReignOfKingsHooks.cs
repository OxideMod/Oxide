using CodeHatch.Common;
using CodeHatch.Engine.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// Game hooks and wrappers for the core Reign of Kings plugin
    /// </summary>
    public partial class ReignOfKingsCore : CSPlugin
    {
        #region Server Hooks

        /// <summary>
        /// Called by the server when starting, wrapped to prevent errors with dynamic assemblies
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        [HookMethod("IGetTypeFromName")]
        private Type IGetTypeFromName(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly is System.Reflection.Emit.AssemblyBuilder) continue;
                try
                {
                    foreach (var type in assembly.GetExportedTypes())
                        if (type.Name == fullTypeName) return type;
                }
                catch
                {
                    // Ignored
                }
            }
            return null;
        }

        /// <summary>
        /// Called when the hash is recalculated
        /// </summary>
        /// <param name="fileHasher"></param>
        [HookMethod("IOnRecalculateHash")]
        private void IOnRecalculateHash(FileHasher fileHasher)
        {
            if (fileHasher.FileLocationFromDataPath.Equals("/Managed/Assembly-CSharp.dll"))
                fileHasher.FileLocationFromDataPath = "/Managed/Assembly-CSharp_Original.dll";
        }

        /// <summary>
        /// Called when the files are counted
        /// </summary>
        /// <param name="fileCounter"></param>
        [HookMethod("IOnCountFolder")]
        private void IOnCountFolder(FileCounter fileCounter)
        {
            if (fileCounter.FolderLocationFromDataPath.Equals("/Managed/") && fileCounter.Folders.Length != 39)
            {
                var folders = (string[])FoldersField.GetValue(fileCounter);
                Array.Resize(ref folders, 39);
                FoldersField.SetValue(fileCounter, folders);
            }
            else if (fileCounter.FolderLocationFromDataPath.Equals("/../") && fileCounter.Folders.Length != 2)
            {
                var folders = (string[])FoldersField.GetValue(fileCounter);
                Array.Resize(ref folders, 2);
                FoldersField.SetValue(fileCounter, folders);
            }
        }

        #endregion Server Hooks

        #region Player Hooks

        /// <summary>
        /// Called when the player is attempting to connect
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Player player)
        {
            var id = player.Id.ToString();
            var ip = player.Connection.IpAddress;

            Covalence.PlayerManager.PlayerJoin(player.Id, player.Name); // TODO: Handle this automatically

            // Call out and see if we should reject
            var loginSpecific = Interface.Call("CanClientLogin", player);
            var loginCovalence = Interface.Call("CanUserLogin", player.Name, id, ip);
            var canLogin = loginSpecific ?? loginCovalence; // TODO: Fix 'ReignOfKingsCore' hook conflict when both return

            // Check if player can login
            if (canLogin is string || (canLogin is bool && !(bool)canLogin))
            {
                // Reject the player with the message
                player.ShowPopup("Disconnected", canLogin is string ? canLogin.ToString() : "Connection was rejected"); // TODO: Localization
                player.Connection.Close();
                return ConnectionError.NoError;
            }

            // Call the approval hooks
            var approvedSpecific = Interface.Call("OnUserApprove", player);
            var approvedCovalence = Interface.Call("OnUserApproved", player.Name, id, ip);
            return approvedSpecific ?? approvedCovalence; // TODO: Fix 'ReignOfKingsCore' hook conflict when both return
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerChat")]
        private object OnPlayerChat(PlayerMessageEvent evt)
        {
            // Call covalence hook
            return Interface.Call("OnUserChat", evt.Player.IPlayer, evt.Message);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(Player player)
        {
            // Ignore the server player
            if (player.Id == 9999999999) return;

            // Update player's permissions group and name
            if (permission.IsLoaded)
            {
                var id = player.Id.ToString();
                permission.UpdateNickname(id, player.Name);
                var defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(id, defaultGroups.Players)) permission.AddUserGroup(id, defaultGroups.Players);
                if (player.HasPermission("admin") && !permission.UserHasGroup(id, defaultGroups.Administrators)) permission.AddUserGroup(id, defaultGroups.Administrators);
            }

            // Call game hook
            Interface.Call("OnPlayerConnected", player);

            // Let covalence know
            Covalence.PlayerManager.PlayerConnected(player);
            var iplayer = Covalence.PlayerManager.FindPlayerById(player.Id.ToString());
            if (iplayer != null)
            {
                player.IPlayer = iplayer;
                Interface.Call("OnUserConnected", iplayer);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(Player player)
        {
            // Ignore the server player
            if (player.Id == 9999999999) return;

            // Call game hook
            Interface.Call("OnPlayerDisconnected", player);

            // Let covalence know
            Interface.Call("OnUserDisconnected", player.IPlayer, "Unknown"); // TODO: Localization
            Covalence.PlayerManager.PlayerDisconnected(player);
        }

        /// <summary>
        /// Called when the player is spawning
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(PlayerFirstSpawnEvent evt)
        {
            // Call covalence hook
            Interface.Call("OnUserSpawn", evt.Player.IPlayer);
        }

        /// <summary>
        /// Called when the player is respawning
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerRespawn")]
        private void OnPlayerRespawn(PlayerRespawnEvent evt)
        {
            // Call covalence hook
            Interface.Call("OnUserRespawn", evt.Player.IPlayer);
        }

        #endregion Player Hooks
    }
}
