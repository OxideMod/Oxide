﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Steamworks;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Hurtworld.Libraries;
using Oxide.Game.Hurtworld.Libraries.Covalence;

namespace Oxide.Game.Hurtworld
{
    /// <summary>
    /// The core Hurtworld plugin
    /// </summary>
    public class HurtworldCore : CSPlugin
    {
        #region Initialization

        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The Hurtworld covalence provider
        private readonly HurtworldCovalenceProvider covalence = HurtworldCovalenceProvider.Instance;

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
        private readonly Dictionary<string, PlayerSession> loadingPlugins = new Dictionary<string, PlayerSession>();

        /// <summary>
        /// Initializes a new instance of the HurtworldCore class
        /// </summary>
        public HurtworldCore()
        {
            // Set attributes
            Name = "HurtworldCore";
            Title = "Hurtworld";
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
        private bool PermissionsLoaded(PlayerSession session)
        {
            if (permission.IsLoaded) return true;
            Reply(Lang("PermissionsNotLoaded", permission.LastException.Message), session);
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
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("version", GameManager.Instance.Version.ToString());

            // Register messages for localization
            lang.RegisterMessages(messages, this);

            // Add general commands
            //cmdlib.AddChatCommand("oxide.plugins", this, "ChatPlugins");
            //cmdlib.AddChatCommand("plugins", this, "ChatPlugins");
            cmdlib.AddChatCommand("oxide.load", this, "ChatLoad");
            cmdlib.AddChatCommand("load", this, "ChatLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "ChatUnload");
            cmdlib.AddChatCommand("unload", this, "ChatUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "ChatReload");
            cmdlib.AddChatCommand("reload", this, "ChatReload");
            cmdlib.AddChatCommand("oxide.version", this, "ChatChatVersion");
            cmdlib.AddConsoleCommand("oxide.version", this, "ConsoleVersion");
            cmdlib.AddChatCommand("version", this, "ChatVersion");
            cmdlib.AddConsoleCommand("version", this, "ConsoleVersion");

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
            SteamGameServer.SetGameTags("oxide,modded");

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", GameManager.Instance.ServerConfig.GameName);

            // Update server console window and status bars
            HurtworldExtension.ServerConsole();
        }

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(PlayerSession session)
        {
            session.Name = session.Identity.Name ?? "Unnamed";
            var id = session.SteamId.ToString();
            var ip = session.Player.ipAddress;

            // Call out and see if we should reject
            var canLogin = Interface.Call("CanClientLogin", session) ?? Interface.Call("CanUserLogin", session.Name, id, ip);
            if (canLogin != null && (!(canLogin is bool) || !(bool)canLogin))
            {
                // Reject the user with the message
                GameManager.Instance.KickPlayer(id, canLogin.ToString());
                return true;
            }

            return Interface.Call("OnUserApprove", session) ?? Interface.Call("OnUserApproved", session.Name, id, ip);
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(PlayerSession session, string message)
        {
            if (message.Trim().Length <= 1) return true;
            var str = message.Substring(0, 1);

            // Get covalence player
            var iplayer = covalence.PlayerManager.GetPlayer(session.SteamId.ToString());

            // Is it a chat command?
            if (!str.Equals("/"))
                return Interface.Call("OnPlayerChat", session, message) ?? Interface.Call("OnUserChat", iplayer, message);

            // Is this a covalence command?
            if (covalence.CommandSystem.HandleChatMessage(iplayer, message)) return true;

            // Get the command string
            var command = message.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Handle it
            if (!cmdlib.HandleChatCommand(session, cmd, args))
            {
                Reply(Lang("UnknownCommand", session.SteamId.ToString(), cmd), session);
                return true;
            }

            Interface.Call("OnChatCommand", session, command);

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="name"></param>
        /// <param name="player"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(string name, uLink.NetworkPlayer player)
        {
            // Set the session name and strip HTML tags
            var session = FindSessionByNetPlayer(player);
            session.Name = Regex.Replace(name, "<.*?>", string.Empty); // TODO: Make sure the name is not blank

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(session);

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var id = session.SteamId.ToString();
                permission.UpdateNickname(id, session.Name);

                // Add player to default group
                if (!permission.UserHasGroup(id, DefaultGroups[0])) permission.AddUserGroup(id, DefaultGroups[0]);

                // Add player to admin group if admin
                if (session.IsAdmin && !permission.UserHasGroup(id, DefaultGroups[2])) permission.AddUserGroup(id, DefaultGroups[2]);
            }

            Interface.Call("OnPlayerConnected", session);

            // Call covalence hook
            Interface.Call("OnUserConnected", covalence.PlayerManager.GetPlayer(session.SteamId.ToString()));
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="session"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(PlayerSession session)
        {
            // Call covalence hook
            Interface.Call("OnUserDisconnected", covalence.PlayerManager.GetPlayer(session.SteamId.ToString()), "Unknown");

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerDisconnect(session);
        }

        /// <summary>
        /// Called when the player has been initialized
        /// </summary>
        /// <param name="session"></param>
        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(PlayerSession session)
        {
            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(session);

            // Call covalence hook
            Interface.Call("OnUserInit", covalence.PlayerManager.GetPlayer(session.SteamId.ToString()));
        }

        /// <summary>
        /// Called when the server receives input from a player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="input"></param>
        [HookMethod("IOnPlayerInput")]
        private void IOnPlayerInput(uLink.NetworkPlayer player, InputControls input) => Interface.Call("OnPlayerInput", FindSessionByNetPlayer(player), input);

        /// <summary>
        /// Called when the player attempts to suicide
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("IOnPlayerSuicide")]
        private object IOnPlayerSuicide(uLink.NetworkPlayer player) => Interface.Call("OnPlayerSuicide", FindSessionByNetPlayer(player));

        #endregion

        #region Structure Hooks

        /// <summary>
        /// Called when a single door is used
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        [HookMethod("IOnSingleDoorUsed")]
        private void IOnSingleDoorUsed(DoorSingleServer door)
        {
            var player = door.LastUsedBy;
            if (player == null) return;

            Interface.Call("OnSingleDoorUsed", door, GameManager.Instance.GetSession((uLink.NetworkPlayer)player));
        }


        /// <summary>
        /// Called when a double door is used
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        [HookMethod("IOnDoubleDoorUsed")]
        private void IOnDoubleDoorUsed(DoubleDoorServer door)
        {
            var player = door.LastUsedBy;
            if (player == null) return;

            Interface.Call("OnDoubleDoorUsed", door, GameManager.Instance.GetSession((uLink.NetworkPlayer)player));
        }

        /// <summary>
        /// Called when a garage door is used
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        [HookMethod("IOnGarageDoorUsed")]
        private void IOnGarageDoorUsed(GarageDoorServer door)
        {
            var player = door.LastUsedBy;
            if (player == null) return;

            Interface.Call("OnGarageDoorUsed", door, GameManager.Instance.GetSession((uLink.NetworkPlayer)player));
        }

        #endregion

        #region Vehicle Hooks

        /// <summary>
        /// Called when a player tries to enter a vehicle
        /// </summary>
        /// <param name="passenger"></param>
        /// <returns></returns>
        [HookMethod("ICanEnterVehicle")]
        private object ICanEnterVehicle(CharacterMotorSimple passenger) => Interface.Call("CanEnterVehicle", FindSessionByNetPlayer(passenger.networkView.owner), passenger);

        /// <summary>
        /// Called when a player tries to exit a vehicle
        /// </summary>
        /// <param name="passenger"></param>
        /// <returns></returns>
        [HookMethod("ICanExitVehicle")]
        private object ICanExitVehicle(CharacterMotorSimple passenger) => Interface.Call("CanExitVehicle", FindSessionByNetPlayer(passenger.networkView.owner), passenger);

        /// <summary>
        /// Called when a player enters a vehicle
        /// </summary>
        /// <param name="passenger"></param>
        /// <returns></returns>
        [HookMethod("IOnEnterVehicle")]
        private object IOnEnterVehicle(CharacterMotorSimple passenger) => Interface.Call("OnEnterVehicle", FindSessionByNetPlayer(passenger.networkView.owner), passenger);

        /// <summary>
        /// Called when a player exits a vehicle
        /// </summary>
        /// <param name="passenger"></param>
        /// <returns></returns>
        [HookMethod("IOnExitVehicle")]
        private object IOnExitVehicle(CharacterMotorSimple passenger) => Interface.Call("OnExitVehicle", FindSessionByNetPlayer(passenger.networkView.owner), passenger);

        #endregion

        #region Chat/Console Commands

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatPlugins")]
        private void ChatPlugins(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;

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
                Reply(Lang("NoPluginsFound", session.SteamId.ToString()), session);
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loadedPlugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var pluginName in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            Reply(output, session);
        }

        #endregion

        #region Load Command

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatLoad")]
        private void ChatLoad(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageLoad", session.SteamId.ToString()), session);
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
                if (!loadingPlugins.ContainsKey(name)) loadingPlugins.Add(name, session);
            }
        }

        #endregion

        #region Reload Command

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatReload")]
        private void ChatReload(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageReload", session.SteamId.ToString()), session);
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
                    Reply(Lang("PluginNotLoaded", session.SteamId.ToString(), name), session);
                    continue;
                }
                Interface.Oxide.ReloadPlugin(name);
                Reply(Lang("PluginReloaded", session.SteamId.ToString(), plugin.Title, plugin.Version, plugin.Author), session);
            }
        }

        #endregion

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatUnload")]
        private void ChatUnload(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageUnload", session.SteamId.ToString()), session);
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
                    Reply(Lang("PluginNotLoaded", session.SteamId.ToString(), name), session);
                    continue;
                }
                Interface.Oxide.UnloadPlugin(name);
                Reply(Lang("PluginUnloaded", session.SteamId.ToString(), plugin.Title, plugin.Version, plugin.Author), session);
            }
        }

