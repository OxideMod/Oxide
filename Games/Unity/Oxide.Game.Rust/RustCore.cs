using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Facepunch;
using Network;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.ServerConsole;
using Oxide.Game.Rust.Libraries;
using Oxide.Game.Rust.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Game.Rust
{
    /// <summary>
    /// The core Rust plugin
    /// </summary>
    public class RustCore : CSPlugin
    {
        #region Initialization

        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal static readonly RustCovalenceProvider Covalence = RustCovalenceProvider.Instance;
        internal static readonly IServer Server = Covalence.CreateServer();
        internal static string ipPattern = @":{1}[0-9]{1}\d*";
        internal bool serverInitialized;
        internal bool isPlayerTakingDamage;

        internal static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            "ownerid", "moderatorid"
        };

        #region Localization

        internal readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"CommandUsageGrant", "Usage: grant <group|user> <name|id> <permission>"},
            {"CommandUsageGroup", "Usage: group <add|set> <name> [title] [rank]"},
            {"CommandUsageGroupParent", "Usage: group <parent> <name> <parentName>"},
            {"CommandUsageGroupRemove", "Usage: group <remove> <name>"},
            {"CommandUsageLang", "Usage: lang <two-digit language code>"},
            {"CommandUsageLoad", "Usage: load *|<pluginname>+"},
            {"CommandUsageReload", "Usage: reload *|<pluginname>+"},
            {"CommandUsageRevoke", "Usage: revoke <group|user> <name|id> <permission>"},
            {"CommandUsageShow", "Usage: show <group|user> <name>\nUsage: show <groups|perms>"}, // TODO: Split this up
            {"CommandUsageUnload", "Usage: unload *|<pluginname>+"},
            {"CommandUsageUserGroup", "Usage: usergroup <add|remove> <username> <groupname>"},
            {"ConnectionRejected", "Connection was rejected"},
            {"GroupAlreadyExists", "Group '{0}' already exists"},
            {"GroupAlreadyHasPermission", "Group '{0}' already has permission '{1}'"},
            {"GroupDoesNotHavePermission", "Group '{0}' does not have permission '{1}'"},
            {"GroupChanged", "Group '{0}' changed"},
            {"GroupCreated", "Group '{0}' created"},
            {"GroupDeleted", "Group '{0}' deleted"},
            {"GroupNotFound", "Group '{0}' doesn't exist"},
            {"GroupParentChanged", "Group '{0}' parent changed to '{1}'"},
            {"GroupParentNotChanged", "Group '{0}' parent was not changed"},
            {"GroupParentNotFound", "Group parent '{0}' doesn't exist"},
            {"GroupPermissionGranted", "Group '{0}' granted permission '{1}'"},
            {"GroupPermissionRevoked", "Group '{0}' revoked permission '{1}'"},
            {"GroupPermissions", "Group '{0}' permissions"},
            {"GroupUsers", "Group '{0}' users"},
            {"Groups", "Groups"},
            {"NoGroupPermissions", "No permissions currently granted"},
            {"NoPermissionGroups", "No groups with this permission"},
            {"NoPermissionUsers", "No users with this permission"},
            {"NoPluginsFound", "No plugins are currently available"},
            {"NoUserGroups", "User is not assigned to any groups"},
            {"NoUserPermissions", "No permissions currently granted"},
            {"NoUsersInGroup", "No users currently in group"},
            {"NotAllowed", "You are not allowed to use the '{0}' command"},
            {"ParentGroupPermissions", "Parent group '{0}' permissions"},
            {"PermissionGroups", "Permission '{0}' Groups"},
            {"PermissionUsers", "Permission '{0}' Users"},
            {"PermissionNotFound", "Permission '{0}' doesn't exist"},
            {"Permissions", "Permissions"},
            {"PermissionsNotLoaded", "Unable to load permission files! Permissions will not work until resolved.\n => {0}"},
            {"PlayerLanguage", "Player language set to {0}"},
            {"PluginNotLoaded", "Plugin '{0}' not loaded."},
            {"PluginReloaded", "Reloaded plugin {0} v{1} by {2}"},
            {"PluginUnloaded", "Unloaded plugin {0} v{1} by {2}"},
            {"ServerLanguage", "Server language set to {0}"},
            {"UnknownCommand", "Unknown command: {0}"},
            {"UserAddedToGroup", "User '{0}' added to group: {1}"},
            {"UserAlreadyHasPermission", "User '{0}' already has permission '{1}'"},
            {"UserDoesNotHavePermission", "User '{0}' does not have permission '{1}'"},
            {"UserNotFound", "User '{0}' not found"},
            {"UserGroups", "User '{0}' groups"},
            {"UserPermissions", "User '{0}' permissions"},
            {"UserPermissionGranted", "User '{0}' granted permission '{1}'"},
            {"UserPermissionRevoked", "User '{0}' revoked permission '{1}'"},
            {"UserRemovedFromGroup", "User '{0}' removed from group '{1}'"},
            {"YouAreNotAdmin", "You are not an admin"}
        };

        #endregion

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustCore()
        {
            var assemblyVersion = RustExtension.AssemblyVersion;

            Name = "RustCore";
            Title = "Rust";
            Author = "Oxide Team";
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool PermissionsLoaded(IPlayer player)
        {
            if (permission.IsLoaded) return true;
            player.Reply(lang.GetMessage("PermissionsNotLoaded", this, player.Id), permission.LastException.Message);
            return false;
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
            RemoteLogger.SetTag("game version", Server.Version);

            lang.RegisterMessages(messages, this);

            // Add core general commands
            AddCovalenceCommand(new[] { "oxide.lang", "lang" }, "LangCommand");
            AddCovalenceCommand(new[] { "oxide.version", "version" }, "VersionCommand");

            // Add core plugin commands
            AddCovalenceCommand(new[] { "oxide.plugins", "plugins" }, "PluginsCommand");
            AddCovalenceCommand(new[] { "oxide.load", "load" }, "LoadCommand");
            AddCovalenceCommand(new[] { "oxide.reload", "reload" }, "ReloadCommand");
            AddCovalenceCommand(new[] { "oxide.unload", "unload" }, "UnloadCommand");

            // Add core permission commands
            AddCovalenceCommand(new[] { "oxide.grant", "grant" }, "GrantCommand");
            AddCovalenceCommand(new[] { "oxide.group", "group" }, "GroupCommand");
            AddCovalenceCommand(new[] { "oxide.revoke", "revoke" }, "RevokeCommand");
            AddCovalenceCommand(new[] { "oxide.show", "show" }, "ShowCommand");
            AddCovalenceCommand(new[] { "oxide.usergroup", "usergroup" }, "UserGroupCommand");

            // Register core permissions
            permission.RegisterPermission("oxide.plugins", this);
            permission.RegisterPermission("oxide.load", this);
            permission.RegisterPermission("oxide.reload", this);
            permission.RegisterPermission("oxide.unload", this);
            permission.RegisterPermission("oxide.grant", this);
            permission.RegisterPermission("oxide.group", this);
            permission.RegisterPermission("oxide.revoke", this);
            permission.RegisterPermission("oxide.show", this);
            permission.RegisterPermission("oxide.usergroup", this);

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
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
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

            if (Interface.Oxide.CheckConsole() && ServerConsole.Instance != null)
            {
                ServerConsole.Instance.enabled = false;
                UnityEngine.Object.Destroy(ServerConsole.Instance);
                typeof(SingletonComponent<ServerConsole>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            }

            RustExtension.ServerConsole();
            Core.Analytics.Collect();

            if (!Interface.Oxide.Config.Options.Modded)
            {
                Interface.Oxide.LogWarning("The server is currently listed under Community. Please be aware that Facepunch only allows admin tools (that do not affect gameplay) under the Community section");
            }
        }

        /// <summary>
        /// Called when the server is saving
        /// </summary>
        [HookMethod("OnServerSave")]
        private void OnServerSave() => Core.Analytics.Collect();

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("IOnServerShutdown")]
        private void IOnServerShutdown()
        {
            Interface.Call("OnServerShutdown");
            Interface.Oxide.OnShutdown();
        }

        /// <summary>
        /// Called when ServerConsole is enabled
        /// </summary>
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
        /// Called when ServerConsole is disabled
        /// </summary>
        [HookMethod("IOnDisableServerConsole")]
        private object IOnDisableServerConsole() => ConsoleWindow.Check(true) && !Interface.Oxide.CheckConsole(true) ? (object)null : false;

        /// <summary>
        /// Called when the command-line is ran
        /// </summary>
        /// <returns></returns>
        [HookMethod("IOnRunCommandLine")]
        private object IOnRunCommandLine()
        {
            foreach (KeyValuePair<string, string> pair in Facepunch.CommandLine.GetSwitches())
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

            if (permission.IsLoaded && authLevel <= DefaultGroups.Length)
            {
                permission.UpdateNickname(id, name);
                if (!permission.UserHasGroup(id, DefaultGroups[0])) permission.AddUserGroup(id, DefaultGroups[0]);
                if (authLevel >= 1 && !permission.UserHasGroup(id, DefaultGroups[authLevel])) permission.AddUserGroup(id, DefaultGroups[authLevel]);
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

        /// <summary>
        /// Called when a locked entity is used
        /// This is used to call the deprecated hook CanUseLock for a limited time
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseLock"></param>
        /// <returns></returns>
        [HookMethod("CanUseLockedEntity")]
        private object CanUseLockedEntity(BasePlayer player, BaseLock baseLock) => Interface.CallDeprecatedHook("CanUseLock", "CanUseLockedEntity", new DateTime(2017, 4, 13), player, baseLock);

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

        /// <summary>
        /// Called when an item is used
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        [HookMethod("OnItemUse")]
        private void OnItemUse(Item item, int amount) => Interface.Oxide.CallDeprecatedHook("OnConsumableUse", "OnItemUse", new DateTime(2017, 4, 1), item, amount);

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

        #region Covalence Commands

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("PluginsCommand")]
        private void PluginsCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !player.HasPermission("oxide.plugins"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            var loadedPlugins = pluginManager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            var loadedPluginNames = new HashSet<string>(loadedPlugins.Select(pl => pl.Name));
            var unloadedPluginErrors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loadedPluginNames))
                {
                    string msg;
                    unloadedPluginErrors[name] = (loader.PluginErrors.TryGetValue(name, out msg)) ? msg : "Unloaded";
                }
            }

            var totalPluginCount = loadedPlugins.Length + unloadedPluginErrors.Count;
            if (totalPluginCount < 1)
            {
                player.Reply(lang.GetMessage("NoPluginsFound", this, player.Id));
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loadedPlugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var pluginName in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            player.Reply(output);
        }

        #endregion

        #region Load Command

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("LoadCommand")]
        private void LoadCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !player.HasPermission("oxide.load"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageLoad", this, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;
                Interface.Oxide.LoadPlugin(name);
                pluginManager.GetPlugin(name);
            }
        }

        #endregion

        #region Reload Command

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ReloadCommand")]
        private void ReloadCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !player.HasPermission("oxide.reload"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageReload", this, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in args)
                if (!string.IsNullOrEmpty(name)) Interface.Oxide.ReloadPlugin(name);
        }

        #endregion

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("UnloadCommand")]
        private void UnloadCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !player.HasPermission("oxide.unload"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id));
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageUnload", this, player.Id));
                return;
            }

            if (args[0].Equals("*") || args[0].Equals("all"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in args)
                if (!string.IsNullOrEmpty(name)) Interface.Oxide.UnloadPlugin(name);
        }

        #endregion

        #region Version Command

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("VersionCommand")]
        private void VersionCommand(IPlayer player, string command, string[] args)
        {
            if (player.Id != "server_console")
            {
                var format = Covalence.FormatText("Server is running [#ffb658]Oxide {0}[/#] and [#ee715c]{1} {2}[/#]");
                player.Reply(format, OxideMod.Version, Covalence.GameName, Server.Version);
            }
            else
            {
                player.Reply($"Protocol: {Server.Protocol}\nBuild Date: {BuildInfo.Current.BuildDate}\n" +
                $"Unity Version: {Application.unityVersion}\nChangeset: {BuildInfo.Current.Scm.ChangeId}\n" +
                $"Branch: {BuildInfo.Current.Scm.Branch}\nOxide Version: {OxideMod.Version}");
            }
        }

        #endregion

        #region Lang Command

        /// <summary>
        /// Called when the "lang" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("LangCommand")]
        private void LangCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageLang", this, player.Id));
                return;
            }

            if (player.Id != "server_console")
            {
                lang.SetLanguage(args[0], player.Id);
                player.Reply(lang.GetMessage("PlayerLanguage", this, player.Id), args[0]);
            }
            else
            {
                lang.SetServerLanguage(args[0]);
                player.Reply(lang.GetMessage("ServerLanguage", this, player.Id), lang.GetServerLanguage());
            }
        }

        #endregion

        #region Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("GroupCommand")]
        private void GroupCommand(IPlayer player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!player.IsAdmin && !player.HasPermission("oxide.group"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 2)
            {
                player.Reply(lang.GetMessage("CommandUsageGroup", this, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupParent", this, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupRemove", this, player.Id));
                return;
            }

            var mode = args[0];
            var group = args[1];
            var title = args.Length >= 3 ? args[2] : "";
            var rank = args.Length == 4 ? int.Parse(args[3]) : 0;

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupAlreadyExists", this, player.Id), group);
                    return;
                }

                permission.CreateGroup(group, title, rank);
                player.Reply(lang.GetMessage("GroupCreated", this, player.Id), group);
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), group);
                    return;
                }

                permission.RemoveGroup(group);
                player.Reply(lang.GetMessage("GroupDeleted", this, player.Id), group);
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), group);
                    return;
                }

                permission.SetGroupTitle(group, title);
                permission.SetGroupRank(group, rank);
                player.Reply(lang.GetMessage("GroupChanged", this, player.Id), group);
            }
            else if (mode.Equals("parent"))
            {
                if (args.Length <= 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageGroupParent", this, player.Id));
                    return;
                }

                if (!permission.GroupExists(group))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), group);
                    return;
                }

                var parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    player.Reply(lang.GetMessage("GroupParentNotFound", this, player.Id), parent);
                    return;
                }

                if (permission.SetGroupParent(group, parent))
                    player.Reply(lang.GetMessage("GroupParentChanged", this, player.Id), group, parent);
                else
                    player.Reply(lang.GetMessage("GroupParentNotChanged", this, player.Id), group);
            }
            else
            {
                player.Reply(lang.GetMessage("CommandUsageGroup", this, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupParent", this, player.Id));
                player.Reply(lang.GetMessage("CommandUsageGroupRemove", this, player.Id));
            }
        }

        #endregion

        #region User Group Command

        /// <summary>
        /// Called when the "usergroup" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("UserGroupCommand")]
        private void UserGroupCommand(IPlayer player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!player.IsAdmin && !player.HasPermission("oxide.usergroup"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageUserGroup", this, player.Id));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = Covalence.PlayerManager.FindPlayer(name);
            if (target == null && !permission.UserIdValid(name))
            {
                player.Reply(lang.GetMessage("UserNotFound", this, player.Id), name);
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.Id;
                name = target.Name;
                permission.UpdateNickname(userId, name);
                name += $"({userId})";
            }

            if (!permission.GroupExists(group))
            {
                player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), group);
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                player.Reply(lang.GetMessage("UserAddedToGroup", this, player.Id), name, group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                player.Reply(lang.GetMessage("UserRemovedFromGroup", this, player.Id), name, group);
            }
            else player.Reply(lang.GetMessage("CommandUsageUserGroup", this, player.Id));
        }

        #endregion

        #region Grant Command

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("GrantCommand")]
        private void GrantCommand(IPlayer player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!player.IsAdmin && !player.HasPermission("oxide.grant"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageGrant", this, player.Id));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                player.Reply(lang.GetMessage("PermissionNotFound", this, player.Id), perm);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), name);
                    return;
                }

                if (permission.GroupHasPermission(name, perm))
                {
                    player.Reply(lang.GetMessage("GroupAlreadyHasPermission", this, player.Id), name, perm);
                    return;
                }

                permission.GrantGroupPermission(name, perm, null);
                player.Reply(lang.GetMessage("GroupPermissionGranted", this, player.Id), name, perm);
            }
            else if (mode.Equals("user"))
            {
                var target = Covalence.PlayerManager.FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(lang.GetMessage("UserNotFound", this, player.Id), name);
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }

                if (permission.UserHasPermission(name, perm))
                {
                    player.Reply(lang.GetMessage("UserAlreadyHasPermission", this, player.Id), userId, perm);
                    return;
                }

                permission.GrantUserPermission(userId, perm, null);
                player.Reply(lang.GetMessage("UserPermissionGranted", this, player.Id), $"{name} ({userId})", perm);
            }
            else player.Reply(lang.GetMessage("CommandUsageGrant", this, player.Id));
        }

        #endregion

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("RevokeCommand")]
        private void RevokeCommand(IPlayer player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!player.IsAdmin && !player.HasPermission("oxide.revoke"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 3)
            {
                player.Reply(lang.GetMessage("CommandUsageRevoke", this, player.Id));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), name);
                    return;
                }

                if (!permission.GroupHasPermission(name, perm))
                {
                    // TODO: Check if group is inheriting permission, mention
                    player.Reply(lang.GetMessage("GroupDoesNotHavePermission", this, player.Id), name, perm);
                    return;
                }

                permission.RevokeGroupPermission(name, perm);
                player.Reply(lang.GetMessage("GroupPermissionRevoked", this, player.Id), name, perm);
            }
            else if (mode.Equals("user"))
            {
                var target = Covalence.PlayerManager.FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(lang.GetMessage("UserNotFound", this, player.Id), name);
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }

                if (!permission.UserHasPermission(userId, perm))
                {
                    // TODO: Check if user is inheriting permission, mention
                    player.Reply(lang.GetMessage("UserDoesNotHavePermission", this, player.Id), name, perm);
                    return;
                }

                permission.RevokeUserPermission(userId, perm);
                player.Reply(lang.GetMessage("UserPermissionRevoked", this, player.Id), $"{name} ({userId})", perm);
            }
            else player.Reply(lang.GetMessage("CommandUsageRevoke", this, player.Id));
        }

        #endregion

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ShowCommand")]
        private void ShowCommand(IPlayer player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!player.IsAdmin && !player.HasPermission("oxide.show"))
            {
                player.Reply(lang.GetMessage("NotAllowed", this, player.Id), command);
                return;
            }

            if (args.Length < 1)
            {
                player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                return;
            }

            var mode = args[0];
            var name = args.Length == 2 ? args[1] : string.Empty;

            if (mode.Equals("perms"))
            {
                player.Reply(lang.GetMessage("Permissions", this, player.Id) + ":\n" + string.Join(", ", permission.GetPermissions()));
            }
            else if (mode.Equals("perm"))
            {
                if (args.Length < 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                    return;
                }

                var users = permission.GetPermissionUsers(name);
                var groups = permission.GetPermissionGroups(name);
                var result = $"{string.Format(lang.GetMessage("PermissionUsers", this, player.Id), name)}:\n";
                result += users.Length > 0 ? string.Join(", ", users) : lang.GetMessage("NoPermissionUsers", this, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("PermissionGroups", this, player.Id), name)}:\n";
                result += groups.Length > 0 ? string.Join(", ", groups) : lang.GetMessage("NoPermissionGroups", this, player.Id);
                player.Reply(result);
            }
            else if (mode.Equals("user"))
            {
                if (args.Length < 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                    return;
                }

                var target = Covalence.PlayerManager.FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    player.Reply(lang.GetMessage("UserNotFound", this, player.Id), name);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.Id;
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }

                var perms = permission.GetUserPermissions(userId);
                var groups = permission.GetUserGroups(userId);
                var result = $"{string.Format(lang.GetMessage("UserPermissions", this, player.Id), name)}:\n";
                result += perms.Length > 0 ? string.Join(", ", perms) : lang.GetMessage("NoUserPermissions", this, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("UserGroups", this, player.Id), name)}:\n";
                result += groups.Length > 0 ? string.Join(", ", groups) : lang.GetMessage("NoUserGroups", this, player.Id);
                player.Reply(result);
            }
            else if (mode.Equals("group"))
            {
                if (args.Length < 2)
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
                    return;
                }

                if (!permission.GroupExists(name))
                {
                    player.Reply(lang.GetMessage("GroupNotFound", this, player.Id), name);
                    return;
                }

                var users = permission.GetUsersInGroup(name);
                var perms = permission.GetGroupPermissions(name);
                var result = $"{string.Format(lang.GetMessage("GroupUsers", this, player.Id), name)}:\n";
                result += users.Length > 0 ? string.Join(", ", users) : lang.GetMessage("NoUsersInGroup", this, player.Id);
                result += $"\n\n{string.Format(lang.GetMessage("GroupPermissions", this, player.Id), name)}:\n";
                result += perms.Length > 0 ? string.Join(", ", perms) : lang.GetMessage("NoGroupPermissions", this, player.Id);
                var parent = permission.GetGroupParent(name);
                while (permission.GroupExists(parent))
                {
                    result += $"\n{string.Format(lang.GetMessage("ParentGroupPermissions", this, player.Id), parent)}:\n";
                    result += string.Join(", ", permission.GetGroupPermissions(parent));
                    parent = permission.GetGroupParent(parent);
                }
                player.Reply(result);
            }
            else if (mode.Equals("groups"))
            {
                player.Reply(lang.GetMessage("Groups", this, player.Id) + ":\n" + string.Join(", ", permission.GetGroups()));
            }
            else player.Reply(lang.GetMessage("CommandUsageShow", this, player.Id));
        }

        #endregion

        #endregion

        #region Command Handling

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnServerCommand")]
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.cmd == null) return null;
            if (arg.cmd.FullName != "chat.say") return null;

            try
            {
                // Get the args
                var str = arg.GetString(0);
                if (string.IsNullOrEmpty(str)) return null;

                // Is it a chat command?
                if (str[0] != '/') return null; // TODO: Return if no arguments given

                // Get the full command
                var message = str.Substring(1);

                // Parse it
                string cmd;
                string[] args;
                ParseChatCommand(message, out cmd, out args);
                if (cmd == null) return null;

                // Check if command is from a player
                var player = arg.Connection?.player as BasePlayer;
                if (player == null) return null;

                // Disable chat commands for non-admins if the server is not set to modded
                if (!Interface.Oxide.Config.Options.Modded && !player.IsAdmin && !permission.UserHasGroup(player.UserIDString, "admin")) return null;

                // Get the covalence player
                var iplayer = Covalence.PlayerManager.FindPlayerById(arg.Connection.userid.ToString());
                if (iplayer == null) return null;

                // Is the command blocked?
                var blockedSpecific = Interface.Call("OnPlayerCommand", arg);
                var blockedCovalence = Interface.Call("OnUserCommand", iplayer, cmd, args);
                if (blockedSpecific != null || blockedCovalence != null) return true;

                // Is it a covalance command?
                if (Covalence.CommandSystem.HandleChatMessage(iplayer, str)) return true;

                // Is it a regular chat command?
                if (!cmdlib.HandleChatCommand(player, cmd, args))
                    iplayer.Reply(lang.GetMessage("UnknownCommand", this, iplayer.Id), cmd);
            }
            catch (NullReferenceException ex)
            {
                var sb = new StringBuilder();
                try
                {
                    var str = arg?.GetString(0);
                    var message = str?.Substring(1);
                    string cmd;
                    string[] args;
                    ParseChatCommand(message, out cmd, out args);
                    sb.AppendLine("NullReferenceError in Oxide.Game.Rust when running OnServerCommand.");
                    sb.AppendLine($"  Command: {arg.cmd?.FullName}");
                    sb.AppendLine($"  Full command: {str}");
                    sb.AppendLine($"  Command: {cmd}");
                    sb.AppendLine($"  Arguments: {args.ToSentence()}");
                    sb.AppendLine($"  Connection: {arg.Connection}");
                    sb.AppendLine($"  Connection ID: {arg.Connection?.userid}");
                    sb.AppendLine($"  Connection player? {arg.Connection?.player != null}");
                    sb.AppendLine($"  Connection player: {arg.Connection?.player}");
                    sb.AppendLine($"  Connection player type: {arg.Connection?.player?.GetType()}");
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    RemoteLogger.Exception(ex.Message, sb.ToString());
                }
            }

            // Handled
            arg.ReplyWith(string.Empty);
            return true;
        }

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseChatCommand(string argstr, out string cmd, out string[] args)
        {
            var arglist = new List<string>();
            var sb = new StringBuilder();
            var inlongarg = false;

            foreach (var c in argstr)
            {
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        var arg = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                        sb.Clear();
                        inlongarg = false;
                    }
                    else
                    {
                        inlongarg = true;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    var arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                var arg = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
            }
            if (arglist.Count == 0)
            {
                cmd = null;
                args = null;
                return;
            }
            cmd = arglist[0];
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the BasePlayer for the specified name, ID, or IP address string
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            BasePlayer player = null;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (string.IsNullOrEmpty(activePlayer.UserIDString)) continue;
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    return activePlayer;
                if (string.IsNullOrEmpty(activePlayer.displayName)) continue;
                if (activePlayer.displayName.Equals(nameOrIdOrIp, StringComparison.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    player = activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (string.IsNullOrEmpty(sleepingPlayer.UserIDString)) continue;
                if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                    return sleepingPlayer;
                if (string.IsNullOrEmpty(sleepingPlayer.displayName)) continue;
                if (sleepingPlayer.displayName.Equals(nameOrIdOrIp, StringComparison.OrdinalIgnoreCase))
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    player = sleepingPlayer;
            }
            return player;
        }

        /// <summary>
        /// Returns the BasePlayer for the specified name string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static BasePlayer FindPlayerByName(string name)
        {
            BasePlayer player = null;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (string.IsNullOrEmpty(activePlayer.displayName)) continue;
                if (activePlayer.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
                    player = activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (string.IsNullOrEmpty(sleepingPlayer.displayName)) continue;
                if (sleepingPlayer.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(name, CompareOptions.OrdinalIgnoreCase))
                    player = sleepingPlayer;
            }
            return player;
        }

        /// <summary>
        /// Returns the BasePlayer for the specified ID ulong
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static BasePlayer FindPlayerById(ulong id)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID == id)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.userID == id)
                    return sleepingPlayer;
            }
            return null;
        }

        /// <summary>
        /// Returns the BasePlayer for the specified ID string
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static BasePlayer FindPlayerByIdString(string id)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (string.IsNullOrEmpty(activePlayer.UserIDString)) continue;
                if (activePlayer.UserIDString.Equals(id))
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (string.IsNullOrEmpty(sleepingPlayer.UserIDString)) continue;
                if (sleepingPlayer.UserIDString.Equals(id))
                    return sleepingPlayer;
            }
            return null;
        }

        #endregion
    }
}
