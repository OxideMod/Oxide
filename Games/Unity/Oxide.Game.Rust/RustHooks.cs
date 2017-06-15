using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Network;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.RemoteConsole;
using Oxide.Core.ServerConsole;

namespace Oxide.Game.Rust
{
    /// <summary>
    /// Game hook and wrappers for the core Rust plugin
    /// </summary>
    public partial class RustCore : CSPlugin
    {
        internal bool isPlayerTakingDamage;
        internal static string ipPattern = @":{1}[0-9]{1}\d*";

        #region Server Hooks

        /// <summary>
        /// Called when ServerConsole is disabled
        /// </summary>
        /// <returns></returns>
        [HookMethod("IOnDisableServerConsole")]
        private object IOnDisableServerConsole() => ConsoleWindow.Check(true) && !Interface.Oxide.CheckConsole(true) ? (object)null : false;

        /// <summary>
        /// Called when ServerConsole is enabled
        /// </summary>
        /// <returns></returns>
        [HookMethod("IOnEnableServerConsole")]
        private object IOnEnableServerConsole(ServerConsole serverConsole)
        {
            if (ConsoleWindow.Check(true) && !Interface.Oxide.CheckConsole(true)) return null;
            serverConsole.enabled = false;
            UnityEngine.Object.Destroy(serverConsole);
            typeof(SingletonComponent<ServerConsole>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            return false;
        }

        /// <summary>
        /// Called when a remote console was run
        /// </summary>
        /// <returns></returns>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        [HookMethod("IOnRconCommand")]
        private object IOnRconCommand(IPAddress sender, string message)
        {
            if (string.IsNullOrEmpty(message)) return null;

            var msg = RemoteMessage.GetMessage(message);
            if (msg == null) return null;

            var args = msg.Message.Split(' ');
            var cmd = args[0];
            var call = Interface.Call("OnRconCommand", sender, cmd, (args.Length > 1) ? args.Skip(1).ToArray() : null);
            if (call != null) return true;

            return null;
        }

        /// <summary>
        /// Called when the command-line is ran
        /// </summary>
        /// <returns></returns>
        [HookMethod("IOnRunCommandLine")]
        private object IOnRunCommandLine()
        {
            foreach (var pair in Facepunch.CommandLine.GetSwitches())
            {
                var value = pair.Value;
                if (value == "") value = "1";
                var str = pair.Key.Substring(1);
                var options = ConsoleSystem.Option.Unrestricted;
                options.PrintOutput = false;
                ConsoleSystem.Run(options, str, value);
            }
            return false;
        }

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a BasePlayer is attacked
        /// This is used to call OnEntityTakeDamage for a BasePlayer when attacked
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        [HookMethod("IOnBasePlayerAttacked")]
        private object IOnBasePlayerAttacked(BasePlayer player, HitInfo info)
        {
            if (!serverInitialized || player == null || info == null || player.IsDead() || isPlayerTakingDamage) return null;
            if (Interface.Call("OnEntityTakeDamage", player, info) != null) return true;

            isPlayerTakingDamage = true;
            try
            {
                player.OnAttacked(info);
            }
            finally
            {
                isPlayerTakingDamage = false;
            }
            return true;
        }

        /// <summary>
        /// Called when a BasePlayer is hurt
        /// This is used to call OnEntityTakeDamage when a player was hurt without being attacked
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HookMethod("IOnBasePlayerHurt")]
        private object IOnBasePlayerHurt(BasePlayer player, HitInfo info)
        {
            return isPlayerTakingDamage ? null : Interface.Call("OnEntityTakeDamage", player, info);
        }

        /// <summary>
        /// Called when the player starts looting an entity
        /// </summary>
        /// <param name="source"></param>
        /// <param name="entity"></param>
        [HookMethod("IOnLootEntity")]
        private void IOnLootEntity(PlayerLoot source, BaseEntity entity) => Interface.Call("OnLootEntity", source.GetComponent<BasePlayer>(), entity);

        /// <summary>
        /// Called when the player starts looting an item
        /// </summary>
        /// <param name="source"></param>
        /// <param name="item"></param>
        [HookMethod("IOnLootItem")]
        private void IOnLootItem(PlayerLoot source, Item item) => Interface.Call("OnLootItem", source.GetComponent<BasePlayer>(), item);

        /// <summary>
        /// Called when the player starts looting another player
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        [HookMethod("IOnLootPlayer")]
        private void IOnLootPlayer(PlayerLoot source, BasePlayer target) => Interface.Call("OnLootPlayer", source.GetComponent<BasePlayer>(), target);

        /// <summary>
        /// Called when a player attacks something
        /// </summary>
        /// <param name="melee"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerAttack")]
        private object IOnPlayerAttack(BaseMelee melee, HitInfo info) => Interface.Call("OnPlayerAttack", melee.GetOwnerPlayer(), info);

        /// <summary>
        /// Called when a player revives another player
        /// </summary>
        /// <param name="tool"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerRevive")]
        private object IOnPlayerRevive(MedicalTool tool, BasePlayer target) => Interface.Call("OnPlayerRevive", tool.GetOwnerPlayer(), target);

        /// <summary>
        /// Called when a server group is set for an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="group"></param>
        /// <param name="name"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        [HookMethod("IOnServerUsersSet")]
        private void IOnServerUsersSet(ulong id, ServerUsers.UserGroup group, string name, string reason)
        {
            if (!serverInitialized) return;

            var iplayer = Covalence.PlayerManager.FindPlayerById(id.ToString());
            if (group == ServerUsers.UserGroup.Banned) Interface.Oxide.CallHook("OnUserBanned", name, id.ToString(), iplayer?.Address ?? "0", reason);
        }

