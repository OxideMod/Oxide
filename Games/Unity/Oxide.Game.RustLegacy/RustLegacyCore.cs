using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.RustLegacy.Libraries;
using Oxide.Game.RustLegacy.Libraries.Covalence;
using Rust;
using uLink;
using UnityEngine;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// The core Rust Legacy plugin
    /// </summary>
    public class RustLegacyCore : CSPlugin
    {
        #region Initialization

        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The Rust Legacy covalence provider
        private readonly RustLegacyCovalenceProvider covalence = RustLegacyCovalenceProvider.Instance;

        // Track when the server has been initialized
        private bool serverInitialized;

        // Cache some player information
        private static readonly Dictionary<NetUser, PlayerData> playerData = new Dictionary<NetUser, PlayerData>();

        public class PlayerData
        {
            public Character character;
            public PlayerInventory inventory;
        }

        // Last Metabolism hacker notification time
        float lastWarningAt;

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            "rcon.login",
            "rcon.password"
        };

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustLegacyCore()
        {
            var assemblyVersion = RustLegacyExtension.AssemblyVersion;

            // Set attributes
            Name = "RustLegacyCore";
            Title = "Rust Legacy";
            Author = "Oxide Team";
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(ConsoleSystem.Arg arg)
        {
            if (permission.IsLoaded) return true;
            arg.ReplyWith("Unable to load permission files! Permissions will not work until resolved.\r\n => " + permission.LastException.Message);
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
            RemoteLogger.SetTag("game version", Rust.Defines.Connection.protocol.ToString());

            // Add general commands
            cmdlib.AddConsoleCommand("oxide.plugins", this, "ConsolePlugins");
            cmdlib.AddConsoleCommand("global.plugins", this, "ConsolePlugins");
            cmdlib.AddConsoleCommand("oxide.load", this, "ConsoleLoad");
            cmdlib.AddConsoleCommand("global.load", this, "ConsoleLoad");
            cmdlib.AddConsoleCommand("oxide.unload", this, "ConsoleUnload");
            cmdlib.AddConsoleCommand("global.unload", this, "ConsoleUnload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "ConsoleReload");
            cmdlib.AddConsoleCommand("global.reload", this, "ConsoleReload");
            cmdlib.AddConsoleCommand("oxide.version", this, "ConsoleVersion");
            cmdlib.AddConsoleCommand("global.version", this, "ConsoleVersion");

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

            Analytics.Collect();

            // Update server console window and status bars
            RustLegacyExtension.ServerConsole();
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
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="approval"></param>
        /// <param name="acceptor"></param>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(ClientConnection connection, NetworkPlayerApproval approval, ConnectionAcceptor acceptor)
        {
            // Reject invalid connections
            if (connection.UserID == 0 || string.IsNullOrEmpty(connection.UserName))
            {
                approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                return true;
            }

            var id = connection.UserID.ToString();
            var ip = approval.ipAddress;

            // Call out and see if we should reject
            var loginSpecific = Interface.Call("CanClientLogin", connection);
            var loginCovalence = Interface.Call("CanUserLogin", connection.UserName, id, ip);
            var canLogin = loginSpecific ?? loginCovalence;

            // Check if player can login
            if (canLogin is string || (canLogin is bool && !(bool)canLogin))
            {
                // Reject the user with the message
                Notice.Popup(connection.netUser.networkPlayer, "", canLogin is string ? canLogin.ToString() : "Connection was rejected", 10f); // TODO: Localization
                approval.Deny(uLink.NetworkConnectionError.NoError);
                return true;
            }

            // Call the approval hooks
            var approvedSpecific = Interface.Call("OnUserApprove", connection, approval, acceptor);
            var approvedCovalence = Interface.Call("OnUserApproved", connection.UserName, id, ip);
            return approvedSpecific ?? approvedCovalence;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="netUser"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(NetUser netUser)
        {
            // Do permission stuff
            if (permission.IsLoaded)
            {
                var id = netUser.userID.ToString();

                // Update stored name
                permission.UpdateNickname(id, netUser.displayName);

                // Add player to default group
                if (!permission.UserHasGroup(id, DefaultGroups[0])) permission.AddUserGroup(id, DefaultGroups[0]);

                // Add player to admin group if admin
                if (netUser.CanAdmin() && !permission.UserHasGroup(id, DefaultGroups[2])) permission.AddUserGroup(id, DefaultGroups[2]);
            }

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(netUser);
            Interface.Call("OnUserConnected", covalence.PlayerManager.FindPlayerById(netUser.userID.ToString()));
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="netPlayer"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(uLink.NetworkPlayer netPlayer)
        {
            var netUser = netPlayer.GetLocalData() as NetUser;
            if (netUser == null) return;

            // Let covalence know
            Interface.Call("OnUserDisconnected", covalence.PlayerManager.FindPlayerById(netUser.userID.ToString()), "Unknown");
            covalence.PlayerManager.NotifyPlayerDisconnect(netUser);

            // Delay removing player until OnPlayerDisconnect has fired in plugins
            Interface.Oxide.NextTick(() =>
            {
                if (playerData.ContainsKey(netUser)) playerData.Remove(netUser);
            });
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(PlayerClient client)
        {
            // Call covalence hook
            Interface.Call("OnUserSpawn", covalence.PlayerManager.FindPlayerById(client.userID.ToString()));
        }

        /// <summary>
        /// Called when the player has spawned
        /// </summary>
        /// <param name="client"></param>
        [HookMethod("OnPlayerSpawned")]
        private void OnPlayerSpawned(PlayerClient client)
        {
            var netUser = client.netUser;
            if (!playerData.ContainsKey(netUser)) playerData.Add(netUser, new PlayerData());
            playerData[netUser].character = client.controllable.GetComponent<Character>();
            playerData[netUser].inventory = client.controllable.GetComponent<PlayerInventory>();

            // Call covalence hook
            Interface.Call("OnUserSpawned", covalence.PlayerManager.FindPlayerById(netUser.userID.ToString()));
        }

        /// <summary>
        /// Called when the player is speaking
        /// </summary>
        /// <param name="netUser"></param>
        [HookMethod("IOnPlayerVoice")]
        private object IOnPlayerVoice(NetUser netUser) => (int?)Interface.Call("OnPlayerVoice", netUser, VoiceCom.playerList);

        #endregion

        #region Console Commands

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        [HookMethod("ConsolePlugins")]
        private void ConsolePlugins(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;

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
                arg.ReplyWith("No plugins are currently available");
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            output = loadedPlugins.Aggregate(output, (current, plugin) => current + $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}");
            output = unloadedPluginErrors.Keys.Aggregate(output, (current, pluginName) => current + $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}");
            arg.ReplyWith(output);
        }

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleLoad")]
        private void ConsoleLoad(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Usage: load *|<pluginname>+");
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
                pluginmanager.GetPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleUnload")]
        private void ConsoleUnload(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Usage: unload *|<pluginname>+");
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

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleReload")]
        private void ConsoleReload(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Usage: reload *|<pluginname>+");
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

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleVersion")]
        private void ConsoleVersion(ConsoleSystem.Arg arg) => arg.ReplyWith($"Oxide {OxideMod.Version} for Rust {Rust.Defines.Connection.protocol}");

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleGroup")]
        private void ConsoleGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(2))
            {
                var reply = "Syntax: group <add|set> <name> [title] [rank]";
                reply += "Syntax: group <remove> <name>\n";
                reply += "Syntax: group <parent> <name> <parentName>";
                arg.ReplyWith(reply);
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var title = arg.GetString(2);
            var rank = arg.GetInt(3);

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' already exist");
                    return;
                }
                permission.CreateGroup(name, title, rank);
                arg.ReplyWith("Group '" + name + "' created");
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RemoveGroup(name);
                arg.ReplyWith("Group '" + name + "' deleted");
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                arg.ReplyWith("Group '" + name + "' changed");
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleUserGroup")]
        private void ConsoleUserGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Usage: usergroup <add|remove> <username> <groupname>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var group = arg.GetString(2);

            var player = FindPlayer(name);
            if (player == null && !permission.UserIdValid(name))
            {
                arg.ReplyWith("User '" + name + "' not found");
                return;
            }
            var userId = name;
            if (player != null)
            {
                userId = player.userID.ToString();
                name = player.displayName;
                permission.UpdateNickname(userId, name);
            }

            if (!permission.GroupExists(group))
            {
                arg.ReplyWith("Group '" + group + "' doesn't exist");
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                if (player != null)
                {
                    arg.ReplyWith("User '" + player.displayName + "' assigned group: " + @group);
                }
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                if (player != null)
                {
                    arg.ReplyWith("User '" + player.displayName + "' removed from group: " + @group);
                }
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleGrant")]
        private void ConsoleGrant(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Usage: grant <group|user> <name|id> <permission>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                arg.ReplyWith("Group '" + name + "' granted permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserIdValid(name))
                {
                    arg.ReplyWith("User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.userID.ToString();
                    name = player.displayName;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                arg.ReplyWith("User '" + name + "' granted permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleRevoke")]
        private void ConsoleRevoke(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Usage: revoke <group|user> <name|id> <permission>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                arg.ReplyWith("Group '" + name + "' revoked permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserIdValid(name))
                {
                    arg.ReplyWith("User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.userID.ToString();
                    name = player.displayName;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                arg.ReplyWith("User '" + name + "' revoked permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("ConsoleShow")]
        private void ConsoleShow(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                var reply = "Syntax: show <group|user> <name>\n";
                reply += "Syntax: show <groups|perms>";
                arg.ReplyWith(reply);
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);

            if (mode.Equals("perms"))
            {
                var result = "Permissions:\n";
                result += string.Join(", ", permission.GetPermissions());
                arg.ReplyWith(result);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserIdValid(name))
                {
                    arg.ReplyWith("User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.userID.ToString();
                    name = player.displayName;
                    permission.UpdateNickname(userId, name);
                    name += $"({userId})";
                }
                var result = "User '" + name + "' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += "\nUser '" + name + "' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                arg.ReplyWith(result);
            }
            else if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                var result = "Group '" + name + "' users:\n";
                result += string.Join(", ", permission.GetUsersInGroup(name));
                result += "\nGroup '" + name + "' permissions:\n";
                result += string.Join(", ", permission.GetGroupPermissions(name));
                var parent = permission.GetGroupParent(name);
                while (permission.GroupExists(parent))
                {
                    result += "\nParent group '" + parent + "' permissions:\n";
                    result += string.Join(", ", permission.GetGroupPermissions(parent));
                    parent = permission.GetGroupParent(name);
                }
                arg.ReplyWith(result);
            }
            else if (mode.Equals("groups"))
            {
                arg.ReplyWith("Groups:\n" + string.Join(", ", permission.GetGroups()));
            }
        }

        #endregion

        #region Command Handling

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="wantreply"></param>
        /// <returns></returns>
        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg, bool wantreply)
        {
            if (arg == null) return null;
            var cmdnamefull = $"{arg.Class}.{arg.Function}";

            // Get the args
            var str = arg.GetString(0);

            // Get the covalence player
            var iplayer = arg.argUser != null ? covalence.PlayerManager.FindPlayerById(arg.argUser.userID.ToString()) : null;

            // Is it a console command?
            if (cmdnamefull != "chat.say")
            {
                if (covalence.CommandSystem.HandleConsoleMessage(iplayer, $"{cmdnamefull} {str}") || cmdlib.HandleConsoleCommand(arg, wantreply)) return true;
                return null;
            }

            if (str.Length == 0) return true;

            // Is it a chat command?
            if (str[0] != '/')
            {
                var chatSpecific = Interface.Call("OnPlayerChat", arg.argUser, str);
                var chatCovalence = Interface.Call("OnUserChat", iplayer, str);
                return chatSpecific ?? chatCovalence;
            }

            // Get the full command
            var command = str.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(command, out cmd, out args);
            if (cmd == null) return true;

            // Is the command blocked?
            var commandSpecific = Interface.Call("OnPlayerCommand", arg);
            var commandCovalence = Interface.Call("OnUserCommand", iplayer, cmd, args);
            if (commandSpecific != null || commandCovalence != null) return true;

            // Is this a Covalence command?
            if (covalence.CommandSystem.HandleChatMessage(iplayer, str)) return true;

            // Is it a regular chat command?
            var player = arg.argUser;
            if (player == null)
                Interface.Oxide.LogDebug("Player is actually a {0}!", arg.argUser);
            else if (!cmdlib.HandleChatCommand(player, cmd, args))
                ConsoleNetworker.SendClientCommand(player.networkPlayer, $"chat.add \"Server\" \" Unknown command {cmd}\"");

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

        #region Game Fixes

        /// <summary>
        /// Called when the GetClientMove packet is received for a player
        /// Checks the player position in the packet to prevent harmful packets crashing the server
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="pos"></param>
        [HookMethod("IOnGetClientMove")]
        private object IOnGetClientMove(NetUser netUser, Vector3 pos)
        {
            if (float.IsNaN(pos.x) || float.IsInfinity(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.y) || float.IsNaN(pos.z) || float.IsInfinity(pos.z))
            {
                Interface.Oxide.LogInfo($"Banned {netUser.displayName} [{netUser.userID}] for sending bad packets (possible teleport hack)");
                BanList.Add(netUser.userID, netUser.displayName, "Sending bad packets (possible teleport hack)");
                netUser.Kick(NetError.ConnectionBanned, true);
                return false;
            }

            return Interface.Oxide.CallHook("OnPlayerMove", netUser, pos);
        }

        /// <summary>
        /// Called when receiving an RPC message from a client attempting to run RecieveNetwork on the server
        /// This shouldn't run from the server ever and is only used by metabolism hacks
        /// </summary>
        [HookMethod("IOnRecieveNetwork")]
        private object IOnRecieveNetwork()
        {
            var now = Interface.Oxide.Now;
            if (now - lastWarningAt > 300f)
            {
                lastWarningAt = now;
                Interface.Oxide.LogInfo("An attempt to use a metabolism hack was prevented.");
            }
            return false;
        }

        /// <summary>
        /// Called when an error is thrown because of an invalid RPC message
        /// </summary>
        /// <param name="obj"></param>
        [HookMethod("IOnRPCError")]
        private void IOnRPCError(object obj)
        {
            var info = obj as uLink.NetworkMessageInfo;
            if (info == null) return;
            if (info.sender == uLink.NetworkPlayer.server) return;
            var netuser = info.sender.localData as NetUser;
            if (netuser == null) return;
            Interface.Oxide.LogWarning($"An RPC message from {netuser.displayName} has triggered an exception. Kicking the player...");
            if (netuser.connected) netuser.Kick(NetError.Facepunch_Kick_Violation, true);
        }

        /// <summary>
        /// Called when an AI moves
        /// Checking the NavMeshPathStatus, if the path is invalid the AI is killed to stop NavMesh errors
        /// </summary>
        /// <param name="ai"></param>
        /// <param name="movement"></param>
        [HookMethod("IOnAIMovement")]
        private void IOnAIMovement(BasicWildLifeAI ai, BaseAIMovement movement)
        {
            var nmMovement = movement as NavMeshMovement;
            if (!nmMovement) return;

            if (nmMovement._agent.pathStatus == NavMeshPathStatus.PathInvalid && ai.GetComponent<TakeDamage>().alive)
            {
                TakeDamage.KillSelf(ai.GetComponent<IDBase>());
                Interface.Oxide.LogInfo($"{ai} was destroyed for having an invalid NavMeshPath");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Check if player is admin
        /// </summary>
        /// <returns></returns>
        private static bool IsAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.argUser == null || arg.argUser.CanAdmin()) return true;
            arg.ReplyWith("You are not an admin.");
            return false;
        }

        public NetUser FindPlayer(string nameOrIdOrIp)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.userID.ToString() == nameOrIdOrIp)?.netUser) != null) return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.displayName.ToLower().Contains(nameOrIdOrIp.ToLower()))?.netUser) != null) return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.networkPlayer.ipAddress == nameOrIdOrIp)?.netUser) != null) return netUser;
            return null;
        }

        public static Character GetCharacter(NetUser netUser) => playerData[netUser].character;

        public static PlayerInventory GetInventory(NetUser netUser) => playerData[netUser].inventory;

        #endregion
    }
}
