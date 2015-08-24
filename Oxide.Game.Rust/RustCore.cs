using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using Network;
using ProtoBuf;
using Rust;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using Oxide.Game.Rust.Libraries;

namespace Oxide.Game.Rust
{
    /// <summary>
    /// The core Rust plugin
    /// </summary>
    public class RustCore : CSPlugin
    {
        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission lib
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "player", "moderator", "admin" };

        // The command lib
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // Track when the server has been initialized
        private bool ServerInitialized;

        // Cache the serverInput field info
        private readonly FieldInfo serverInputField = typeof(BasePlayer).GetField("serverInput", BindingFlags.Instance | BindingFlags.NonPublic);

        // Track if a BasePlayer.OnAttacked call is in progress
        private bool isPlayerTakingDamage;

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustCore()
        {
            // Set attributes
            Name = "rustcore";
            Title = "Rust Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);
        }

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Add our commands
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

            cmdlib.AddConsoleCommand("oxide.group", this, "cmdGroup");
            cmdlib.AddConsoleCommand("global.group", this, "cmdGroup");
            cmdlib.AddConsoleCommand("oxide.usergroup", this, "cmdUserGroup");
            cmdlib.AddConsoleCommand("global.usergroup", this, "cmdUserGroup");
            cmdlib.AddConsoleCommand("oxide.grant", this, "cmdGrant");
            cmdlib.AddConsoleCommand("global.grant", this, "cmdGrant");
            cmdlib.AddConsoleCommand("oxide.revoke", this, "cmdRevoke");
            cmdlib.AddConsoleCommand("global.revoke", this, "cmdRevoke");

            if (permission.IsLoaded)
            {
                var rank = 0;
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
            }
            // Configure remote logging
            RemoteLogger.SetTag("game", "rust");
            RemoteLogger.SetTag("protocol", typeof(Protocol).GetField("network").GetValue(null).ToString());
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(ConsoleSystem.Arg arg)
        {
            if (permission.IsLoaded) return true;
            arg.ReplyWith("Unable to load permission files! Permissions will not work until the error has been resolved.\r\n => " + permission.LastException.Message);
            return false;
        }

