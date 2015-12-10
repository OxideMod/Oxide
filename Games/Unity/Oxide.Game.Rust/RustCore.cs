using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using Facepunch;
using Network;
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

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "player", "moderator", "admin" }; // TODO: Migrate "player" to "default"

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

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
            Title = "Rust";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            // Cheat references in the default plugin reference list
            // TODO: Create DefaultReferences for Rust
            var fpNetwork = Network.Client.disconnectReason; // Facepunch.Network
            var fpSystem = Math.unixTimestamp; // Facepunch.System
            var fpUnity = TimeWarning.Enabled; // Facepunch.UnityEngine
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

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "rust");
            RemoteLogger.SetTag("version", Protocol.printable);

            // Add general commands
            cmdlib.AddConsoleCommand("oxide.plugins", this, "CmdPlugins");
            cmdlib.AddConsoleCommand("global.plugins", this, "CmdPlugins");
            cmdlib.AddConsoleCommand("oxide.load", this, "CmdLoad");
            cmdlib.AddConsoleCommand("global.load", this, "CmdLoad");
            cmdlib.AddConsoleCommand("oxide.unload", this, "CmdUnload");
            cmdlib.AddConsoleCommand("global.unload", this, "CmdUnload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "CmdReload");
            cmdlib.AddConsoleCommand("global.reload", this, "CmdReload");
            cmdlib.AddConsoleCommand("oxide.version", this, "CmdVersion");
            //cmdlib.AddConsoleCommand("global.version", this, "CmdVersion");

            // Add permission commands
            cmdlib.AddConsoleCommand("oxide.group", this, "CmdGroup");
            cmdlib.AddConsoleCommand("global.group", this, "CmdGroup");
            cmdlib.AddConsoleCommand("oxide.usergroup", this, "CmdUserGroup");
            cmdlib.AddConsoleCommand("global.usergroup", this, "CmdUserGroup");
            cmdlib.AddConsoleCommand("oxide.grant", this, "CmdGrant");
            cmdlib.AddConsoleCommand("global.grant", this, "CmdGrant");
            cmdlib.AddConsoleCommand("oxide.revoke", this, "CmdRevoke");
            cmdlib.AddConsoleCommand("global.revoke", this, "CmdRevoke");
            cmdlib.AddConsoleCommand("oxide.show", this, "CmdShow");
            cmdlib.AddConsoleCommand("global.show", this, "CmdShow");

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
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", ConVar.Server.hostname);
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
            RustExtension.EnableConsole();
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
            // Call out and see if we should reject
            var canlogin = Interface.CallHook("CanClientLogin", connection);
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
            if (permission.IsLoaded && authLevel <= DefaultGroups.Length)
            {
                var userId = player.userID.ToString();
                permission.UpdateNickname(userId, player.displayName);

                // Add player to default group
                if (!permission.UserHasAnyGroup(userId)) permission.AddUserGroup(userId, DefaultGroups[authLevel]);
            }

            // Cache serverInput for player so that reflection only needs to be used once
            playerInputState[player] = (InputState)serverInputField.GetValue(player);
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

            playerInputState.Remove(player);
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
            InputState input;
            return playerInputState.TryGetValue(player, out input) ? Interface.CallHook("OnPlayerInput", player, input) : null;
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
            if (isPlayerTakingDamage) return null;
            if (Interface.CallHook("OnEntityTakeDamage", player, info) != null) return true;

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
            return isPlayerTakingDamage ? null : Interface.CallHook("OnEntityTakeDamage", entity, info);
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

        #endregion

        #region Entity Hooks

        /// <summary>
        /// Called when a BaseCombatEntity takes damage
        /// This is used to call OnEntityTakeDamage for anything other than a BasePlayer
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="info"></param>
        [HookMethod("IOnBaseCombatEntityHurt")]
        private object IOnBaseCombatEntityHurt(BaseCombatEntity entity, HitInfo info)
        {
            return entity is BasePlayer ? null : Interface.CallHook("OnEntityTakeDamage", entity, info);
        }

        #endregion

        #region Item Hooks

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
            var condition = item.condition;
            item.condition -= amount;
            if ((item.condition <= 0f) && (item.condition < condition)) item.OnBroken();
            return true;
        }

        #endregion

        #region Console Commands

        /// <summary>
        /// Called when the "oxide.plugins" command has been executed
        /// </summary>
        [HookMethod("CmdPlugins")]
        private void CmdPlugins(ConsoleSystem.Arg arg)
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
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var plugin_name in unloaded_plugin_errors.Keys)
                output += $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}";
            arg.ReplyWith(output);
        }

        /// <summary>
        /// Called when the "oxide.load" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdLoad")]
        private void CmdLoad(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: load *|<pluginname>+");
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
        /// Called when the "oxide.unload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdUnload")]
        private void CmdUnload(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: unload *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
                if (string.IsNullOrEmpty(name)) Interface.Oxide.UnloadPlugin(name);
        }

        /// <summary>
        /// Called when the "oxide.reload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdReload")]
        private void CmdReload(ConsoleSystem.Arg arg)
        {
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: reload *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) Interface.Oxide.ReloadPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdVersion")]
        private void CmdVersion(ConsoleSystem.Arg arg)
        {
            var oxide = OxideMod.Version.ToString();
            var rust = Protocol.printable;

            if (!string.IsNullOrEmpty(oxide) && !string.IsNullOrEmpty(rust))
                arg.ReplyWith($"Oxide version: {oxide}, Rust Protocol: {rust}");
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdGroup")]
        private void CmdGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(2))
            {
                var reply = "Syntax: group <add|set> <name> [title] [rank]\n";
                reply += "Syntax: group <remove> <name>\n";
                reply += "Syntax: group <parent> <name> <parentName>";
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
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdUserGroup")]
        private void CmdUserGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: usergroup <add|remove> <username> <groupname>");
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
                name += $"({userId})";
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
        [HookMethod("CmdGrant")]
        private void CmdGrant(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: grant <group|user> <name|id> <permission>");
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
                    permission.UpdateNickname(userId, name);
                    name += $"({userId})";
                }
                permission.GrantUserPermission(userId, perm, null);
                arg.ReplyWith("User '" + name + "' granted permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdRevoke")]
        private void CmdRevoke(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;
            if (!IsAdmin(arg)) return;
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: revoke <group|user> <name|id> <permission>");
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
                    permission.UpdateNickname(userId, name);
                    name += $"({userId})";
                }
                permission.RevokeUserPermission(userId, perm);
                arg.ReplyWith("User '" + name + "' revoked permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("CmdShow")]
        private void CmdShow(ConsoleSystem.Arg arg)
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
                    name += $" ({userId})";
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
                    parent = permission.GetGroupParent(parent);
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

        #endregion

        #region Command Handling

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.cmd == null) return null;
            if (arg.cmd.namefull != "chat.say") return null;

            if (arg.connection != null)
            {
                var rustCovalence = Libraries.Covalence.RustCovalenceProvider.Instance;
                var livePlayer = rustCovalence.PlayerManager.GetOnlinePlayer(arg.connection.userid.ToString());
                if (rustCovalence.CommandSystem.HandleChatMessage(livePlayer, arg.GetString(0))) return true;
            }

            // Get the args
            var str = arg.GetString(0, "text");
            if (str.Length == 0) return null;

            // Is it a chat command?
            if (str[0] == '/' || str[0] == '!')
            {
                // Get the message
                var message = str.Substring(1);

                // Parse it
                string cmd;
                string[] args;
                ParseChatCommand(message, out cmd, out args);
                if (cmd == null) return null;

                // Handle it
                var player = arg.connection.player as BasePlayer;
                if (player == null)
                    Interface.Oxide.LogDebug("Player is actually a {0}!", arg.connection.player.GetType());
                else
                    if (!cmdlib.HandleChatCommand(player, cmd, args))
                        player.SendConsoleCommand("chat.add", 0, $"Unknown command '{cmd}'!");

                // Handled
                arg.ReplyWith(string.Empty);
                return true;
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

        #region Deprecated Hooks

        /// <summary>
        /// Used to handle the deprecated hook OnItemPickup
        /// </summary>
        /// <param name="item"></param>
        /// <param name="player"></param>
        [HookMethod("OnCollectiblePickup")]
        private object OnCollectiblePickup(Item item, BasePlayer player) => Interface.CallDeprecatedHook("OnItemPickup", player, item);

        /// <summary>
        /// Used to handle the deprecated hook OnWeaponThrown
        /// </summary>
        /// <param name="player"></param>
        /// <param name="entity"></param>
        [HookMethod("OnExplosiveThrown")]
        private object OnExplosiveThrown(BasePlayer player, BaseEntity entity) => Interface.CallDeprecatedHook("OnWeaponThrown", player, entity);

        /// <summary>
        /// Used to handle the deprecated hook OnTurrentHasTarget
        /// </summary>
        /// <param name="turret"></param>
        [HookMethod("OnTurretHasTarget")]
        private object OnTurretHasTarget(AutoTurret turret) => Interface.CallDeprecatedHook("OnTurrentHasTarget", turret);

        /// <summary>
        /// Used to handle the deprecated hook OnTurrentSetTarget
        /// </summary>
        /// <param name="turret"></param>
        /// <param name="entity"></param>
        [HookMethod("OnTurretSetTarget")]
        private object OnTurretSetTarget(AutoTurret turret, BaseCombatEntity entity) => Interface.CallDeprecatedHook("OnTurrentSetTarget", turret, entity);

        #endregion
    }
}
