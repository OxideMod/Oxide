using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Steamworks;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Hurtworld.Libraries;

namespace Oxide.Game.Hurtworld
{
    /// <summary>
    /// The core Hurtworld plugin
    /// </summary>
    public class HurtworldCore : CSPlugin
    {
        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        // Track 'load' chat commands
        private readonly Dictionary<string, uLink.NetworkPlayer> loadingPlugins = new Dictionary<string, uLink.NetworkPlayer>();

        /// <summary>
        /// Initializes a new instance of the HurtworldCore class
        /// </summary>
        public HurtworldCore()
        {
            // Set attributes
            Name = "hurtworldcore";
            Title = "Hurtworld Core";
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
        private bool PermissionsLoaded(uLink.NetworkPlayer player)
        {
            if (permission.IsLoaded) return true;
            ReplyWith(player, "Unable to load permission files! Permissions will not work until the error has been resolved.\n => " + permission.LastException.Message);
            return false;
        }

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "hurtworld");
            RemoteLogger.SetTag("version", GameManager.Instance.GetProtocolVersion().ToString());

            // Add general chat commands
            //cmdlib.AddChatCommand("oxide.plugins", this, "CmdPlugins");
            //cmdlib.AddChatCommand("plugins", this, "CmdPlugins");
            cmdlib.AddChatCommand("oxide.load", this, "CmdLoad");
            cmdlib.AddChatCommand("load", this, "CmdLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "CmdUnload");
            cmdlib.AddChatCommand("unload", this, "CmdUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "CmdReload");
            cmdlib.AddChatCommand("reload", this, "CmdReload");
            cmdlib.AddChatCommand("oxide.version", this, "CmdVersion");
            cmdlib.AddChatCommand("version", this, "CmdVersion");

            // Add permission chat commands
            cmdlib.AddChatCommand("oxide.group", this, "CmdGroup");
            cmdlib.AddChatCommand("group", this, "CmdGroup");
            cmdlib.AddChatCommand("oxide.usergroup", this, "CmdUserGroup");
            cmdlib.AddChatCommand("usergroup", this, "CmdUserGroup");
            cmdlib.AddChatCommand("oxide.grant", this, "CmdGrant");
            cmdlib.AddChatCommand("grant", this, "CmdGrant");
            cmdlib.AddChatCommand("oxide.revoke", this, "CmdRevoke");
            cmdlib.AddChatCommand("revoke", this, "CmdRevoke");
            cmdlib.AddChatCommand("oxide.show", this, "CmdShow");
            cmdlib.AddChatCommand("show", this, "CmdShow");

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
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore") InitializeLogging();

            if (!loadingPlugins.ContainsKey(plugin.Name)) return;
            ReplyWith(loadingPlugins[plugin.Name], $"Loaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
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

            // Add 'modded' and 'oxide' tags
            SteamGameServer.SetGameTags("modded,oxide");

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", GameManager.Instance.ServerConfig.GameName);
        }

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(string name, uLink.NetworkMessageInfo info)
        {
            var identity = GameManager.Instance.GetIdentity(info.sender);
            identity.Name = name;

            // Let covalence know
            Libraries.Covalence.HurtworldCovalenceProvider.Instance.PlayerManager.NotifyPlayerConnect(info.sender);

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var userId = identity.SteamId.ToString();
                permission.UpdateNickname(userId, identity.Name);

                // Add player to default group
                if (!permission.UserHasAnyGroup(userId)) permission.AddUserGroup(userId, DefaultGroups[0]);
            }

            Interface.Oxide.CallHook("OnPlayerConnected", identity, info.sender);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="player"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(PlayerIdentity identity, uLink.NetworkPlayer player)
        {
            // Let covalence know
            Libraries.Covalence.HurtworldCovalenceProvider.Instance.PlayerManager.NotifyPlayerDisconnect(player);
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="message"></param>
        [HookMethod("OnPlayerChat")]
        private object OnPlayerChat(PlayerIdentity identity, uLink.NetworkMessageInfo info, string message)
        {
            if (message.Trim().Length <= 1) return true;

            var str = message.Substring(0, 1);

            // Is it a chat command?
            if (!str.Equals("/") && !str.Equals("!")) return null;

            // Get the arg string
            var argstr = message.Substring(1);

            // Parse it
            string chatcmd;
            string[] args;
            ParseChatCommand(argstr, out chatcmd, out args);
            if (chatcmd == null) return null;

            // Handle it
            if (!cmdlib.HandleChatCommand(identity, info, chatcmd, args))
            {
                ChatManager.Instance.AppendChatboxServerSingle($"<color=#b8d7a3>Unknown command: {chatcmd}</color>", info.sender);
                return true;
            }

            return true;
        }

        #endregion

        #region Console/Chat Commands

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdPlugins")]
        private void CmdPlugins(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;

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
                ReplyWith(info.sender, "No plugins are currently available");
                return;
            }

            var output = $"Listing {loaded_plugins.Length + unloaded_plugin_errors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loaded_plugins) output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}";
            foreach (var plugin_name in unloaded_plugin_errors.Keys) output += $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}";
            ReplyWith(info.sender, output);
        }

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdLoad")]
        private void CmdLoad(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 1)
            {
                ReplyWith(info.sender, "Syntax: load *|<pluginname>+");
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
                if (!loadingPlugins.ContainsKey(name)) loadingPlugins.Add(name, info.sender);
            }
        }

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdReload")]
        private void CmdReload(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 1)
            {
                ReplyWith(info.sender, "Syntax: reload *|<pluginname>+");
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

                // Reload
                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    ReplyWith(info.sender, $"Plugin '{name}' not loaded.");
                    continue;
                }
                Interface.Oxide.ReloadPlugin(name);
                ReplyWith(info.sender, $"Reloaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            }
        }

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdUnload")]
        private void CmdUnload(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 1)
            {
                ReplyWith(info.sender, "Syntax: unload *|<pluginname>+");
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

                // Unload
                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    ReplyWith(info.sender, $"Plugin '{name}' not loaded.");
                    continue;
                }
                Interface.Oxide.UnloadPlugin(name);
                ReplyWith(info.sender, $"Unloaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            }
        }

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdVersion")]
        private void CmdVersion(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            var oxide = OxideMod.Version.ToString();
            var game = GameManager.Instance?.GetProtocolVersion().ToString();

            if (!string.IsNullOrEmpty(oxide) && !string.IsNullOrEmpty(game))
                ReplyWith(info.sender, $"Oxide version: {oxide}, Hurtworld version: {game}");
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdGroup")]
        private void CmdGroup(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 2)
            {
                ReplyWith(info.sender, "Syntax: group <add|remove|set> <name> [title] [rank]");
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
                    ReplyWith(info.sender, "Group '" + name + "' already exist");
                    return;
                }
                permission.CreateGroup(name, title, rank);
                ReplyWith(info.sender, "Group '" + name + "' created");
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(info.sender, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RemoveGroup(name);
                ReplyWith(info.sender, "Group '" + name + "' deleted");
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(info.sender, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                ReplyWith(info.sender, "Group '" + name + "' changed");
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdUserGroup")]
        private void CmdUserGroup(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 3)
            {
                ReplyWith(info.sender, "Syntax: usergroup <add|remove> <username> <groupname>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindIdentity(name);
            if (target == null && !permission.UserExists(name))
            {
                ReplyWith(info.sender, "User '" + name + "' not found");
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
                ReplyWith(info.sender, "Group '" + group + "' doesn't exist");
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                ReplyWith(info.sender, "User '" + name + "' assigned group: " + group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                ReplyWith(info.sender, "User '" + name + "' removed from group: " + group);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdGrant")]
        private void CmdGrant(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 3)
            {
                ReplyWith(info.sender, "Syntax: grant <group|user> <name|id> <permission>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(info.sender, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                ReplyWith(info.sender, "Group '" + name + "' granted permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var target = FindIdentity(name);
                if (target == null && !permission.UserExists(name))
                {
                    ReplyWith(info.sender, "User '" + name + "' not found");
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
                ReplyWith(info.sender, "User '" + name + "' granted permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="info"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdRevoke")]
        private void CmdRevoke(PlayerIdentity identity, uLink.NetworkMessageInfo info, string command, string[] args)
        {
            if (!PermissionsLoaded(info.sender)) return;
            if (!identity.IsAdmin) return;
            if (args.Length < 3)
            {
                ReplyWith(info.sender, "Syntax: revoke <group|user> <name|id> <permission>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(info.sender, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                ReplyWith(info.sender, "Group '" + name + "' revoked permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var target = FindIdentity(name);
                if (target == null && !permission.UserExists(name))
                {
                    ReplyWith(info.sender, "User '" + name + "' not found");
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
                ReplyWith(info.sender, "User '" + name + "' revoked permission: " + perm);
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
        private object OnRunCommand(string arg) => arg == null || arg.Trim().Length == 0 ? null : cmdlib.HandleConsoleCommand(arg);

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
        /// Replies to the player with a specific message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        private static void ReplyWith(uLink.NetworkPlayer player, string message) => ChatManager.Instance?.AppendChatboxServerSingle(message, player);

        /// <summary>
        /// Lookup the player identity using name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        private PlayerIdentity FindIdentity(string nameOrIdOrIp)
        {
            var identityMap = GameManager.Instance.GetIdentityMap();
            PlayerIdentity identity = null;
            foreach (var i in identityMap)
            {
                if (nameOrIdOrIp.Equals(i.Value.Name, StringComparison.OrdinalIgnoreCase) ||
                    nameOrIdOrIp.Equals(i.Value.SteamId.ToString()) || nameOrIdOrIp.Equals(i.Key.ipAddress))
                {
                    identity = i.Value;
                    break;
                }
            }
            return identity;
        }
    }
}