        #endregion

        #region Version Command

        /// <summary>
        /// Called when the "version" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        [HookMethod("ChatVersion")]
        private void ChatVersion(PlayerSession session)
        {
            Reply($"Oxide {OxideMod.Version} for {Title} {GameManager.Instance.Version} ({GameManager.PROTOCOL_VERSION})", session);
        }

        /// <summary>
        /// Called when the "version" console command has been executed
        /// </summary>
        [HookMethod("ConsoleVersion")]
        private void ConsoleVersion() => ChatVersion(null);

        #endregion

        #region Group Command

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatGroup")]
        private void ChatGroup(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 2)
            {
                Reply(Lang("CommandUsageGroup", session.SteamId.ToString()), session);
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
                    Reply(Lang("GroupAlreadyExists", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.CreateGroup(name, title, rank);
                Reply(Lang("GroupCreated", session.SteamId.ToString(), name), session);
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.RemoveGroup(name);
                Reply(Lang("GroupDeleted", session.SteamId.ToString(), name), session);
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                Reply(Lang("GroupChanged", session.SteamId.ToString(), name), session);
            }
            else if (mode.Equals("parent"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                var parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    Reply(Lang("GroupParentNotFound", session.SteamId.ToString(), parent), session);
                    return;
                }
                if (permission.SetGroupParent(name, parent))
                    Reply(Lang("GroupParentChanged", session.SteamId.ToString(), name, parent), session);
                else
                    Reply(Lang("GroupParentNotChanged", session.SteamId.ToString(), name), session);
            }
        }

        #endregion

        #region Usergroup Command

        /// <summary>
        /// Called when the "usergroup" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatUserGroup")]
        private void ChatUserGroup(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageUserGroup", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindSession(name);
            if (target == null && !permission.UserIdValid(name))
            {
                Reply(Lang("UserNotFound", session.SteamId.ToString(), name), session);
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.SteamId.ToString();
                name = target.Name;
                permission.UpdateNickname(userId, name);
            }

            if (!permission.GroupExists(group))
            {
                Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                Reply(Lang("UserAddedToGroup", session.SteamId.ToString(), name, group), session);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                Reply(Lang("UserRemovedFromGroup", session.SteamId.ToString(), name, group), session);
            }
        }

        #endregion

        #region Grant Command

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatGrant")]
        private void ChatGrant(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageGrant", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(Lang("PermissionNotFound", session.SteamId.ToString(), perm), session);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                Reply(Lang("GroupPermissionGranted", session.SteamId.ToString(), name, perm), session);
            }
            else if (mode.Equals("user"))
            {
                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                Reply(Lang("UserPermissionGranted", session.SteamId.ToString(), $"{name} ({userId})", perm), session);
            }
        }