        /// <summary>
        /// Check if player is admin
        /// </summary>
        /// <returns></returns>
        private static bool IsAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || arg.Player().IsAdmin()) return true;
            arg.ReplyWith("You are not an admin.");
            return false;
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (ServerInitialized) return;
            ServerInitialized = true;
            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", ConVar.Server.hostname);
        }

        /// <summary>
        /// Called when ServerConsole is enabled
        /// </summary>
        [HookMethod("IOnEnableServerConsole")]
        private object IOnEnableServerConsole(ServerConsole serverConsole)
        {
            if (!Interface.Oxide.CheckConsole(true)) return null;
            serverConsole.enabled = false;
            UnityEngine.Object.Destroy(serverConsole);
            typeof(SingletonComponent<ServerConsole>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
            RustExtension.EnableConsole();
            return false;
        }

        /// <summary>
        /// Called when ServerConsole is disabled
        /// </summary>
        [HookMethod("IOnDisableServerConsole")]
        private object IOnDisableServerConsole()
        {
            if (!Interface.Oxide.CheckConsole(true)) return null;
            return false;
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown()
        {
            Interface.Oxide.OnShutdown();
        }

        /// <summary>
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (ServerInitialized) plugin.CallHook("OnServerInitialized");
        }

        /// <summary>
        /// Called when the "oxide.plugins" command has been executed
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
            foreach (var plugin in loaded_plugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}";
            foreach (var plugin_name in unloaded_plugin_errors.Keys)
                output += $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}";
            arg.ReplyWith(output);
        }

        /// <summary>
        /// Called when the "oxide.load" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdLoad")]
        private void cmdLoad(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            // Check arg 1 exists
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: oxide.load *|<pluginname>+");
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
                // Load
                Interface.Oxide.LoadPlugin(name);
                pluginmanager.GetPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "oxide.unload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdUnload")]
        private void cmdUnload(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            // Check arg 1 exists
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: oxide.unload *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Unload
                Interface.Oxide.UnloadPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "oxide.reload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdReload")]
        private void cmdReload(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            // Check arg 1 exists
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: oxide.reload *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Reload
                Interface.Oxide.ReloadPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdVersion")]
        private void cmdVersion(ConsoleSystem.Arg arg)
        {
            // Get the Rust network protocol version at runtime
            var protocol = typeof(Protocol).GetField("network").GetValue(null).ToString();

            // Get the Oxide Core version
            var oxide = OxideMod.Version.ToString();

            // Show the versions
            if (!string.IsNullOrEmpty(protocol) && !string.IsNullOrEmpty(oxide))
            {
                arg.ReplyWith("Oxide version: " + oxide + ", Rust Protocol: " + protocol);
            }
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
            // Check 2 args exists
            if (!arg.HasArgs(2))
            {
                var reply = "Syntax: oxide.group <add|set> <name> [title] [rank]\n";
                reply += "Syntax: oxide.group <remove|show> <name>\n";
                reply += "Syntax: oxide.group <parent> <name> <parentName>";
                arg.ReplyWith(reply);
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' already exist");
                    return;
                }
                permission.CreateGroup(name, arg.GetString(2), arg.GetInt(3));
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
                permission.SetGroupTitle(name, arg.GetString(2));
                permission.SetGroupRank(name, arg.GetInt(3));
                arg.ReplyWith("Group '" + name + "' changed");
            }
            else if (mode.Equals("parent"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                var parent = arg.GetString(2);
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    arg.ReplyWith("Parent group '" + parent + "' doesn't exist");
                    return;
                }
                if (permission.SetGroupParent(name, parent))
                    arg.ReplyWith("Group '" + name + "' changed");
                else
                    arg.ReplyWith("Group '" + name + "' failed to change");
            }
            else if (mode.Equals("show"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                var result = "Group '" + name + "' permissions:\n";
                result += string.Join(",", permission.GetGroupPermissions(name));
                arg.ReplyWith(result);
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
            // Check 3 args exists
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: oxide.usergroup <add|remove> <username> <groupname>");
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
                permission.GetUserData(userId).LastSeenNickname = name;
            }

            if (!permission.GroupExists(group))
            {
                arg.ReplyWith("Group '" + group + "' doesn't exist");
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                arg.ReplyWith("User '" + name + "' assigned group: " + group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                arg.ReplyWith("User '" + name + "' removed from group: " + group);
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
            // Check 3 args exists
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: oxide.grant <group|user> <name|id> <permission>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (!permission.PermissionExists(perm))
            {
                arg.ReplyWith("Permission '" + perm + "' doesn't exist");
                return;
            }

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
                    permission.GetUserData(name).LastSeenNickname = name;
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
            // Check 3 args exists
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: oxide.revoke <group|user> <name|id> <permission>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (!permission.PermissionExists(perm))
            {
                arg.ReplyWith("Permission '" + perm + "' doesn't exist");
                return;
            }

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
                    permission.GetUserData(name).LastSeenNickname = name;
                }
                permission.RevokeUserPermission(userId, perm);
                arg.ReplyWith("User '" + name + "' revoked permission: " + perm);
            }
        }

        private static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID.ToString() == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.userID.ToString() == nameOrIdOrIp)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return null;
        }

        /// <summary>
        /// Called when the server wants to know what tags to use
        /// </summary>
        /// <param name="oldtags"></param>
        /// <returns></returns>
        [HookMethod("IModifyTags")]
        private string IModifyTags(string oldtags)
        {
            // We're going to call out and build a list of all tags to use
            var taglist = new List<string>(oldtags.Split(','));
            Interface.CallHook("BuildServerTags", taglist);
            return string.Join(",", taglist.ToArray());
        }

        /// <summary>
        /// Called when it's time to build the tags list
        /// </summary>
        /// <param name="taglist"></param>
        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist)
        {
            // Add modded and oxide
            taglist.Add("modded");
            taglist.Add("oxide");
        }

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Connection connection)
        {
            // Reject invalid connections
            if (connection.userid == 0 || string.IsNullOrEmpty(connection.username))
            {
                ConnectionAuth.Reject(connection, "Your Steam ID or username is invalid");
                return true;
            }

            // Call out and see if we should reject
            object canlogin = Interface.CallHook("CanClientLogin", connection);
            if (canlogin != null)
            {
                // If it's a bool and it's true, let them in
                if (canlogin is bool && (bool)canlogin) return null;

                // If it's a string, reject them with a message
                if (canlogin is string)
                {
                    ConnectionAuth.Reject(connection, (string)canlogin);
                    return true;
                }

                // We don't know what type it is, reject them with it anyway
                ConnectionAuth.Reject(connection, canlogin.ToString());
                return true;
            }
            return Interface.CallHook("OnUserApprove", connection);
        }

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.cmd == null) return null;

            if (arg.cmd.namefull == "chat.say")
            {
                if (arg.connection != null)
                {
                    var rustCovalence = Libraries.Covalence.RustCovalenceProvider.Instance;
                    var livePlayer = rustCovalence.PlayerManager.GetOnlinePlayer(arg.connection.userid.ToString());
                    if (rustCovalence.CommandSystem.HandleChatMessage(livePlayer, arg.GetString(0, ""))) return true;
                }

                // Get the args
                string str = arg.GetString(0, "text");
                if (str.Length == 0) return null;

                // Is it a chat command?
                if (str[0] == '/' || str[0] == '!')
                {
                    // Get the message
                    string message = str.Substring(1);

                    // Parse it
                    string cmd;
                    string[] args;
                    ParseChatCommand(message, out cmd, out args);
                    if (cmd == null) return null;

                    // Handle it
                    var player = arg.connection.player as BasePlayer;
                    if (player == null)
                    {
                        Interface.Oxide.LogDebug("Player is actually a {0}!", arg.connection.player.GetType());
                    }
                    else
                    {
                        if (!cmdlib.HandleChatCommand(player, cmd, args))
                        {
                            player.SendConsoleCommand("chat.add", 0, $"Unknown command '{cmd}'!");
                        }
                    }

                    // Handled
                    arg.ReplyWith(string.Empty);
                    return true;
                }
            }

            // Default behavior
            return null;
        }

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseChatCommand(string argstr, out string cmd, out string[] args)
        {
            List<string> arglist = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inlongarg = false;
            for (int i = 0; i < argstr.Length; i++)
            {
                char c = argstr[i];
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        string arg = sb.ToString().Trim();
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
                    string arg = sb.ToString().Trim();
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
                string arg = sb.ToString().Trim();
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

        /// <summary>
        /// Called when the player has been initialized
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(BasePlayer player)
        {
            // Let covalence know
            Libraries.Covalence.RustCovalenceProvider.Instance.PlayerManager.NotifyPlayerConnect(player);

            // Do permission stuff
            var authLevel = player.net.connection.authLevel;
            if (authLevel > DefaultGroups.Length || !permission.IsLoaded) return;
            var userId = player.userID.ToString();
            var userData = permission.GetUserData(userId);
            userData.LastSeenNickname = player.displayName;
            if (userData.Groups.Count > 0) return;
            permission.AddUserGroup(userId, DefaultGroups[authLevel]);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BasePlayer player)
        {
            // Let covalence know
            Libraries.Covalence.RustCovalenceProvider.Instance.PlayerManager.NotifyPlayerDisconnect(player);
        }

        /// <summary>
        /// Called when a player tick is received from a client
        /// </summary>
        /// <param name="player"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerTick")]
        private object OnPlayerTick(BasePlayer player, PlayerTick msg)
        {
            return Interface.CallHook("OnPlayerInput", player, serverInputField.GetValue(player));
        }

        /// <summary>
        /// Called when the player has been melee attacked
        /// </summary>
        /// <param name="melee"></param>
        /// <param name="hitinfo"></param>
        [HookMethod("OnMeleeAttack")]
        private object OnMeleeAttack(BaseMelee melee, HitInfo hitinfo)
        {
            return Interface.CallHook("OnPlayerAttack", melee.ownerPlayer, hitinfo);
        }

        /// <summary>
        /// Called when a player arms a trap (currently only BearTrap)
        /// </summary>
        /// <param name="trap"></param>
        /// <param name="player"></param>
        [HookMethod("IOnTrapArm")]
        private object IOnTrapArm(BearTrap trap, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnTrapArm", trap, msg.player);
        }

        /// <summary>
        /// Called when a player disarms a trap (currently only Landmine)
        /// </summary>
        /// <param name="trap"></param>
        /// <param name="player"></param>
        [HookMethod("IOnTrapDisarm")]
        private object IOnTrapDisarm(Landmine trap, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnTrapDisarm", trap, msg.player);
        }

        /// <summary>
        /// Called when the player upgrades a BuildingBlock
        /// </summary>
        /// <param name="block"></param>
        /// <param name="msg"></param>
        /// <param name="grade"></param>
        [HookMethod("IOnStructureUpgrade")]
        private object IOnStructureUpgrade(BuildingBlock block, BaseEntity.RPCMessage msg, BuildingGrade.Enum grade)
        {
            return Interface.CallHook("OnStructureUpgrade", block, msg.player, grade);
        }

        /// <summary>
        /// Called when the player demolishes a BuildingBlock
        /// </summary>
        /// <param name="block"></param>
        /// <param name="msg"></param>
        [HookMethod("IOnStructureDemolish")]
        private object IOnStructureDemolish(BuildingBlock block, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnStructureDemolish", block, msg.player);
        }

        /// <summary>
        /// Called when the player rotates a BuildingBlock
        /// </summary>
        /// <param name="block"></param>
        /// <param name="msg"></param>
        [HookMethod("IOnStructureRotate")]
        private object IOnStructureRotate(BuildingBlock block, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnStructureRotate", block, msg.player);
        }

        /// <summary>
        /// Called when a player locks a sign
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [HookMethod("IOnSignLocked")]
        private object IOnSignLocked(Signage sign, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnSignLocked", sign, msg.player);
        }

        /// <summary>
        /// Called when a player has changed a sign
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="msg"></param>
        [HookMethod("IOnSignUpdated")]
        private object IOnSignUpdated(Signage sign, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnSignUpdated", sign, msg.player);
        }

        /// <summary>
        /// Called when an item loses durability
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        [HookMethod("IOnLoseCondition")]
        private object IOnLoseCondition(Item item, float amount)
        {
            var arguments = new object[] { item, amount };
            Interface.CallHook("OnLoseCondition", arguments);
            amount = (float)arguments[1];
            float condition = item.condition;
            item.condition -= amount;

            if ((item.condition <= 0f) && (item.condition < condition))
            {
                item.OnBroken();
            }

            return true;
        }

        /// <summary>
        /// Called when a BasePlayer is attacked
        /// This is used to call OnEntityTakeDamage for a BasePlayer when attacked
        /// </summary>
        /// <param name="player"></param>
        /// <param name="info"></param>
        [HookMethod("OnBasePlayerAttacked")]
        private object OnBasePlayerAttacked(BasePlayer player, HitInfo info)
        {
            if (isPlayerTakingDamage) return null;
            if (Interface.CallHook("OnEntityTakeDamage", player, info) != null) return true;
            isPlayerTakingDamage = true;
            player.OnAttacked(info);
            isPlayerTakingDamage = false;
            return true;
        }

        /// <summary>
        /// Called when a BasePlayer is hurt
        /// This is used to call OnEntityTakeDamage when a player was hurt without being attacked
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [HookMethod("OnBasePlayerHurt")]
        private object OnBasePlayerHurt(BasePlayer entity, HitInfo info)
        {
            if (isPlayerTakingDamage) return null;
            return Interface.CallHook("OnEntityTakeDamage", entity, info);
        }

        /// <summary>
        /// Called when a BaseCombatEntity takes damage
        /// This is used to call OnEntityTakeDamage for anything other than a BasePlayer
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        [HookMethod("OnBaseCombatEntityHurt")]
        private object OnBaseCombatEntityHurt(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer) return null;
            return Interface.CallHook("OnEntityTakeDamage", entity, info);
        }

        /// <summary>
        /// Called when a player throws a weapon like a Timed Explosive Charge or a F1 Grenade
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="entity"></param>
        [HookMethod("IOnWeaponThrown")]
        private object IOnWeaponThrown(BaseEntity.RPCMessage msg, BaseEntity entity)
        {
            return Interface.CallHook("OnWeaponThrown", msg.player, entity);
        }

        /// <summary>
        /// Called when a player finishes researching an item, before the result is available (success/failure)
        /// </summary>
        /// <param name="table"></param>
        /// <param name="chance"></param>
        [HookMethod("IOnItemResearchEnd")]
        private float IOnItemResearchEnd(ResearchTable table, float chance)
        {
            var returnvar = Interface.CallHook("OnItemResearchEnd", table, chance);
            if (returnvar is float) return (float)returnvar;
            if (returnvar is double) return (float)(double)returnvar;
            return chance;
        }

        /// <summary>
        /// Called when a player launches a rocket with the rocket launcher
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        [HookMethod("IOnRocketLaunched")]
        private object IOnRocketLaunched(BaseEntity.RPCMessage msg, BaseEntity entity)
        {
            return Interface.CallHook("OnRocketLaunched", msg.player, entity);
        }

        /// <summary>
        /// Called when a player fires a weapon
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="msg"></param>
        /// <param name="component"></param>
        /// <param name="projectiles"></param>
        /// <returns></returns>
        [HookMethod("IOnWeaponFired")]
        private object IOnWeaponFired(BaseProjectile projectile, BaseEntity.RPCMessage msg, ItemModProjectile component, ProjectileShoot projectiles)
        {
            return Interface.CallHook("OnWeaponFired", projectile, msg.player, component, projectiles);
        }

        /// <summary>
        /// Called when a player collects an item
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HookMethod("IOnItemPickup")]
        private object IOnItemPickup(BaseEntity.RPCMessage msg, Item item)
        {
            return Interface.CallHook("OnItemPickup", msg.player, item);
        }

        /// <summary>
        /// Called when a player gathers a plant
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HookMethod("IOnPlantGather")]
        private object IOnPlantGather(BaseEntity.RPCMessage msg, BaseEntity entity, Item item)
        {
            return Interface.CallHook("OnPlantGather", entity, item, msg.player);
        }

        /// <summary>
        /// Called when the player has hit something with a hammer
        /// </summary>
        /// <param name="hammer"></param>
        /// <param name="info"></param>
        [HookMethod("IOnHammerHit")]
        private object IOnHammerHit(Hammer hammer, HitInfo info)
        {
            var ent = info.HitEntity as BuildingBlock;
            return ent != null ? null : Interface.CallHook("OnHammerHit", hammer.ownerPlayer, info);
        }

        /// <summary>
        /// Called when a player opened a door
        /// </summary>
        /// <param name="door"></param>
        /// <param name="msg"></param>
        [HookMethod("IOnDoorOpened")]
        private object IOnDoorOpened(Door door, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnDoorOpened", door, msg.player);
        }

        /// <summary>
        /// Called when a player closed a door
        /// </summary>
        /// <param name="door"></param>
        /// <param name="msg"></param>
        [HookMethod("IOnDoorClosed")]
        private object IOnDoorClosed(Door door, BaseEntity.RPCMessage msg)
        {
            return Interface.CallHook("OnDoorClosed", door, msg.player);
        }

        /// <summary>
        /// Called when a player uses a door
        /// This is used to handle the deprecated hook CanOpenDoor
        /// </summary>
        /// <param name="player"></param>
        /// <param name="doorlock"></param>
        [HookMethod("CanUseDoor")]
        private object CanUseDoor(BasePlayer player, BaseLock doorlock)
        {
            return Interface.CallDeprecatedHook("CanOpenDoor", player, doorlock);
        }

        /// <summary>
        /// Called when a player gathers from a dispenser
        /// This is used to handle the deprecated hook OnGather
        /// </summary>
        /// <param name="dispenser"></param>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        [HookMethod("OnDispenserGather")]
        private object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            return Interface.CallDeprecatedHook("OnGather", dispenser, entity, item);
        }

        /// <summary>
        /// Called when the player demolishes a BuildingBlock
        /// This is used to handle the deprecated hook OnBuildingBlockDemolish
        /// </summary>
        /// <param name="block"></param>
        /// <param name="msg"></param>
        [HookMethod("OnStructureDemolish")]
        private object OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            return Interface.CallDeprecatedHook("OnBuildingBlockDemolish", block, player);
        }

        /// <summary>
        /// Called when the player repairs a BuildingBlock
        /// This is used to handle the deprecated hook OnRepair
        /// </summary>
        /// <param name="block"></param>
        /// <param name="player"></param>
        [HookMethod("OnStructureRepair")]
        private object OnStructureRepair(BuildingBlock block, BasePlayer player)
        {
            return Interface.CallDeprecatedHook("OnRepair", block, player);
        }

        /// <summary>
        /// Called when the player rotates a BuildingBlock
        /// This is used to handle the deprecated hook OnBuildingBlockRotate
        /// </summary>
        /// <param name="block"></param>
        /// <param name="msg"></param>
        [HookMethod("OnStructureRotate")]
        private object OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            return Interface.CallDeprecatedHook("OnBuildingBlockRotate", block, player);
        }

        /// <summary>
        /// Called when the player upgrades a BuildingBlock
        /// This is used to handle the deprecated hook OnBuildingBlockUpgrade
        /// </summary>
        /// <param name="block"></param>
        /// <param name="msg"></param>
        /// <param name="grade"></param>
        [HookMethod("OnStructureUpgrade")]
        private object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            return Interface.CallDeprecatedHook("OnBuildingBlockUpgrade", block, player, grade);
        }

        /// <summary>
        /// Called when a mining quarry is enabled
        /// This is used to handle the deprecated hook OnMiningQuarryEnabled
        /// </summary>
        [HookMethod("OnQuarryEnabled")]
        private object OnQuarryEnabled(MiningQuarry quarry)
        {
            return Interface.CallDeprecatedHook("OnMiningQuarryEnabled", quarry);
        }

        /// <summary>
        /// Called when a bear trap snapped
        /// This is used to handle the deprecated hook OnBearTrapSnapped
        /// </summary>
        /// <param name="trap"></param>
        /// <param name="go"></param>
        [HookMethod("OnTrapSnapped")]
        private object OnTrapSnapped(BaseTrapTrigger trap, GameObject go)
        {
            return Interface.CallDeprecatedHook("OnBearTrapSnapped", trap, go);
        }

        /// <summary>
        /// Called when a bear trap triggers
        /// This is used to handle the deprecated hook OnBearTrapTrigger
        /// </summary>
        /// <param name="trap"></param>
        /// <param name="go"></param>
        [HookMethod("OnTrapTrigger")]
        private object OnTrapTrigger(BearTrap trap, GameObject go)
        {
            return Interface.CallDeprecatedHook("OnBearTrapTrigger", trap, go);
        }
    }
}
