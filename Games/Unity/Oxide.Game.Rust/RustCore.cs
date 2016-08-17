using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Network;
using Rust;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;
using Oxide.Game.Rust.Libraries.Covalence;

namespace Oxide.Game.Rust
{
    /// <summary>
    /// The core Rust plugin
    /// </summary>
    public class RustCore : CSPlugin
    {
        #region Initialization

        // The plugin manager
        private readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The covalence provider
        private readonly RustCovalenceProvider covalence = RustCovalenceProvider.Instance;

        #region Localization

        // The language library
        private readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        private readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"CommandUsageLoad", "Usage: load *|<pluginname>+"},
            {"CommandUsageGrant", "Usage: grant <group|user> <name|id> <permission>"},
            {"CommandUsageGroup", "Usage: group <add|remove|set> <name> [title] [rank]"},
            {"CommandUsageReload", "Usage: reload *|<pluginname>+"},
            {"CommandUsageRevoke", "Usage: revoke <group|user> <name|id> <permission>"},
            {"CommandUsageShow", "Usage: show <group|user> <name>\nUsage: show <groups|perms>"},
            {"CommandUsageUnload", "Usage: unload *|<pluginname>+"},
            {"CommandUsageUserGroup", "Usage: usergroup <add|remove> <username> <groupname>"},
            {"GroupAlreadyExists", "Group '{0}' already exists"},
            {"GroupChanged", "Group '{0}' changed"},
            {"GroupCreated", "Group '{0}' created"},
            {"GroupDeleted", "Group '{0}' deleted"},
            {"GroupNotFound", "Group '{0}' doesn't exist"},
            {"GroupParentChanged", "Group '{0}' parent changed to '{1}'"},
            {"GroupParentNotChanged", "Group '{0}' parent was not changed"},
            {"GroupParentNotFound", "Group parent '{0}' doesn't exist"},
            {"GroupPermissionGranted", "Group '{0}' granted permission '{1}'"},
            {"GroupPermissionRevoked", "Group '{0}' revoked permission '{1}'"},
            {"NoPluginsFound", "No plugins are currently available"},
            {"PermissionNotFound", "Permission '{0}' doesn't exist"},
            {"PermissionsNotLoaded", "Unable to load permission files! Permissions will not work until resolved.\n => {0}"},
            {"PlayerLanguage", "Player language set to {0}"},
            {"PluginNotLoaded", "Plugin '{0}' not loaded."},
            {"PluginReloaded", "Reloaded plugin {0} v{1} by {2}"},
            {"PluginUnloaded", "Unloaded plugin {0} v{1} by {2}"},
            {"ServerLanguage", "Server language set to {0}"},
            {"UnknownCommand", "Unknown command: {0}"},
            {"UserAddedToGroup", "User '{0}' added to group: {1}"},
            {"UserNotFound", "User '{0}' not found"},
            {"UserPermissionGranted", "User '{0}' granted permission '{1}'"},
            {"UserPermissionRevoked", "User '{0}' revoked permission '{1}'"},
            {"UserRemovedFromGroup", "User '{0}' removed from group '{1}'"},
            {"YouAreNotAdmin", "You are not an admin"}
        };

        #endregion

        // Track when the server has been initialized
        private bool serverInitialized;

        // Cache the serverInput field info
        private readonly FieldInfo serverInputField = typeof(BasePlayer).GetField("serverInput", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly Dictionary<BasePlayer, InputState> playerInputState = new Dictionary<BasePlayer, InputState>();

        // Track if a BasePlayer.OnAttacked call is in progress
        private bool isPlayerTakingDamage;

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustCore()
        {
            // Set attributes
            Name = "RustCore";
            Title = "Rust";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            // Cheat references in the default plugin reference list
            var fpNetwork = Network.Client.disconnectReason; // Facepunch.Network
            var fpSystem = Facepunch.Math.Epoch.Current; // Facepunch.System
            var fpUnity = TimeWarning.Enabled; // Facepunch.UnityEngine
            var rustXp = global::Rust.Xp.Config.LevelToXp(1); // Rust.Xp
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(ConsoleSystem.Arg arg)
        {
            if (permission.IsLoaded) return true;
            Reply(arg, "PermissionsNotLoaded", permission.LastException.Message);
            return false;
        }

        #endregion

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("version", BuildInformation.VersionStampDays.ToString());

            // Register messages for localization
            lang.RegisterMessages(messages, this);

            // Add general commands
            cmdlib.AddConsoleCommand("oxide.plugins", this, "ConsolePlugins");
            cmdlib.AddConsoleCommand("global.plugins", this, "ConsolePlugins");
            cmdlib.AddConsoleCommand("oxide.load", this, "ConsoleLoad");
            cmdlib.AddConsoleCommand("global.load", this, "ConsoleLoad");
            cmdlib.AddConsoleCommand("oxide.unload", this, "ConsoleUnload");
            cmdlib.AddConsoleCommand("global.unload", this, "ConsoleUnload");
            cmdlib.AddChatCommand("reload", this, "ChatReload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "ConsoleReload");
            cmdlib.AddConsoleCommand("global.reload", this, "ConsoleReload");
            cmdlib.AddChatCommand("version", this, "ChatVersion");
            cmdlib.AddChatCommand("oxide.version", this, "ChatVersion");
            cmdlib.AddConsoleCommand("oxide.version", this, "ConsoleVersion");
            cmdlib.AddChatCommand("lang", this, "ChatLang");
            cmdlib.AddConsoleCommand("oxide.lang", this, "ConsoleLang");
            cmdlib.AddConsoleCommand("global.lang", this, "ConsoleLang");

            // Add permission commands
            cmdlib.AddConsoleCommand("oxide.group", this, "ConsoleGroup");
            cmdlib.AddConsoleCommand("global.group", this, "ConsoleGroup");
            cmdlib.AddConsoleCommand("oxide.usergroup", this, "ConsoleUserGroup");
            cmdlib.AddConsoleCommand("global.usergroup", this, "ConsoleUserGroup");
            cmdlib.AddConsoleCommand("oxide.grant", this, "ConsoleGrant");
            cmdlib.AddConsoleCommand("global.grant", this, "ConsoleGrant");
            cmdlib.AddConsoleCommand("oxide.revoke", this, "ConsoleRevoke");
            cmdlib.AddConsoleCommand("global.revoke", this, "ConsoleRevoke");
            cmdlib.AddConsoleCommand("oxide.show", this, "ConsoleShow");
            cmdlib.AddConsoleCommand("global.show", this, "ConsoleShow");

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

            // Migrate default player groups
            permission.MigrateGroup("player", "default");

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", ConVar.Server.hostname);

            // Destroy default server console
            if (ServerConsole.Instance != null)
            {
                ServerConsole.Instance.enabled = false;
                UnityEngine.Object.Destroy(ServerConsole.Instance);
                typeof(SingletonComponent<ServerConsole>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            }

            // Update server console window and status bars
            RustExtension.ServerConsole();

            // Check for 'load' variable
            if (Interface.Oxide.CommandLine.HasVariable("load")) Interface.Oxide.LogWarning("The 'load' startup variable is unused and can be removed");
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        /// <summary>
        /// Called when ServerConsole is enabled
        /// </summary>
        [HookMethod("IOnEnableServerConsole")]
        private object IOnEnableServerConsole(ServerConsole serverConsole)
        {
            if (!Interface.Oxide.CheckConsole(true)) return null;
            serverConsole.enabled = false;
            UnityEngine.Object.Destroy(serverConsole);
            typeof(SingletonComponent<ServerConsole>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
            return false;
        }

        /// <summary>
        /// Called when ServerConsole is disabled
        /// </summary>
        [HookMethod("IOnDisableServerConsole")]
        private object IOnDisableServerConsole() => !Interface.Oxide.CheckConsole(true) ? (object)null : false;

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Connection connection)
        {
            var id = connection.userid.ToString();
            var ip = Regex.Replace(connection.ipaddress, @":{1}[0-9]{1}\d*", "");

            // Migrate user from 'player' group to 'default'
            if (permission.UserHasGroup(id, "player"))
            {
                permission.AddUserGroup(id, "default");
                permission.RemoveUserGroup(id, "player");
                Interface.Oxide.LogWarning($"Migrated '{id}' to the new 'default' group");
            }

            // Call out and see if we should reject
            var canLogin = Interface.Call("CanClientLogin", connection) ?? Interface.Call("CanUserLogin", connection.username, id, ip);
            if (canLogin != null && (!(canLogin is bool) || !(bool)canLogin))
            {
                // Reject the user with the message
                ConnectionAuth.Reject(connection, canLogin.ToString());
                return true;
            }

            return Interface.Call("OnUserApprove", connection) ?? Interface.Call("OnUserApproved", connection.username, id, ip);
        }

        /// <summary>
        /// Called when the player has been initialized
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerChat")]
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            // Call covalence hook
            return Interface.Call("OnUserChat", covalence.PlayerManager.GetPlayer(arg.connection.userid.ToString()), arg.Args[0]);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // Let covalence know
            Interface.Call("OnUserDisconnected", covalence.PlayerManager.GetPlayer(player.UserIDString), reason);
            covalence.PlayerManager.NotifyPlayerDisconnect(player);

            playerInputState.Remove(player);
        }

        /// <summary>
        /// Called when the player has been initialized
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(BasePlayer player)
        {
            // Do permission stuff
            var authLevel = player.net.connection.authLevel;
            if (permission.IsLoaded && authLevel <= DefaultGroups.Length)
            {
                var id = player.UserIDString;

                // Add player to default group
                if (!permission.UserHasGroup(id, DefaultGroups[0])) permission.AddUserGroup(id, DefaultGroups[0]);

                // Add player to group based on auth level
                if (authLevel >= 1 && !permission.UserHasGroup(id, DefaultGroups[authLevel])) permission.AddUserGroup(id, DefaultGroups[authLevel]);

                permission.UpdateNickname(id, player.displayName);
            }

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(player);

            // Cache serverInput for player so that reflection only needs to be used once
            playerInputState[player] = (InputState)serverInputField.GetValue(player);

            // Call covalence hooks
            var iplayer = covalence.PlayerManager.GetPlayer(player.UserIDString);
            Interface.Call("OnUserConnected", iplayer);
            Interface.Call("OnUserInit", iplayer);
        }

        /// <summary>
        /// Called when the player is respawning
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerRespawn")]
        private object OnPlayerRespawn(BasePlayer player)
        {
            // Call covalence hook
            return Interface.Call("OnUserRespawn", covalence.PlayerManager.GetPlayer(player.UserIDString));
        }

        /// <summary>
        /// Called when the player has respawned
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerRespawned")]
        private void OnPlayerRespawned(BasePlayer player)
        {
            // Call covalence hook
            Interface.Call("OnUserRespawned", covalence.PlayerManager.GetPlayer(player.UserIDString));
        }

        /// <summary>
        /// Called when a player tick is received from a client
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerTick")]
        private object OnPlayerTick(BasePlayer player)
        {
            InputState input;
            return playerInputState.TryGetValue(player, out input) ? Interface.Call("OnPlayerInput", player, input) : null;
        }

        /// <summary>
        /// Called when a player attacks something
        /// </summary>
        /// <param name="melee"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerAttack")]
        private object IOnPlayerAttack(BaseMelee melee, HitInfo info)
        {
            var player = melee.GetOwnerPlayer();
            return Interface.Call("OnPlayerAttack", player, info);
        }

        /// <summary>
        /// Called when a BasePlayer is attacked
        /// This is used to call OnEntityTakeDamage for a BasePlayer when attacked
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        [HookMethod("IOnBasePlayerAttacked")]
        private object IOnBasePlayerAttacked(BasePlayer player, HitInfo info)
        {
            if (!serverInitialized || isPlayerTakingDamage) return null;
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
        /// <param name="entity"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HookMethod("IOnBasePlayerHurt")]
        private object IOnBasePlayerHurt(BasePlayer entity, HitInfo info)
        {
            return isPlayerTakingDamage ? null : Interface.Call("OnEntityTakeDamage", entity, info);
        }

        /// <summary>
        /// Called when a player finishes researching an item, before the result is available (success/failure)
        /// </summary>
        /// <param name="table"></param>
        /// <param name="chance"></param>
        /// <returns></returns>
        [HookMethod("IOnItemResearchEnd")]
        private float IOnItemResearchEnd(ResearchTable table, float chance)
        {
            var returnvar = Interface.Call("OnItemResearchEnd", table, chance);
            if (returnvar is float) return (float)returnvar;
            if (returnvar is double) return (float)(double)returnvar;
            return chance;
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

        #region Chat/Console Commands

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        [HookMethod("ConsolePlugins")]
        private void ConsolePlugins(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg.Player())) return;

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
                Reply(arg, "NoPluginsFound");
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loadedPlugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var pluginName in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            arg.ReplyWith(output);
        }

        #endregion

        #region Load Command

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleLoad")]
        private void ConsoleLoad(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs())
            {
                Reply(arg, "CommandUsageLoad");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) continue;
                Interface.Oxide.LoadPlugin(name);
                pluginManager.GetPlugin(name);
            }
        }

        #endregion

        #region Reload Command

        /// <summary>
        /// Called when the "reload" chat command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatReload")]
        private void ChatReload(BasePlayer player, string command, string[] args)
        {
            if (!IsAdmin(player)) return;
            if (args.Length == 0)
            {
                Reply(player, "CommandUsageReload");
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in args)
                if (!string.IsNullOrEmpty(name)) Interface.Oxide.ReloadPlugin(name);
        }

        /// <summary>
        /// Called when the "reload" console command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleReload")]
        private void ConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs())
            {
                Reply(arg, "CommandUsageReload");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
                if (!string.IsNullOrEmpty(name)) Interface.Oxide.ReloadPlugin(name);
        }

        #endregion

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleUnload")]
        private void ConsoleUnload(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs())
            {
                Reply(arg, "CommandUsageUnload");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
                if (!string.IsNullOrEmpty(name)) Interface.Oxide.UnloadPlugin(name);
        }

        #endregion

        #region Version Command

        /// <summary>
        /// Called when the "oxide.version" chat command has been executed
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("ChatVersion")]
        private void ChatVersion(BasePlayer player)
        {
            Reply(player, $"Oxide {OxideMod.Version} for {Title} {BuildInformation.VersionStampDays} ({Protocol.network})");
        }

        /// <summary>
        /// Called when the "oxide.version" console command has been executed
        /// </summary>
        [HookMethod("ConsoleVersion")]
        private void ConsoleVersion(ConsoleSystem.Arg arg)
        {
            Reply(arg, $"Oxide {OxideMod.Version} for {Title} {BuildInformation.VersionStampDays} ({Protocol.network})");
        }

        #endregion

        #region Lang Command

        /// <summary>
        /// Called when the "lang" chat command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatLang")]
        private void ChatLang(BasePlayer player, string command, string[] args)
        {
            if (args != null && args.Length > 0) lang.SetLanguage(args[0], player.UserIDString);
            Reply(player, "PlayerLanguage", lang.GetLanguage(player.UserIDString));
        }

        /// <summary>
        /// Called when the "lang" console command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleLang")]
        private void ConsoleLang(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg.Player())) return;

            if (arg.HasArgs()) lang.SetServerLanguage(arg.GetString(0));
            Reply(arg, "ServerLanguage", lang.GetServerLanguage());
        }

        #endregion

        #region Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleGroup")]
        private void ConsoleGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs(2))
            {
                Reply(arg, "CommandUsageGroup");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    Reply(arg, "GroupAlreadyExists", name);
                    return;
                }
                permission.CreateGroup(name, arg.GetString(2), arg.GetInt(3));
                Reply(arg, "GroupCreated", name);
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(arg, "GroupNotFound", name);
                    return;
                }
                permission.RemoveGroup(name);
                Reply(arg, "GroupDeleted", name);
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(arg, "GroupNotFound", name);
                    return;
                }
                permission.SetGroupTitle(name, arg.GetString(2));
                permission.SetGroupRank(name, arg.GetInt(3));
                Reply(arg, "GroupChanged", name);
            }
            else if (mode.Equals("parent"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(arg, "GroupNotFound", name);
                    return;
                }
                var parent = arg.GetString(2);
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    Reply(arg, "GroupParentNotFound", parent);
                    return;
                }
                if (permission.SetGroupParent(name, parent))
                    Reply(arg, "GroupParentChanged", name, parent);
                else
                    Reply(arg, "GroupParentNotChanged", name);
            }
        }

        #endregion

        #region User Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleUserGroup")]
        private void ConsoleUserGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs(3))
            {
                Reply(arg, "CommandUsageUserGroup");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var group = arg.GetString(2);

            var player = FindPlayer(name);
            if (player == null && !permission.UserIdValid(name))
            {
                Reply(arg, "UserNotFound", name);
                return;
            }
            var userId = name;
            if (player != null)
            {
                userId = player.UserIDString;
                name = player.displayName;
                permission.UpdateNickname(userId, name);
                name += $"({userId})";
            }

            if (!permission.GroupExists(group))
            {
                Reply(arg, "GroupNotFound", name);
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                Reply(arg, "UserAddedToGroup", name, group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                Reply(arg, "UserRemovedFromGroup", name, group);
            }
        }

        #endregion

        #region Grant Command

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleGrant")]
        private void ConsoleGrant(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs(3))
            {
                Reply(arg, "CommandUsageGrant");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (!permission.PermissionExists(perm))
            {
                Reply(arg, "PermissionNotFound", perm);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(arg, "GroupNotFound", name);
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                Reply(arg, "GroupPermissionGranted", name, perm);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserIdValid(name))
                {
                    Reply(arg, "UserNotFound", name);
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.UserIDString;
                    name = player.displayName;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                Reply(arg, "UserPermissionGranted", $"{name} ({userId})", perm);
            }
        }

        #endregion

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleRevoke")]
        private void ConsoleRevoke(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs(3))
            {
                Reply(arg, "CommandUsageRevoke");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (!permission.PermissionExists(perm))
            {
                Reply(arg, "PermissionNotFound", perm);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(arg, "GroupNotFound", name);
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                Reply(arg, "GroupPermissionRevoked", name, perm);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserIdValid(name))
                {
                    Reply(arg, "UserNotFound", name);
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.UserIDString;
                    name = player.displayName;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                Reply(arg, "UserPermissionRevoked", $"{name} ({userId})", perm);
            }
        }

        #endregion

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleShow")]
        private void ConsoleShow(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg.Player())) return;
            if (!arg.HasArgs())
            {
                Reply(arg, "CommandUsageShow");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);

            if (mode.Equals("perms"))
            {
                arg.ReplyWith("Permissions:\n" + string.Join(", ", permission.GetPermissions()));
            }
            else if (mode.Equals("perm"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(arg, "CommandUsageShow");
                    return;
                }

                var result = $"Permission '{name}' Users:\n";
                result += string.Join(", ", permission.GetPermissionUsers(name));
                result += $"\nPermission '{name}' Groups:\n";
                result += string.Join(", ", permission.GetPermissionGroups(name));
                arg.ReplyWith(result);
            }
            else if (mode.Equals("user"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(arg, "CommandUsageShow");
                    return;
                }

                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(arg, "UserNotFound", name);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.UserIDString;
                    name = target.displayName;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }
                var result = $"User '{name}' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += $"\nUser '{name}' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                arg.ReplyWith(result);
            }
            else if (mode.Equals("group"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(arg, "CommandUsageShow");
                    return;
                }

                if (!permission.GroupExists(name))
                {
                    Reply(arg, "GroupNotFound", name);
                    return;
                }

                var result = $"Group '{name}' users:\n";
                result += string.Join(", ", permission.GetUsersInGroup(name));
                result += $"\nGroup '{name}' permissions:\n";
                result += string.Join(", ", permission.GetGroupPermissions(name));
                var parent = permission.GetGroupParent(name);
                while (permission.GroupExists(parent))
                {
                    result += $"\nParent group '{parent}' permissions:\n";
                    result += string.Join(", ", permission.GetGroupPermissions(parent));
                    parent = permission.GetGroupParent(parent);
                }
                arg.ReplyWith(result);
            }
            else if (mode.Equals("groups"))
            {
                arg.ReplyWith("Groups:\n" + string.Join(", ", permission.GetGroups()));
            }
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
            if (arg.cmd.namefull != "chat.say") return null;

            // Get the args
            var str = arg.GetString(0, "text");
            if (str.Length == 0) return null;

            // Is it a chat command?
            if (str[0] != '/') return null;

            // Get the full command
            var message = str.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(message, out cmd, out args);
            if (cmd == null) return null;

            // Get the covalence player
            var iplayer = covalence.PlayerManager.GetConnectedPlayer(arg.connection.userid.ToString());

            // Is the command blocked?
            var blockedSpecific = Interface.Call("OnPlayerCommand", arg);
            var blockedCovalence = Interface.Call("OnUserCommand", iplayer, cmd, args);

            if (blockedSpecific != null || blockedCovalence != null) return true;

            // Is it a covalance command?
            if (covalence.CommandSystem.HandleChatMessage(iplayer, str)) return true;
            
            // It is a regular chat command
            // Handle it
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                Interface.Oxide.LogDebug("Player is actually a {0}!", arg.connection.player.GetType());
            else if (!cmdlib.HandleChatCommand(player, cmd, args))
                Reply(player, "UnknownCommand", cmd);
            
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
        /// Returns if specified player is admin
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private static bool IsAdmin(BasePlayer player) => player == null || player.net.connection.authLevel >= 2;

        /// <summary>
        /// Replies to the player with a specific message
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="key"></param>
        /// <param name="args"></param>
        private void Reply(ConsoleSystem.Arg arg, string key, params object[] args)
        {
            arg.ReplyWith(string.Format(lang.GetMessage(key, this, arg.connection?.userid.ToString()), args));
        }

        /// <summary>
        /// Replies to the player with a specific message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="key"></param>
        /// <param name="args"></param>
        private void Reply(BasePlayer player, string key, params object[] args)
        {
            player.ChatMessage(string.Format(lang.GetMessage(key, this, player.UserIDString), args));
        }

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
