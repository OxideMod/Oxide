using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using uLink;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.RustLegacy.Libraries;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// The core Rust Legacy plugin
    /// </summary>
    public class RustLegacyCore : CSPlugin
    {
        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "player", "moderator", "admin" }; // TODO: Migrate to "player" to "default"

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // Track when the server has been initialized
        private bool serverInitialized;

        // Cache the VoiceCom.playerList field info
        private readonly FieldInfo playerList = typeof(VoiceCom).GetField("playerList", BindingFlags.Static | BindingFlags.NonPublic);

        // Cache some player information
        private static readonly Dictionary<NetUser, PlayerData> playerData = new Dictionary<NetUser, PlayerData>();

        public class PlayerData
        {
            public Character character;
            public PlayerInventory inventory;
        }

        // Last Metabolism hacker notification time
        float lastWarningAt;

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustLegacyCore()
        {
            // Set attributes
            Name = "rustlegacycore";
            Title = "Rust Legacy Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);
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

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "rust legacy");
            RemoteLogger.SetTag("version", Rust.Defines.Connection.protocol.ToString());

            // Add general commands
            cmdlib.AddConsoleCommand("oxide.plugins", this, "cmdPlugins");
            cmdlib.AddConsoleCommand("global.plugins", this, "cmdPlugins");
            cmdlib.AddConsoleCommand("oxide.load", this, "cmdLoad");
            cmdlib.AddConsoleCommand("global.load", this, "cmdLoad");
            cmdlib.AddConsoleCommand("oxide.unload", this, "cmdUnload");
            cmdlib.AddConsoleCommand("global.unload", this, "cmdUnload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "cmdReload");
            cmdlib.AddConsoleCommand("global.reload", this, "cmdReload");
            cmdlib.AddConsoleCommand("oxide.version", this, "cmdVersion");
            cmdlib.AddConsoleCommand("global.version", this, "cmdVersion");

            // Add permission commands
            cmdlib.AddConsoleCommand("oxide.group", this, "cmdGroup");
            cmdlib.AddConsoleCommand("global.group", this, "cmdGroup");
            cmdlib.AddConsoleCommand("oxide.usergroup", this, "cmdUserGroup");
            cmdlib.AddConsoleCommand("global.usergroup", this, "cmdUserGroup");
            cmdlib.AddConsoleCommand("oxide.grant", this, "cmdGrant");
            cmdlib.AddConsoleCommand("global.grant", this, "cmdGrant");
            cmdlib.AddConsoleCommand("oxide.revoke", this, "cmdRevoke");
            cmdlib.AddConsoleCommand("global.revoke", this, "cmdRevoke");
            cmdlib.AddConsoleCommand("oxide.show", this, "cmdShow");
            cmdlib.AddConsoleCommand("global.show", this, "cmdShow");

            // Setup the default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
                permission.CleanUp(s =>
                {
                    ulong temp;
                    return ulong.TryParse(s, out temp);
                });
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
        /// Check if player is admin
        /// </summary>
        /// <returns></returns>
        private static bool IsAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.argUser == null || arg.argUser.CanAdmin()) return true;
            arg.ReplyWith("You are not an admin.");
            return false;
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", server.hostname);
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

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

        #region Console Commands

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        [HookMethod("cmdPlugins")]
        private void cmdPlugins(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;

            var loaded_plugins = pluginmanager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            var loaded_plugin_names = new HashSet<string>(loaded_plugins.Select(pl => pl.Name));
            var unloaded_plugin_errors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loaded_plugin_names))
                {
                    string msg;
                    unloaded_plugin_errors[name] = (loader.PluginErrors.TryGetValue(name, out msg)) ? msg : "Unloaded";
                }
            }

            var total_plugin_count = loaded_plugins.Length + unloaded_plugin_errors.Count;
            if (total_plugin_count < 1)
            {
                arg.ReplyWith("[Oxide] No plugins are currently available");
                return;
            }

            var output = $"[Oxide] Listing {loaded_plugins.Length + unloaded_plugin_errors.Count} plugins:";
            var number = 1;
            output = loaded_plugins.Aggregate(output, (current, plugin) => current + $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}");
            output = unloaded_plugin_errors.Keys.Aggregate(output, (current, plugin_name) => current + $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}");
            arg.ReplyWith(output);
        }

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdLoad")]
        private void cmdLoad(ConsoleSystem.Arg arg)
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
        [HookMethod("cmdUnload")]
        private void cmdUnload(ConsoleSystem.Arg arg)
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
        [HookMethod("cmdReload")]
        private void cmdReload(ConsoleSystem.Arg arg)
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
        [HookMethod("cmdVersion")]
        private void cmdVersion(ConsoleSystem.Arg arg)
        {
            var oxide = OxideMod.Version.ToString();
            var rust = Rust.Defines.Connection.protocol.ToString();

            if (!string.IsNullOrEmpty(oxide) && !string.IsNullOrEmpty(rust))
                arg.ReplyWith($"Oxide version: {oxide}, Rust Protocol: {rust}");
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdGroup")]
        private void cmdGroup(ConsoleSystem.Arg arg)
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
        [HookMethod("cmdUserGroup")]
        private void cmdUserGroup(ConsoleSystem.Arg arg)
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
            if (player == null && !permission.UserExists(name))
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
        [HookMethod("cmdGrant")]
        private void cmdGrant(ConsoleSystem.Arg arg)
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
                if (player == null && !permission.UserExists(name))
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
        [HookMethod("cmdRevoke")]
        private void cmdRevoke(ConsoleSystem.Arg arg)
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
                if (player == null && !permission.UserExists(name))
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

        #endregion

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdShow")]
        private void cmdShow(ConsoleSystem.Arg arg)
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
                if (player == null && !permission.UserExists(name))
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
                    result = "\nParent group '" + parent + "' permissions:\n";
                    result += string.Join(", ", permission.GetGroupPermissions(parent));
                    parent = permission.GetGroupParent(name);
                }
                arg.ReplyWith(result);
            }
            else if (mode.Equals("groups"))
            {
                var result = "Groups:\n";
                result += string.Join(", ", permission.GetGroups());
                arg.ReplyWith(result);
            }
        }

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
            // Sanity checks
            if (arg == null) return null;

            string cmd = $"{arg.Class}.{arg.Function}";

            // Is it chat.say?
            if (cmd == "chat.say")
            {
                // Get the args
                var str = arg.GetString(0);
                if (str.Length == 0) return true;

                // Is it a chat command?
                if (str[0] == '/' || str[0] == '!')
                {
                    // Get the arg string
                    var argstr = str.Substring(1);
                    if (str.Length == 1) return true;

                    // Parse it
                    string chatcmd;
                    string[] args;
                    ParseChatCommand(argstr, out chatcmd, out args);
                    if (chatcmd == null) return null;

                    // Handle it
                    var ply = arg.argUser;
                    if (ply != null && !cmdlib.HandleChatCommand(ply, chatcmd, args))
                    {
                        ConsoleNetworker.SendClientCommand(ply.networkPlayer, $"chat.add \"Server\" \" Unknown command {chatcmd}\"");
                        return true;
                    }

                    // Handled
                    arg.ReplyWith(string.Empty);
                    return true;
                }
                return Interface.CallHook("OnPlayerChat", arg.argUser, str);
            }

            return cmdlib.HandleConsoleCommand(arg, wantreply);
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
                return false;
            }

            // Call out and see if we should reject
            var canlogin = Interface.CallHook("CanClientLogin", connection);
            if (canlogin is uLink.NetworkConnectionError)
            {
                approval.Deny((uLink.NetworkConnectionError)canlogin);
                return true;
            }

            return Interface.CallHook("OnUserApprove", connection, approval, acceptor);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(NetUser player)
        {
            // Do permission stuff
            if (permission.IsLoaded)
            {
                var userId = player.userID.ToString();
                permission.UpdateNickname(userId, player.displayName);

                // Add player to default group
                if (!permission.UserHasAnyGroup(userId)) permission.AddUserGroup(userId, DefaultGroups[0]);
            }
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            // Delay removing player until OnPlayerDisconnect has fired in plugins
            var netUser = player.GetLocalData() as NetUser;
            if (netUser != null)
            {
                Interface.Oxide.NextTick(() =>
                {
                    if (playerData.ContainsKey(netUser)) playerData.Remove(netUser);
                });
            }
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
        }

        /// <summary>
        /// Called when the player is speaking
        /// </summary>
        /// <param name="netUser"></param>
        [HookMethod("IOnPlayerVoice")]
        private object IOnPlayerVoice(NetUser netUser)
        {
            var players = (List<uLink.NetworkPlayer>)playerList.GetValue(null);
            playerList.SetValue(null, players);
            return (int?)Interface.CallHook("OnPlayerVoice", netUser, players);
        }

        #endregion

        #region Anti-Cheat Hooks

        /// <summary>
        /// Called when the GetClientMove packed is received for a player
        /// Checking the player position in the packet to prevent harmful packets crashing the server
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
            return null;
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

        #endregion

        public NetUser FindPlayer(string strNameOrIdorIp)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.userID.ToString() == strNameOrIdorIp)?.netUser) != null) return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.displayName.ToLower().Contains(strNameOrIdorIp.ToLower()))?.netUser) != null) return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.networkPlayer.ipAddress == strNameOrIdorIp)?.netUser) != null) return netUser;
            return null;
        }

        public static Character GetCharacter(NetUser netUser) => playerData[netUser].character;

        public static PlayerInventory GetInventory(NetUser netUser) => playerData[netUser].inventory;
    }
}
