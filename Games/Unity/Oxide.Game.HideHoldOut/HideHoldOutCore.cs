using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using uLink;
using UnityEngine;
using Network = uLink.Network;
using NetworkConnectionError = uLink.NetworkConnectionError;
using NetworkPlayer = uLink.NetworkPlayer;
using NetworkView = uLink.NetworkView;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Game.HideHoldOut.Libraries;
using Oxide.Game.HideHoldOut.Libraries.Covalence;

namespace Oxide.Game.HideHoldOut
{
    /// <summary>
    /// The core Hide & Hold Out plugin
    /// </summary>
    public class HideHoldOutCore : CSPlugin
    {
        #region Setup

        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The Rust covalence provider
        private readonly HideHoldOutCovalenceProvider covalence = HideHoldOutCovalenceProvider.Instance;

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
            {"ShowGroups", "Groups: {0}"},
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
        private bool loggingInitialized;

        // Track 'load' chat commands
        private readonly Dictionary<string, PlayerInfos> loadingPlugins = new Dictionary<string, PlayerInfos>();

        // Get ChatManager NetworkView
        private static readonly FieldInfo ChatNetViewField = typeof(ChatManager).GetField("Chat_NetView", BindingFlags.NonPublic | BindingFlags.Instance);
        public static NetworkView ChatNetView = ChatNetViewField.GetValue(NetworkController.NetManager_.chatManager) as NetworkView;

        #endregion

        #region Initialization

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

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(PlayerInfos player)
        {
            if (permission.IsLoaded) return true;
            Reply(string.Format(Lang("PermissionsNotLoaded", permission.LastException.Message)), player);
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
            // Configure remote logging
            RemoteLogger.SetTag("game", "hide & hold out");
            RemoteLogger.SetTag("version", NetworkController.NetManager_.get_GAME_VERSION);

            // Register messages for localization
            lang.RegisterMessages(messages, this);

            // Add general commands
            //cmdlib.AddChatCommand("oxide.plugins", this, "CmdPlugins");
            //cmdlib.AddChatCommand("plugins", this, "CmdPlugins");
            cmdlib.AddChatCommand("oxide.load", this, "ChatLoad");
            cmdlib.AddChatCommand("load", this, "ChatLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "ChatUnload");
            cmdlib.AddChatCommand("unload", this, "ChatUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "ChatReload");
            cmdlib.AddChatCommand("reload", this, "ChatReload");
            cmdlib.AddChatCommand("oxide.version", this, "ChatVersion");
            cmdlib.AddChatCommand("version", this, "ChatVersion");
            cmdlib.AddConsoleCommand("oxide.version", this, "ConsoleVersion");
            cmdlib.AddConsoleCommand("version", this, "ConsoleVersion");
            cmdlib.AddConsoleCommand("quit", this, "ConsoleQuit");
            cmdlib.AddConsoleCommand("shutdown", this, "ConsoleQuit");

            // Add permission commands
            cmdlib.AddChatCommand("oxide.group", this, "ChatGroup");
            cmdlib.AddChatCommand("group", this, "ChatGroup");
            cmdlib.AddChatCommand("oxide.usergroup", this, "ChatUserGroup");
            cmdlib.AddChatCommand("usergroup", this, "ChatUserGroup");
            cmdlib.AddChatCommand("oxide.grant", this, "ChatGrant");
            cmdlib.AddChatCommand("grant", this, "ChatGrant");
            cmdlib.AddChatCommand("oxide.revoke", this, "ChatRevoke");
            cmdlib.AddChatCommand("revoke", this, "ChatRevoke");
            cmdlib.AddChatCommand("oxide.show", this, "ChatShow");
            cmdlib.AddChatCommand("show", this, "ChatShow");

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

            if (!loadingPlugins.ContainsKey(plugin.Name)) return;
            Reply($"Loaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            loadingPlugins.Remove(plugin.Name);
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

            // Add 'oxide' and 'modded' tags
            //SteamGameServer.SetGameTags("oxide,modded");

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
        /// <param name="id"></param>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(NetworkPlayerApproval approval, string id)
        {
            var player = NetworkController.NetManager_.ServManager.GetPlayerInfos_accountID(id);

            // Call out and see if we should reject
            var canlogin = Interface.CallHook("CanClientLogin", approval, player) ?? Interface.CallHook("CanUserLogin", player.Nickname, id);
            if (canlogin is NetworkConnectionError)
            {
                approval.Deny((NetworkConnectionError)canlogin);
                return true;
            }

            return Interface.CallHook("OnUserApprove", approval, player) ?? Interface.CallHook("OnUserApproved", player.Nickname, id);
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="netPlayer"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(NetworkPlayer netPlayer, string message)
        {
            if (message.Trim().Length <= 1) return true;
            var str = message.Substring(0, 1);
            var player = FindPlayerByNetPlayer(netPlayer);

            // Get covalence player
            var iplayer = covalence.PlayerManager.GetPlayer(player.account_id);

            // Is it a chat command?
            if (!str.Equals("/") && !str.Equals("!"))
                return Interface.CallHook("OnPlayerChat", player, message) ?? Interface.CallHook("OnUserChat", iplayer, message);

            // Is this a covalence command?
            var livePlayer = covalence.PlayerManager.GetOnlinePlayer(player.account_id);
            if (covalence.CommandSystem.HandleChatMessage(livePlayer, message)) return true;

            // Get the command string
            var command = message.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Handle it
            if (!cmdlib.HandleChatCommand(player, cmd, args))
            {
                Reply(string.Format(Lang("UnknownCommand", player.account_id), cmd), player);
                return true;
            }

            Interface.CallHook("OnChatCommand", player, command);

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(PlayerInfos player)
        {
            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(player);

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var userId = player.account_id;
                permission.UpdateNickname(userId, player.Nickname);

                // Add player to default group
                if (!permission.UserHasAnyGroup(userId)) permission.AddUserGroup(userId, DefaultGroups[0]);
            }

            // Call covalence hook
            var iplayer = covalence.PlayerManager.GetPlayer(player.account_id);
            Interface.CallHook("OnUserConnected", iplayer);

            Debug.Log($"{player.account_id}/{player.Nickname} joined");
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(PlayerInfos player)
        {
            // Call covalence hook
            var iplayer = covalence.PlayerManager.GetPlayer(player.account_id);
            Interface.CallHook("OnUserDisconnected", iplayer, null);

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerDisconnect(player);

            Debug.Log($"{player.account_id}/{player.Nickname} quit");
        }

        #endregion

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatPlugins")]
        private void ChatPlugins(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;

            var loadedPlugins = pluginmanager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
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
                Reply(Lang("NoPluginsFound", player.account_id), player);
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loadedPlugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var plugin_name in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {plugin_name} - {unloadedPluginErrors[plugin_name]}";
            Reply(output, player);
        }

        #endregion

        #region Load Command

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatLoad")]
        private void ChatLoad(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageLoad", player.account_id), player);
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name) || !Interface.Oxide.LoadPlugin(name)) continue;
                if (!loadingPlugins.ContainsKey(name)) loadingPlugins.Add(name, player);
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
        [HookMethod("ChatReload")]
        private void ChatReload(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageReload", player.account_id), player);
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    Reply(string.Format(Lang("PluginNotLoaded", player.account_id), name), player);
                    continue;
                }
                Interface.Oxide.ReloadPlugin(name);
                Reply(string.Format(Lang("PluginReloaded", player.account_id), plugin.Title, plugin.Version, plugin.Author), player);
            }
        }

        #endregion

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatUnload")]
        private void ChatUnload(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageUnload", player.account_id), player);
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    Reply(string.Format(Lang("PluginNotLoaded", player.account_id), name), player);
                    continue;
                }
                Interface.Oxide.UnloadPlugin(name);
                Reply(string.Format(Lang("PluginUnloaded", player.account_id), plugin.Title, plugin.Version, plugin.Author), player);
            }
        }

        #endregion

        #region Version Command

        /// <summary>
        /// Called when the "version" chat command has been executed
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("ChatVersion")]
        private void ChatVersion(PlayerInfos player) => Reply($"Oxide {OxideMod.Version} for {NetworkController.NetManager_.get_GAME_VERSION}", player);