        #endregion

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatRevoke")]
        private void ChatRevoke(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageRevoke", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(Lang("PermissionNotFound", session.SteamId.ToString(), perm), session);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                Reply(Lang("GroupPermissionRevoked", session.SteamId.ToString(), name, perm), session);
            }
            else if (mode.Equals("user"))
            {
                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                Reply(Lang("UserPermissionRevoked", session.SteamId.ToString(), $"{name} ({userId})", perm), session);
            }
        }

        #endregion

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatShow")]
        private void ChatShow(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session)) return;
            if (!IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageShow", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args.Length > 1 ? args[1] : string.Empty;

            if (mode.Equals("perms"))
            {
                var result = "Permissions:\n";
                result += string.Join(", ", permission.GetPermissions());
                Reply(result, session);
            }
            else if (mode.Equals("user"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(Lang("CommandUsageShow", session.SteamId.ToString()), session);
                    return;
                }

                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", session.SteamId.ToString()), session);
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }
                var result = $"User '{name}' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += $"\nUser '{name}' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                Reply(result, session);
            }
            else if (mode.Equals("group"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(Lang("CommandUsageShow", session.SteamId.ToString()), session);
                    return;
                }

                if (!permission.GroupExists(name) && !string.IsNullOrEmpty(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
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
                Reply(result, session);
            }
            else if (mode.Equals("groups"))
            {
                Reply(Lang("ShowGroups", session.SteamId.ToString(), "\n" + string.Join(", ", permission.GetGroups())), session);
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
        private object OnServerCommand(string arg)
        {
            if (arg == null || arg.Trim().Length == 0) return null;

            // Is this a covalence command?
            if (covalence.CommandSystem.HandleConsoleMessage(covalence.CommandSystem.consolePlayer, arg)) return true;

            return cmdlib.HandleConsoleCommand(arg);
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
        /// <param name="session"></param>
        /// <returns></returns>
        private bool IsAdmin(PlayerSession session)
        {
            if (session == null || session.IsAdmin) return true;
            Reply(Lang("YouAreNotAdmin", session.SteamId.ToString()), session);
            return false;
        }

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="session"></param>
        /// <param name="args"></param>
        private static void Reply(string message, PlayerSession session = null, params object[] args)
        {
            if (session == null)
            {
                Interface.Oxide.LogInfo(message, args);
                return;
            }
            ChatManagerServer.Instance.RPC("RelayChat", session.Player, string.Format(message, args));
        }

        /// <summary>
        /// Returns the localized message from key using optional ID string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        /// <summary>
        /// Gets the player session using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public PlayerSession FindSession(string nameOrIdOrIp)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;
            foreach (var i in sessions)
            {
                if (!nameOrIdOrIp.Equals(i.Value.Name, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(i.Value.SteamId.ToString()) && !nameOrIdOrIp.Equals(i.Key.ipAddress)) continue;
                session = i.Value;
                break;
            }
            return session;
        }

        /// <summary>
        /// Gets the player session using a uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerSession FindSessionByNetPlayer(uLink.NetworkPlayer player) => GameManager.Instance.GetSession(player);

        /// <summary>
        /// Gets the player session using a UnityEngine.GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public PlayerSession FindSessionByGo(GameObject go)
        {
            var sessions = GameManager.Instance.GetSessions();
            return (from i in sessions where go.Equals(i.Value.WorldPlayerEntity) select i.Value).FirstOrDefault();
        }

        #endregion
    }
}