        /// <summary>
        /// Called when a player is attempting to connect
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Connection connection)
        {
            var name = connection.username;
            var id = connection.userid.ToString();
            var authLevel = connection.authLevel;
            var ip = Regex.Replace(connection.ipaddress, ipPattern, "");

            // Update player's permissions group and name
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(id, name);
                var defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(id, defaultGroups.Players)) permission.AddUserGroup(id, defaultGroups.Players);
                if (authLevel >= 1 && !permission.UserHasGroup(id, defaultGroups.Administrators)) permission.AddUserGroup(id, defaultGroups.Administrators);
            }

            Covalence.PlayerManager.PlayerJoin(connection.userid, name);

            var loginSpecific = Interface.Call("CanClientLogin", connection);
            var loginCovalence = Interface.Call("CanUserLogin", name, id, ip);
            var canLogin = loginSpecific ?? loginCovalence; // TODO: Fix 'RustCore' hook conflict when both return

            if (canLogin is string || (canLogin is bool && !(bool)canLogin))
            {
                ConnectionAuth.Reject(connection, canLogin is string ? canLogin.ToString() : lang.GetMessage("ConnectionRejected", this, id));
                return true;
            }

            var approvedSpecific = Interface.Call("OnUserApprove", connection);
            var approvedCovalence = Interface.Call("OnUserApproved", name, id, ip);
            return approvedSpecific ?? approvedCovalence; // TODO: Fix 'RustCore' hook conflict when both return
        }

        /// <summary>
        /// Called when a player has been banned on connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerBanned")]
        private void OnPlayerBanned(Connection connection, string reason)
        {
            var ip = Regex.Replace(connection.ipaddress, ipPattern, "");
            Interface.Oxide.CallHook("OnUserBanned", connection.username, connection.userid.ToString(), ip, reason);
        }

        /// <summary>
        /// Called when the player has been initialized
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerChat")]
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var iplayer = Covalence.PlayerManager.FindPlayerById(arg.Connection.userid.ToString());
            return string.IsNullOrEmpty(arg.GetString(0)) ? null : Interface.Call("OnUserChat", iplayer, arg.GetString(0));
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var iplayer = Covalence.PlayerManager.FindPlayerById(player.UserIDString);
            if (iplayer != null) Interface.Call("OnUserDisconnected", iplayer, reason);
            Covalence.PlayerManager.PlayerDisconnected(player);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(BasePlayer player)
        {
            // Set language for player
            lang.SetLanguage(player.net.connection.info.GetString("global.language", "en"), player.UserIDString);

            Covalence.PlayerManager.PlayerConnected(player);
            var iplayer = Covalence.PlayerManager.FindPlayerById(player.UserIDString);
            if (iplayer != null) Interface.Call("OnUserConnected", iplayer);
        }

        /// <summary>
        /// Called when the player is respawning
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerRespawn")]
        private object OnPlayerRespawn(BasePlayer player)
        {
            var iplayer = Covalence.PlayerManager.FindPlayerById(player.UserIDString);
            return iplayer != null ? Interface.Call("OnUserRespawn", iplayer) : null;
        }

        /// <summary>
        /// Called when the player has respawned
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerRespawned")]
        private void OnPlayerRespawned(BasePlayer player)
        {
            var iplayer = Covalence.PlayerManager.FindPlayerById(player.UserIDString);
            if (iplayer != null) Interface.Call("OnUserRespawned", iplayer);
        }

        /// <summary>
        /// Called when a player tick is received from a client
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerTick")]
        private object OnPlayerTick(BasePlayer player) => Interface.Call("OnPlayerInput", player, player.serverInput);

        #endregion

        #region Entity Hooks

        /// <summary>
        /// Called when a BaseCombatEntity takes damage
        /// This is used to call OnEntityTakeDamage for anything other than a BasePlayer
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HookMethod("IOnBaseCombatEntityHurt")]
        private object IOnBaseCombatEntityHurt(BaseCombatEntity entity, HitInfo info)
        {
            return entity is BasePlayer ? null : Interface.Call("OnEntityTakeDamage", entity, info);
        }

        #endregion

        #region Item Hooks

        /// <summary>
        /// Called when an item loses durability
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        [HookMethod("IOnLoseCondition")]
        private object IOnLoseCondition(Item item, float amount)
        {
            var arguments = new object[] { item, amount };
            Interface.Call("OnLoseCondition", arguments);
            amount = (float)arguments[1];
            var condition = item.condition;
            item.condition -= amount;
            if ((item.condition <= 0f) && (item.condition < condition)) item.OnBroken();
            return true;
        }

        #endregion

        #region Structure Hooks

        /// <summary>
        /// Called when a player selects Demolish from the BuildingBlock menu
        /// </summary>
        /// <param name="block"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnStructureDemolish")]
        private object IOnStructureDemolish(BuildingBlock block, BasePlayer player) => Interface.Call("OnStructureDemolish", block, player, false);

        /// <summary>
        /// Called when a player selects Demolish Immediate from the BuildingBlock menu
        /// </summary>
        /// <param name="block"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnStructureImmediateDemolish")]
        private object IOnStructureImmediateDemolish(BuildingBlock block, BasePlayer player) => Interface.Call("OnStructureDemolish", block, player, true);

        #endregion
    }
}