        /// <summary>
        /// Called when the "version" console command has been executed
        /// </summary>
        [HookMethod("ConsoleVersion")]
        private void ConsoleVersion() => ChatVersion(null);

        #endregion

        #region Quit Command

        /// <summary>
        /// Called when the "quit" console command has been executed
        /// </summary>
        [HookMethod("ConsoleQuit")]
        private void ConsoleQuit()
        {
            NetworkController.isEXITING = true;
            uLink.MasterServer.UnregisterHost();
            NetworkController.NetManager_.ServManager.Invoke("DataBase_Storing", 0f);
            Application.Quit();
        }

        #endregion

        #region Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatGroup")]
        private void ChatGroup(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 2)
            {
                Reply(Lang("CommandUsageGroup", player.account_id), player);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var title = args.Length > 2 ? args[2] : string.Empty;
            int rank;
            if (args.Length < 4 || !int.TryParse(args[3], out rank))
                rank = 0;

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupAlreadyExists", player.account_id), name), player);
                    return;
                }
                permission.CreateGroup(name, title, rank);
                Reply(string.Format(Lang("GroupCreated", player.account_id), name), player);
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
                    return;
                }
                permission.RemoveGroup(name);
                Reply(string.Format(Lang("GroupDeleted", player.account_id), name), player);
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                Reply(string.Format(Lang("GroupChanged", player.account_id), name), player);
            }
            else if (mode.Equals("parent"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
                    return;
                }
                var parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    Reply(string.Format(Lang("GroupParentNotFound", player.account_id), parent), player);
                    return;
                }
                if (permission.SetGroupParent(name, parent))
                    Reply(string.Format(Lang("GroupParentChanged", player.account_id), name, parent), player);
                else
                    Reply(string.Format(Lang("GroupParentNotChanged", player.account_id), name), player);
            }
        }

        #endregion

        #region Usergroup Command

        /// <summary>
        /// Called when the "usergroup" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatUserGroup")]
        private void ChatUserGroup(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageUserGroup", player.account_id), player);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindPlayer(name);
            if (target == null && !permission.UserIdValid(name))
            {
                Reply(string.Format(Lang("UserNotFound", player.account_id), name), player);
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.account_id;
                name = target.Nickname;
                permission.UpdateNickname(userId, name);
            }

            if (!permission.GroupExists(group))
            {
                Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                Reply(string.Format(Lang("UserAddedToGroup", player.account_id), name, group), player);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                Reply(string.Format(Lang("UserRemovedFromGroup", player.account_id), name, group), player);
            }
        }

        #endregion

        #region Grant Command

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatGrant")]
        private void ChatGrant(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageGrant", player.account_id), player);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(string.Format(Lang("PermissionNotFound", player.account_id), perm), player);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                Reply(string.Format(Lang("GroupPermissionGranted", player.account_id), name, perm), player);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(string.Format(Lang("UserNotFound", player.account_id), name), player);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.account_id;
                    name = target.Nickname;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                Reply(string.Format(Lang("UserPermissionGranted", player.account_id), $"{name} ({userId})", perm), player);
            }
        }

        #endregion

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatShow")]
        private void ChatShow(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageShow", player.account_id), player);
                return;
            }

            var mode = args[0];
            var name = args.Length > 1 ? args[1] : string.Empty;

            if (mode.Equals("perms"))
            {
                var result = "Permissions:\n";
                result += string.Join(", ", permission.GetPermissions());
                Reply(result, player);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", player.account_id), player);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.account_id;
                    name = target.Nickname;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }
                var result = $"User '{name}' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += $"\nUser '{name}' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                Reply(result, player);
            }
            else if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
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
                Reply(result, player);
            }
            else if (mode.Equals("groups"))
            {
                Reply(string.Format(Lang("ShowGroups", player.account_id), "\n" + string.Join(", ", permission.GetGroups())), player);
            }
        }

        #endregion

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatRevoke")]
        private void ChatRevoke(PlayerInfos player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!IsAdmin(player)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageRevoke", player.account_id), player);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(string.Format(Lang("PermissionNotFound", player.account_id), perm), player);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(string.Format(Lang("GroupNotFound", player.account_id), name), player);
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                Reply(string.Format(Lang("GroupPermissionRevoked", player.account_id), name, perm), player);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(string.Format(Lang("UserNotFound", player.account_id), name), player);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.account_id;
                    name = target.Nickname;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                Reply(string.Format(Lang("UserPermissionRevoked", player.account_id), $"{name} ({userId})", perm), player);
            }
        }

        #endregion

        #region Command Handling

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnServerCommand")]
        private object OnServerCommand(string arg) => arg == null || arg.Trim().Length == 0 ? null : cmdlib.HandleConsoleCommand(arg);

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
                        sb = new StringBuilder();
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
                    sb = new StringBuilder();
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
        private bool IsAdmin(PlayerInfos player)
        {
            if (player == null || player.isADMIN) return true;
            Reply(Lang("YouAreNotAdmin", player.account_id), player);
            return false;
        }

        /// <summary>
        /// Replies to the player with a specific message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        private static void Reply(string message, PlayerInfos player = null)
        {
            if (player == null)
            {
                Interface.Oxide.LogInfo(message);
                return;
            }
            ChatNetView.RPC("NET_Receive_msg", player.NetPlayer, "\n" + message, chat_msg_type.standard, player.account_id);
        }

        /// <summary>
        /// Returns the localized message from key using optional ID string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        string Lang(string key, string id = null) => lang.GetMessage(key, this, id);

        /// <summary>
        /// Returns the PlayerInfos for the specified name, ID, or IP address string
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public static PlayerInfos FindPlayer(string nameOrIdOrIp)
        {
            var server = NetworkController.NetManager_.ServManager;
            var player = server.GetPlayerInfos_nickname(nameOrIdOrIp);
            if (player == null)
            {
                ulong id;
                if (ulong.TryParse(nameOrIdOrIp, out id)) player = server.GetPlayerInfos_accountID(id.ToString());
            }
            if (player == null)
            {
                foreach (var target in Network.connections)
                    if (target.ipAddress == nameOrIdOrIp) player = server.GetPlayerInfos_nickname(target.loginData.ReadString());
            }
            return player;
        }

        /// <summary>
        /// Returns the PlayerInfos for the specified ID ulong
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PlayerInfos FindPlayerById(ulong id) => FindPlayer(id.ToString());

        /// <summary>
        /// Returns the PlayerInfos for the specified ID string
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PlayerInfos FindPlayerByIdString(string id) => FindPlayer(id);

        /// <summary>
        /// Returns the PlayerInfos for the specified uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerInfos FindPlayerByNetPlayer(NetworkPlayer player) => NetworkController.NetManager_.ServManager.GetPlayerInfos(player);

        #endregion
    }
}
